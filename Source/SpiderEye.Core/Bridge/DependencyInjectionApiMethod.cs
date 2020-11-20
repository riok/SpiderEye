using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SpiderEye.Bridge
{
    internal class DependencyInjectionApiMethod : ApiMethod, IDependencyInjectionApiMethod
    {
        private readonly Type type;

        public DependencyInjectionApiMethod(Type type, MethodInfo info)
            : base(info)
        {
            this.type = type;
        }

        public Task<object> InvokeAsync(IServiceProvider serviceProvider, object parameter)
        {
            var instance = serviceProvider.GetRequiredService(type);
            return InvokeAsync(instance, parameter);
        }
    }
}
