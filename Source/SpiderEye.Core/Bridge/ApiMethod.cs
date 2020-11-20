using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpiderEye.Tools;

namespace SpiderEye.Bridge
{
    internal abstract class ApiMethod
    {
        public string Name { get; }
        public Type ParameterType { get; }
        public Type ReturnType { get; }
        public bool HasReturnValue { get; }
        public bool HasParameter { get; }
        public bool IsAsync { get; }

        private readonly MethodInfo info;
        private readonly Func<object, object> getTaskResult;

        public ApiMethod(MethodInfo info)
        {
            this.info = info ?? throw new ArgumentNullException(nameof(info));

            Name = JsTools.NormalizeToJsName(info.Name);
            ParameterType = info.GetParameters().FirstOrDefault()?.ParameterType;
            HasParameter = ParameterType != null;

            ReturnType = info.ReturnType;
            if (ReturnType.IsGenericType && ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var prop = ReturnType.GetProperty("Result");
                getTaskResult = (task) => prop.GetValue(task, null);

                ReturnType = ReturnType.GenericTypeArguments[0];
                IsAsync = true;
            }
            else if (ReturnType == typeof(Task))
            {
                ReturnType = typeof(void);
                IsAsync = true;
            }

            HasReturnValue = ReturnType != typeof(void);
        }

        protected async Task<object> InvokeAsync(object instance, object parameter)
        {
            var result = HasParameter
                ? info.Invoke(instance, new object[] { parameter })
                : info.Invoke(instance, null);

            if (!IsAsync) { return result; }

            await (result as Task);

            if (!HasReturnValue) { return null; }
            else { return getTaskResult(result); }
        }
    }
}
