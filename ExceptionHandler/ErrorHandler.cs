using log4net.Core;
using System;

namespace ExceptionHandler
{
    /// <summary>
    /// Created to handle errors that will occur in Log4Net.
    /// </summary>
    public class ErrorHandler : IErrorHandler
    {
        public void Error(string message)
        {
        }

        public void Error(string message, Exception ex)
        {
        }

        public void Error(string message, Exception ex, ErrorCode errorCode)
        {
        }
    }
}
