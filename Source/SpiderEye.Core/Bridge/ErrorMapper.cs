using System;
using SpiderEye.Bridge.Models;

namespace SpiderEye.Bridge
{
    /// <summary>
    /// Maps exceptions to API results.
    /// </summary>
    public class ErrorMapper
    {
        /// <summary>
        /// Maps an error to an API result.
        /// </summary>
        /// <param name="exception">The error to map.</param>
        /// <returns>The resulting API model.</returns>
        public virtual ApiResultModel MapErrorToApiResult(Exception exception)
        {
            return new ApiResultModel
            {
                Value = null,
                Success = false,
                Error = exception.Message,
                ErrorTypeName = exception.GetType().Name,
                ErrorTypeFullName = exception.GetType().FullName,
                ErrorDetail = exception.ToString(),
            };
        }
    }
}
