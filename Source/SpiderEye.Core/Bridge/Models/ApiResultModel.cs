using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpiderEye.Bridge.Models
{
    public class ApiResultModel
    {
        [JsonIgnore]
        public string Value
        {
            get { return ValueRaw?.Value as string; }
            set { ValueRaw = value == null ? null : new JRaw(value); }
        }

        public bool Success { get; set; }
        public string Error { get; set; }
        public string ErrorTypeName { get; set; }
        public string ErrorTypeFullName { get; set; }
        public bool IsUiFriendlyError { get; set; }
        public string ErrorDetail { get; set; }

        [JsonProperty(nameof(Value))]
        private JRaw ValueRaw { get; set; }

        public static ApiResultModel FromError(string message)
        {
            return new ApiResultModel
            {
                Value = null,
                Success = false,
                Error = message,
            };
        }
    }
}
