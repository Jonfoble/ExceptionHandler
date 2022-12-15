using log4net;
using log4net.Core;
using log4net.Repository;
using System;
using System.Diagnostics;
using System.Reflection;

namespace ExceptionHandler
{
    public class Logger
    {
        #region Fields
        private static object lockedW = new object();
        private static object lockedI = new object();
        private static object lockedH = new object();

        private static ILoggerRepository fileRepo;
        private static ILoggerRepository mailRepo;
        private static ILoggerRepository databaseRepo;
        private static ILoggerRepository historyRepo;

        private static log4net.ILog logFile;
        private static log4net.ILog logMail;
        private static log4net.ILog logDatabase;
        private static log4net.ILog logHistory;
        #endregion

        /// <summary>
        /// Hata fırlatıldığı noktalarda kullanılması gereken metod
        /// </summary>
        /// <param name="ex">Hata</param>
        /// <param name="uMessage">Opsiyonel kullanıcı mesajı</param>
        /// <param name="lType">Loglama Tipi</param>
        public static void W(Exception ex, string uMessage = "", LoggerType lType = LoggerType.Database)
        {
            try
            {
                lock (lockedW)
                {
                    LoggerRepoControl();
                    ILog i = TypeSelector(lType);
                    if (i != null)
                    {
                        if (ex != null)
                        {
                            StackTrace sTrace = new StackTrace(ex, true);
                            if (sTrace.FrameCount > 0)
                            {
                                log4net.GlobalContext.Properties["exMethod"] = sTrace.GetFrame(0).GetMethod().Name;
                                log4net.GlobalContext.Properties["exLine"] = sTrace.GetFrame(0).GetFileLineNumber();
                            }

                            if (ex.TargetSite != null && ex.TargetSite.ReflectedType != null)
                            {
                                log4net.GlobalContext.Properties["exClass"] = ex.TargetSite.ReflectedType.Name;
                                log4net.GlobalContext.Properties["exNamespace"] = ex.TargetSite.ReflectedType.Namespace;
                            }

                            string fullNamepsace = Assembly.GetCallingAssembly().FullName ?? string.Empty;
                            
                            log4net.GlobalContext.Properties["callingNamespace"] = fullNamepsace;
                        }
                        i.Error(uMessage, ex);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Sadece bilgi amaçlı loglama yapılması durumunda kullanılması gereken metod
        /// </summary>
        /// <param name="lType">Loglama Tipi</param>
        /// <param name="message">Mesaj</param>
        public static void I(string message, LoggerType lType = LoggerType.Database)
        {
            try
            {
                lock (lockedI)
                {
                    LoggerRepoControl();
                    ILog log = TypeSelector(lType);
                    if (log != null)
                    {
                        log4net.GlobalContext.Properties["exMethod"] = string.Empty;
                        log4net.GlobalContext.Properties["exLine"] = 0;

                        log4net.GlobalContext.Properties["exClass"] = string.Empty;
                        log4net.GlobalContext.Properties["exNamespace"] = string.Empty;
                        log.Info(message ?? string.Empty);
                    }
                }
            }
            catch (Exception)
            {   
            }
        }

        /// <summary>
        /// Belgeleme amaçı ile oluşturulmuş ve tekrarlayan kayıtların ayrı satırda tutulması gerektiğinde kullanılması gereken metod
        /// </summary>
        /// <param name="message"></param>
        public static void H(string message)
        {
            try
            {
                lock (lockedH)
                {
                    LoggerRepoControl();
                    ILog log = TypeSelector(LoggerType.History);

                    string fullNamepsace = Assembly.GetCallingAssembly().FullName ?? string.Empty;
                    log4net.GlobalContext.Properties["callingNamespace"] = fullNamepsace;

                    if (log != null)
                        log.Info(message ?? string.Empty);
                }
            }
            catch (Exception)
            {
            }
        }

        private static void LoggerRepoControl() 
        {
            try
            {
                string callingNamespace = Assembly.GetCallingAssembly().FullName.Split(',')[0] ?? Guid.NewGuid().ToString();
                if (fileRepo == null)
                {
                    fileRepo = LoggerManager.CreateRepository(string.Format("{0}.{1}", "FileRepo", callingNamespace));
                    logFile = log4net.LogManager.GetLogger(string.Format("{0}.{1}", "FileRepo", callingNamespace), "FileAppender");
                }
                if (databaseRepo == null)
                {
                    databaseRepo = LoggerManager.CreateRepository(string.Format("{0}.{1}", "DatabaseRepo", callingNamespace));
                    logDatabase = log4net.LogManager.GetLogger(string.Format("{0}.{1}", "DatabaseRepo", callingNamespace), "DatabaseAppender");
                }
                if (mailRepo == null)
                {
                    mailRepo = LoggerManager.CreateRepository(string.Format("{0}.{1}", "MailRepo", callingNamespace));
                    logMail = log4net.LogManager.GetLogger(string.Format("{0}.{1}", "MailRepo", callingNamespace), "MailAppender");
                }
                if (historyRepo == null)
                {
                    historyRepo = LoggerManager.CreateRepository(string.Format("{0}.{1}", "HistoryRepo", callingNamespace));
                    logHistory = log4net.LogManager.GetLogger(string.Format("{0}.{1}", "HistoryRepo", callingNamespace), "DatabaseAppender");
                }
                
            }
            catch (Exception)
            {
            }
        }

        private static ILog TypeSelector(LoggerType lType)
        {
            try
            {
                switch (lType)
                {
                    case LoggerType.Database:
                        {
                            if (!logDatabase.Logger.Repository.Configured)
                                LoggerConfiguration.ConfigureAppender(lType, ref databaseRepo);

                            return logDatabase;
                        }
                    case LoggerType.Mail:
                        {
                            if (!logMail.Logger.Repository.Configured)
                                LoggerConfiguration.ConfigureAppender(lType, ref mailRepo);

                            return logMail;
                        }
                    case LoggerType.File:
                        {
                            if (!logFile.Logger.Repository.Configured)
                                LoggerConfiguration.ConfigureAppender(lType, ref fileRepo);

                            return logFile;
                        }
                    case LoggerType.History:
                        {
                            if (!logHistory.Logger.Repository.Configured)
                                LoggerConfiguration.ConfigureAppender(lType, ref historyRepo);

                            return logHistory;
                        }
                    default:
                        {
                            if (!logFile.Logger.Repository.Configured)
                                LoggerConfiguration.ConfigureAppender(LoggerType.File, ref fileRepo);

                            return logFile;
                        }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}