using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Logging
{
    /// <summary>
    /// A simple logger that outputs messages to a log file in the \Logs subfolder using the 3rd-party NuGet package "nLog".
    /// This class is supposed to be utilized by all Proschlaf applications that require logging.
    /// Configuration happens in the "NLog.config" file.
    /// The log file is in the *csv format and saves time, level, category and text/error message of the log entries.
    /// </summary>
    public static class Logger
    {
        #region Public Vars
        /// <summary>
        /// Determines whether to write an error log file or not.
        /// </summary>
        public static bool WriteLogFile = true;

        /// <summary>
        /// Determines whether to write the user-friendly log file or not.
        /// </summary>
        public static bool WriteUserFriendlyLogFile = true;

        /// <summary>
        /// Affects the category and thus the number of log messages being stored.
        /// </summary>
        public static LogLevels LogLevel = LogLevels.All;

        /// <summary>
        /// Set to true once the logger has been initialization via the constructor. If an exception occurs during the initialization, this flag remains set to false.
        /// </summary>
        public static bool IsInitialized = false;

        public static Exception LastException = null;

        /// <summary>
        /// A list of all errors that occured within the last hours. Maximum size = 1000 entries.
        /// </summary>
        public static List<LogEntry> LatestErrors { get; set; } = new List<LogEntry>();
        #endregion

        #region Private Vars
        private static NLog.Logger applicationLogger = null;
        private static NLog.Logger userFriendlyLogger = null;

        static string logFilePath = null;

        private static object errorListLock = new object();
        #endregion

        #region Enums
        public enum LogLevels { All, ErrorsOnly, None };

        /// <summary>
        /// Creates the possibility to categorize a log message.
        /// </summary>
        public enum LogEntryCategories { Fatal, Error, Warning, Info, Debug, Trace, Other };
        #endregion

        #region Events

        /// <summary>
        /// Fired everytime a log entry to the error is added.
        /// </summary>
        public static event EventHandler<LogEntry> OnLogEvent;

        /// <summary>
        /// Fired everytime a log entry is added to the user-friendly log.
        /// </summary>
        public static event EventHandler<UserLogEntry> OnUserLogEvent;
        #endregion

        /// <summary>
        /// Inits and opens the *csv log files.
        /// </summary>
        static Logger()
        {
            try
            {
                //init logger
                applicationLogger = LogManager.GetLogger("logfile");
                userFriendlyLogger = LogManager.GetLogger("userLogfile");
                LogManager.Configuration.Reload(); //if the NLog configuration has changed, trigger an update of the physical log files
                                                   //LogManager.ReconfigExistingLoggers();
                logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Logs\nLog.csv");

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                LastException = ex;
                IsInitialized = false;
            }
        }

        static void AppendHeaderColumnToLogFile(string filename, string columnName)
        {
            string tempfile = Path.GetTempFileName();
            using (var writer = new StreamWriter(tempfile))
            using (var reader = new StreamReader(filename))
            {
                string originalHeader = reader.ReadLine();
                writer.WriteLine(originalHeader + ";" + columnName);
               
                while (!reader.EndOfStream)
                    writer.WriteLine(reader.ReadLine());
            }

            File.Copy(tempfile, filename, true);
        }

        #region Status monitoring methods

        /// <summary>
        /// Returns the size of the logfile in megabyte.
        /// </summary>
        /// <returns>The size of the serverlog.txt file in mB.</returns>
        public static double GetServerLogfileSize()
        {
          
            if (File.Exists(logFilePath))
            {
                FileInfo f = new FileInfo(logFilePath);
                return ((double)f.Length / 1000000);
            }
            else
                return -1;
        }

        /// <summary>
        /// Returns some status messages about the logger status.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetLoggerStatus()
        {
            List<string> statusList = new List<string>();

            statusList.Add("Logfilepfad Server: " + logFilePath);
            statusList.Add("Logfile Server existiert: " + File.Exists(logFilePath).ToString());
            statusList.Add("Logfilegröße Server: " + String.Format("{0:0.00}", GetServerLogfileSize()) + "mB");
            statusList.Add("Log Level: " + LogLevel.ToString());

            return statusList;
        }
        #endregion

        #region Error log methods
        /// <summary>
        /// Saves a new log entry.
        /// </summary>
        /// <param name="category">The category of the entry.</param>
        /// <param name="message">The text to be displayed.</param>
        /// <param name="exception">An optional Exception object.</param>
        public static void AddLogEntry(LogEntryCategories category, string message, Exception exception = null, string className = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!WriteLogFile)
                return;

            #region LogLevel check
            if (LogLevel == LogLevels.None)
                return;

            if (category == LogEntryCategories.Debug || category == LogEntryCategories.Info || category == LogEntryCategories.Other)
                if (LogLevel == LogLevels.ErrorsOnly)
                    return;
            #endregion

            if (message != null)
                message = message.Replace(';', ',').Replace(Environment.NewLine, " |-> ").Replace("\"", "'"); //make sure that the message does not contain any line breaks (the current CSV reader does not support multiple multi-line columns)

            message = (string.IsNullOrEmpty(className) ? null : className + ".") + memberName + "[" + sourceLineNumber + "]: " + message;

            if (exception != null && exception.Message != null)
                exception = new Exception(exception.ToString().Replace(';', ',').Replace("\"", "'")); //prevent semicolons and apostrophe in exception messages because the exceptions are written to a CSV file

            LogLevel logLevel = NLog.LogLevel.Info;

            switch (category)
            {
                case LogEntryCategories.Debug:
                    logLevel = NLog.LogLevel.Debug;
                    break;
                case LogEntryCategories.Error:
                    logLevel = NLog.LogLevel.Error;
                    break;
                case LogEntryCategories.Fatal:
                    logLevel = NLog.LogLevel.Fatal;
                    break;
                case LogEntryCategories.Info:
                    logLevel = NLog.LogLevel.Info;
                    break;
                case LogEntryCategories.Trace:
                    logLevel = NLog.LogLevel.Trace;
                    break;
                case LogEntryCategories.Warning:
                    logLevel = NLog.LogLevel.Warn;
                    break;
                default:
                    logLevel = NLog.LogLevel.Error;
                    break;
            }

            LogEventInfo newLogEntry = new LogEventInfo(logLevel, "", message);
            newLogEntry.Properties["category"] = category.ToString(); //custom field 'category'
            newLogEntry.Properties["codeLine"] = sourceLineNumber; //custom field 'codeLine'
            newLogEntry.Exception = exception;
            applicationLogger.Log(newLogEntry);

            if (category == LogEntryCategories.Error || category == LogEntryCategories.Fatal) //only store errors
                lock (errorListLock)
                {
                    if (LatestErrors == null)
                        LatestErrors = new List<LogEntry>();

                    while (LatestErrors.Count > 1000)
                        LatestErrors.RemoveAt(0);

                    LatestErrors.Add(new ErrorLogEntry() { Category = category.ToString(), ExceptionText = exception?.ToString(), Message = message, Time = DateTime.Now, LogLevel = logLevel.ToString() });
                }

            //log event
            OnLogEvent?.Invoke(null, new ErrorLogEntry() { Category = category.ToString(), ExceptionText = exception.ToString(), Message = message, LogLevel = logLevel.ToString(), Time = DateTime.Now, CodeLine = sourceLineNumber.ToString() });
        }

        /// <summary>
        /// Returns the specified number of log entries of the specified category, starting with the latest one.
        /// </summary>
        /// <param name="category">The category of the log entries to be returned  (-1 for "all").</param>
        /// <param name="maxItems">The maximum number of items returned (-1 for "all").</param>
        /// <param name="logfilePath">For environments where "AppDomain.CurrentDomain.BaseDirectory" doesn't work reliably, the path to the nLog.csv file can be explicitly specified here.</param>
        /// <returns>A list of LogEntrys or null.</returns>
        public static List<ErrorLogEntry> GetLogEntries(LogEntryCategories category, int maxItems, DateTime startTime, DateTime endTime, string searchText, string myLogfilePath = null )
        {
            Exception ex;
            myLogfilePath = myLogfilePath ?? logFilePath;
            DataTable dtEntries = CsvHandler.ReadCSVFile(myLogfilePath, out ex);

            if (dtEntries == null || ex != null)
            {
                LastException = ex;
                return null;
            }

            bool displayAllCategories = (int)category == -1 ? true : false; //determine if all entries have to be displayed

            List<ErrorLogEntry> entriesToReturn = new List<ErrorLogEntry>();

            for (int i = dtEntries.Rows.Count - 1; i >= 0; i--) //iterate the table from bottom to top (new to old)
            {
                DateTime entryTime;
                string categoryString = category.ToString();

                if (displayAllCategories || dtEntries.Rows[i].ItemArray[2].ToString() == categoryString) //category matches the input parameter
                {
                    if (!DateTime.TryParse(dtEntries.Rows[i].ItemArray[0].ToString(), out entryTime))
                    {
                        entryTime = endTime;
                    }

                    if (entryTime >= startTime && entryTime <= endTime)
                    {
                        if (string.IsNullOrEmpty(searchText) || (dtEntries.Rows[i].ItemArray[3].ToString().Contains(searchText) || dtEntries.Rows[i].ItemArray[4].ToString().Contains(searchText)))
                        {
                            ErrorLogEntry newLogEntry = new ErrorLogEntry() { Time = entryTime, LogLevel = dtEntries.Rows[i].ItemArray[1].ToString(), Category = dtEntries.Rows[i].ItemArray[2].ToString(), Message = dtEntries.Rows[i].ItemArray[3].ToString(), ExceptionText = dtEntries.Rows[i].ItemArray[4].ToString(), CodeLine = dtEntries.Rows[i].ItemArray[5].ToString() };
                            entriesToReturn.Add(newLogEntry);
                        }
                    }
                }

                if (maxItems > 0 && entriesToReturn.Count == maxItems)
                    break;
            }

            return entriesToReturn;
        }
        #endregion

        #region User log methods

        /// <summary>
        /// Saves a new log entry to the user-friendly log.
        /// </summary>
        /// <param name="category">The custom category of the entry (has to be an enum).</param>
        /// <param name="message">The text to be displayed.</param>
        /// <param name="exception">An optional Exception object.</param>
        public static void AddUserLogEntry(Enum category, string message, Exception exception = null, string className = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!WriteUserFriendlyLogFile)
                return;

            #region LogLevel check
            if (LogLevel == LogLevels.None)
                return;
            #endregion

            if (message != null)
                message = message.Replace(';', ',').Replace(Environment.NewLine, " |-> ").Replace("\"", "'"); //make sure that the message does not contain any line breaks 

            string codeLine = className + "." + memberName + "()[" + sourceLineNumber + "]";

            if (exception != null && exception.Message != null)
                exception = new Exception(exception.ToString().Replace(';', ',').Replace("\"", "'")); //prevent semicolons and apostrophe in exception messages because the exceptions are written to a CSV file

            LogEventInfo newLogEntry = new LogEventInfo(NLog.LogLevel.Error, "", message);
            newLogEntry.Properties["category"] = category.ToString(); //custom field 'category'
            newLogEntry.Exception = exception;
            userFriendlyLogger.Log(newLogEntry);

            //log event
            OnUserLogEvent?.Invoke(null, new UserLogEntry() { Category = category.ToString(), ExceptionText = exception.ToString(), Message = message, Time = DateTime.Now });
        }

        /// <summary>
        /// Returns the specified number of log entries, starting with the latest one.
        /// </summary>
        /// <param name="maxItems">The maximum number of items returned (-1 for "all").</param>
        /// <returns>A list of LogEntrys or null.</returns>
        public static List<UserLogEntry> GetUserLogEntries(int maxItems, DateTime startTime, DateTime endTime)
        {
            Exception ex;
            DataTable dtEntries = CsvHandler.ReadCSVFile(Environment.CurrentDirectory + @"\Logs\nUserLog.csv", out ex);

            if (dtEntries == null || ex != null)
                return null;

            List<UserLogEntry> entriesToReturn = new List<UserLogEntry>();

            for (int i = dtEntries.Rows.Count - 1; i >= 0; i--) //iterate the table from bottom to top (new to old)
            {
                DateTime entryTime;

                if (!DateTime.TryParse(dtEntries.Rows[i].ItemArray[0].ToString(), out entryTime))
                {
                    entryTime = endTime;
                }

                if (entryTime >= startTime && entryTime <= endTime)
                {
                    UserLogEntry newLogEntry = new UserLogEntry() { Time = entryTime, Category = dtEntries.Rows[i].ItemArray[1].ToString(), Message = dtEntries.Rows[i].ItemArray[2].ToString(), ExceptionText = dtEntries.Rows[i].ItemArray[3].ToString() };
                    entriesToReturn.Add(newLogEntry);
                }

                if (maxItems > 0 && entriesToReturn.Count == maxItems)
                    break;
            }

            return entriesToReturn;
        }
        #endregion
    }

    /// <summary>
    /// Base class for all log entry types.
    /// </summary>
    public abstract class LogEntry
    {
        public string Category { get; set; }
        public string Message { get; set; }
        public string ExceptionText { get; set; }
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return $"{Time} [{Category}]: {Message} --> {Environment.NewLine}{ExceptionText}";
        }
    }

    /// <summary>
    /// Represents a single log entry as used in the error logger.
    /// </summary>
    public class ErrorLogEntry : LogEntry
    {
        public string LogLevel { get; set; }
        public string CodeLine { get; set; }
    }

    /// <summary>
    /// Represents a single log entry as used in the user logger.
    /// </summary>
    public class UserLogEntry : LogEntry
    {

    }
}
