using System;
using System.Runtime.InteropServices;
using SpiderEye.Mac.Native;

namespace SpiderEye.Mac.Interop
{
    internal sealed class NativeClassInstance : IDisposable
    {
        public IntPtr Handle { get; private set; }

        private readonly GCHandle parentHandle;
        private readonly IntPtr ivar;
        private bool disposed;

        internal NativeClassInstance(IntPtr instance, GCHandle parentHandle, IntPtr ivar)
        {
            Handle = instance;
            this.parentHandle = parentHandle;
            this.ivar = ivar;
        }

        ~NativeClassInstance()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (!disposing)
            {
                // Native Cocoa/WebKit cleanup must happen deterministically during Dispose().
                // Avoid running Objective-C interop from the finalizer thread.
                return;
            }

            if (Handle != IntPtr.Zero)
            {
                if (ivar != IntPtr.Zero)
                {
                    ObjC.SetVariableValue(Handle, ivar, IntPtr.Zero);
                }

                ObjC.Call(Handle, "release");
                Handle = IntPtr.Zero;
            }

            if (parentHandle.IsAllocated)
            {
                parentHandle.Free();
            }
        }
    }
}
