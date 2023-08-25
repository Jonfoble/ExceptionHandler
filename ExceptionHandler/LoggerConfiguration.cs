using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ExceptionHandler
{
    public enum LoggerType
    {
        Database,
        Mail,
        File,
        History
    }

    class LoggerConfiguration
    {
        static string sqlLogConStr = @"Data Source=SFNMSSQLDB\SFNMSSQLDB;Initial Catalog=SFN_LOG;User ID=sfn_bt;Password=mgu204p2";
        static string sqlSettConStr = @"Data Source=SFNMSSQLDB\SFNMSSQLDB;Initial Catalog=SFN_ServiceManager;User ID=sfn_bt;Password=mgu204p2";

        /// <summary>
        /// The method that makes the log4net library available for use
        /// </summary>
        public static void ConfigureAppender(LoggerType lType, ref ILoggerRepository iRepo)
        {
            try
            {
                if (lType == LoggerType.Database)
                {
                    try
                    {
                        SqlConnection con = new SqlConnection(sqlLogConStr);
                        con.Open();
                        lType = (con.State == ConnectionState.Closed || con.State == ConnectionState.Broken) ? LoggerType.Mail : lType;
                    }
                    catch (Exception)
                    {
                        lType = LoggerType.Mail;
                    }
                }

                var appender = lType == LoggerType.Database ||  lType == LoggerType.History ? GetAdoNetAppender(lType) : lType == LoggerType.Mail ? GetMailAppender() : GetFileAppender();
                
                if (appender != null)
                {
                    BasicConfigurator.Configure(iRepo, appender);
                    ((Hierarchy)LogManager.GetRepository(iRepo.Name)).Root.Level = Level.Debug;
                }
            }
            catch (Exception)
            {}
        }

        /// <summary>
        /// Method to make settings for logging to database (only mssql)
        /// </summary>
        /// <returns></returns>
        private static IAppender GetAdoNetAppender(LoggerType lType = LoggerType.Database)
        {
            try
            {
                var appender = new Appenders.AsyncAdoNetAppender
                {
                    Name = "DatabaseAppender",
                    BufferSize = 1,
                    ConnectionType = typeof(SqlConnection).AssemblyQualifiedName,
                    ConnectionString = sqlLogConStr,
                    CommandText = lType == LoggerType.Database ? "INSERT INTO tbl_Log([DateUTC], [Thread], [Namespace], [Class], [Method], [Line], [Level], [Logger], [User], [Message], [Exception], [Count_], [CallingNamespace]) VALUES (@date, @thread, @namespace, @class, @method, @line, @level, @logger, @username, @message, @exception, 1, @callingNamespace)" : "INSERT INTO tbl_HistoryLog([Date], [Source], [User], [Message]) VALUES(@date, @callingNamespace, @username, @message)",
                    Threshold = Level.All,
                    ErrorHandler = new ErrorHandler()
                };

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@date",
                    DbType = DbType.DateTime,
                    Layout = new RawTimeStampLayout()
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@callingNamespace",
                    DbType = DbType.String,
                    Size = 150,
                    Layout = new Layout2RawLayoutAdapter(new PatternLayout("%property{callingNamespace}"))
                });


                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@username",
                    DbType = DbType.String,
                    Size = 100,
                    Layout = new Layout2RawLayoutAdapter(new PatternLayout("%username"))
                });

                appender.AddParameter(new AdoNetAppenderParameter
                {
                    ParameterName = "@message",
                    DbType = DbType.String,
                    Size = 250,
                    Layout = new Layout2RawLayoutAdapter(new PatternLayout("%message"))
                });

                if (lType != LoggerType.History)
                {
                    appender.AddParameter(new AdoNetAppenderParameter
                    {
                        ParameterName = "@thread",
                        DbType = DbType.String,
                        Size = 4000,
                        Layout = new Layout2RawLayoutAdapter(new PatternLayout("%thread"))
                    });

                    appender.AddParameter(new AdoNetAppenderParameter
                    {
                        ParameterName = "@namespace",
                        DbType = DbType.String,
                        Size = 150,
                        Layout = new Layout2RawLayoutAdapter(new PatternLayout("%property{exNamespace}"))
                    });

                    appender.AddParameter(new AdoNetAppenderParameter
                    {
                        ParameterName = "@class",
                        DbType = DbType.String,
                        Size = 150,
                        Layout = new Layout2RawLayoutAdapter(new PatternLayout("%property{exClass}"))
                    });

                    appender.AddParameter(new AdoNetAppenderParameter
                    {
                        ParameterName = "@method",
                        DbType = DbType.String,
                        Size = 250,
                        Layout = new Layout2RawLayoutAdapter(new PatternLayout("%property{exMethod}"))
                    });

                    appender.AddParameter(new AdoNetAppenderParameter
                    {
                        ParameterName = "@line",
                        DbType = DbType.Int32,
                        Layout = new Layout2RawLayoutAdapter(new PatternLayout("%property{exLine}"))
                    });

                    appender.AddParameter(new AdoNetAppenderParameter
                    {
                        ParameterName = "@level",
                        DbType = DbType.String,
                        Size = 60,
                        Layout = new Layout2RawLayoutAdapter(new PatternLayout("%level"))
                    });

                    appender.AddParameter(new AdoNetAppenderParameter
                    {
                        ParameterName = "@logger",
                        DbType = DbType.String,
                        Size = 250,
                        Layout = new Layout2RawLayoutAdapter(new PatternLayout("%logger"))
                    });

                    appender.AddParameter(new AdoNetAppenderParameter
                    {
                        ParameterName = "@exception",
                        DbType = DbType.String,
                        Size = 1250,
                        Layout = new Layout2RawLayoutAdapter(new PatternLayout("%exception"))
                    });
                }

                appender.ActivateOptions();
                return appender;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// The method by which the settings are made for mail logging
        /// </summary>
        /// <returns></returns>
        private static IAppender GetMailAppender()
        {
            SqlConnection con = new SqlConnection(sqlSettConStr);
            try
            {
                PatternLayout patternLayout = new PatternLayout { ConversionPattern = "[%date] %newline [%thread] %username %property{exNamespace}-%property{exClass}-%property{exMethod}-Line %property{exLine} %newline %message %newline %exception %newline %property{callingNamespace}" };
                patternLayout.ActivateOptions();

                //Circular Ref. We fall into a design error. It was shot manually as there were no suitable common areas for Dependency Injection. In case of slowness, it may be that the relevant settings are checked by regedit as an extra for this place.
                #region Mail ile ilgili bilgilerin alındığı kısım
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT [key], [value] FROM tbl_CommonConfig WHERE [key] IN('_emailAdress', '_smtpPort', '_smtpHost', '_enableSSL', '_userName', '_password')", con);
                SqlDataReader dr = cmd.ExecuteReader();
                Dictionary<string, string> di = new Dictionary<string, string>();

                while (dr.Read())
                {
                    di.Add(dr["key"].ToString() ?? string.Empty, dr["value"].ToString() ?? string.Empty);
                }
                #endregion

                var appender = new SmtpAppender
                {
                    Name = "MailAppender",
                    Authentication = SmtpAppender.SmtpAuthentication.Basic,
                    SmtpHost = di["_smtpHost"],
                    Port = Convert.ToInt32(di["_smtpPort"]),
                    Username = di["_userName"],
                    Password = di["_password"],
                    EnableSsl = false,
                    To = "",
                    BufferSize = 1,
                    Threshold = Level.All,
                    Lossy = false,
                    From = di["_emailAdress"],
                    Subject = "",
                    Layout = patternLayout,
                    ErrorHandler = new ErrorHandler()
                };

                appender.ActivateOptions();
                return appender;
            }
            catch (Exception)
            {
                con.Dispose();
                con.Close();
                return null;
            }
            finally 
            {
                con.Dispose();
                con.Close();
            }
        }

        /// <summary>
        /// Method to make settings for logging to file
        /// </summary>
        /// <returns></returns>
        private static IAppender GetFileAppender()
        {
            try
            {
                PatternLayout patternLayout = new PatternLayout { ConversionPattern = "[%date] %newline [%thread] %username %property{exNamespace}-%property{exClass}-%property{exMethod}-Line %property{exLine} %newline %message %newline %exception %newline %property{callingNamespace} %newline" };
                patternLayout.ActivateOptions();

                var appender = new Appenders.AsyncFileAppender
                {
                    Name = "FileAppender",
                    File = "SOFTWARE_LOG.txt",
                    MaximumFileSize = "10MB",
                    RollingStyle = RollingFileAppender.RollingMode.Size,
                    StaticLogFileName = true,
                    MaxSizeRollBackups = 5,
                    Layout = patternLayout,
                    ErrorHandler = new ErrorHandler(),
                    Threshold = Level.All
                };

                appender.ActivateOptions();
                return appender;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
