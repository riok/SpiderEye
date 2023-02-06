namespace SpiderEye.Bridge.Models
{
    public class ApiResultModel
    {
        public object Value { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public string ErrorTypeName { get; set; }
        public string ErrorTypeFullName { get; set; }
        public bool IsUiFriendlyError { get; set; }
        public string ErrorDetail { get; set; }

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
