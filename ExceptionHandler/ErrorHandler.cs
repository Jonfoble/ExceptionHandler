using log4net.Core;
using System;

namespace ExceptionHandler
{
    /// <summary>
    /// Log4Net içerisinde oluşacak hataları handle etmek için oluşturuldu
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
