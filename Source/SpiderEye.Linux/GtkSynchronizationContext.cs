using System;
using System.Runtime.InteropServices;
using System.Threading;
using GLib;
using GLib.Internal;
using Functions = GLib.Internal.Functions;
using Thread = System.Threading.Thread;

namespace SpiderEye.Linux
{
    internal sealed class GtkSynchronizationContext : SynchronizationContext
    {
        public bool IsMainThread
        {
            get { return Thread.CurrentThread.ManagedThreadId == mainThreadId; }
        }

        private readonly int mainThreadId;
        private readonly GLib.Internal.SourceFunc invokeCallbackDelegate = InvokeCallback;

        public GtkSynchronizationContext()
            : this(Thread.CurrentThread.ManagedThreadId)
        {
        }

        private GtkSynchronizationContext(int mainThreadId)
        {
            this.mainThreadId = mainThreadId;
        }

        public override SynchronizationContext CreateCopy()
        {
            return new GtkSynchronizationContext(mainThreadId);
        }

        public override void Post(SendOrPostCallback d, object state)
            => ScheduleAction(() => d(state));

        public override void Send(SendOrPostCallback d, object state)
        {
            // This can be reworked as soon as GirCore supports this in the MainLoopSynchronizationContext
            if (d == null) { throw new ArgumentNullException(nameof(d)); }

            if (IsMainThread) { d(state); }
            else
            {
                var data = new InvokeState(d, state, true);
                var handle = GCHandle.Alloc(data, GCHandleType.Normal);

                lock (data)
                {
                    Functions.IdleAdd(Constants.PRIORITY_DEFAULT_IDLE, invokeCallbackDelegate, GCHandle.ToIntPtr(handle), null);
                    Monitor.Wait(data);
                }
            }
        }

        private static bool InvokeCallback(IntPtr data)
        {
            var handle = GCHandle.FromIntPtr(data);
            var state = (InvokeState)handle.Target;

            try { state.Callback(state.State); }
            finally
            {
                if (state.Synchronous) { lock (state) { Monitor.Pulse(state); } }
                handle.Free();
            }

            return false;
        }

        private static void ScheduleAction(Action action)
        {
            var proxy = new SourceFuncNotifiedHandler(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    UnhandledException.Raise(ex);
                }

                return false;
            });

            Functions.IdleAdd(Constants.PRIORITY_DEFAULT_IDLE, proxy.NativeCallback, IntPtr.Zero, proxy.DestroyNotify);
        }

        private sealed class InvokeState
        {
            public readonly SendOrPostCallback Callback;
            public readonly object State;
            public readonly bool Synchronous;

            public InvokeState(SendOrPostCallback callback, object state, bool synchronous)
            {
                Callback = callback;
                State = state;
                Synchronous = synchronous;
            }
        }
    }
}
