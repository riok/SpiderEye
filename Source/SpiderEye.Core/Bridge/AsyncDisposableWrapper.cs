using System;
using System.Threading.Tasks;

namespace SpiderEye.Bridge
{
    internal class AsyncDisposableWrapper : IAsyncDisposable
    {
        private readonly object obj;

        public AsyncDisposableWrapper(object obj)
        {
            this.obj = obj;
        }

        public async ValueTask DisposeAsync()
        {
            switch (obj)
            {
                case IAsyncDisposable d:
                    await d.DisposeAsync();
                    break;
                case IDisposable d:
                    d.Dispose();
                    break;
            }

            GC.SuppressFinalize(this);
        }
    }
}
