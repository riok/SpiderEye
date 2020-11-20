using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SpiderEye.Bridge
{
    internal class InstanceApiMethod : ApiMethod
    {
        private readonly object instance;

        public InstanceApiMethod(object instance, MethodInfo info)
            : base(info)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public Task<object> InvokeAsync(object parameter)
            => InvokeAsync(instance, parameter);
    }
}
