using System;
using System.Diagnostics;
using System.Text;

namespace Kentico.ADImportService
{
    /// <summary>
    /// Listener logger that enables to log to event log.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Event log object to log to.
        /// </summary>
        public EventLog EventLog
        {
            get; 
            private set; 
        }


        /// <summary>
        /// Default constructor for Logger.
        /// </summary>
        /// <param name="eventLog"></param>
        public Logger(EventLog eventLog)
        {
            EventLog = eventLog;
        }


        /// <summary>
        /// Log message to event log.
        /// </summary>
        /// <param name="message">Message text</param>
        public void LogMessage(string message)
        {
            if (!string.IsNullOrEmpty(message) && (EventLog != null))
            {
                EventLog.WriteEntry(message, EventLogEntryType.Information);
            }
        }


        /// <summary>
        /// Log error to event log.
        /// </summary>
        /// <param name="message">Message text</param>
        /// <param name="e">Exception that occured</param>
        public void LogError(string message, Exception e = null)
        {
            if (!string.IsNullOrEmpty(message) && (EventLog != null))
            {
                var error = new StringBuilder();
                
                error.AppendLine(message);

                if (e != null)
                {
                    // Append exception
                    var mainException = GetExceptionString(e);

                    if (!string.IsNullOrEmpty(mainException))
                    {
                        error.AppendLine(mainException);
                    }

                    var innerException = e.InnerException;

                    while (innerException != null)
                    {
                        // Append inner exception
                        var innerExceptionString = GetExceptionString(innerException);

                        if (!string.IsNullOrEmpty(innerExceptionString))
                        {
                            error.AppendLine("Inner Exception:");
                            error.AppendLine(innerExceptionString);
                        }

                        innerException = innerException.InnerException;
                    }
                }

                EventLog.WriteEntry(error.ToString(), EventLogEntryType.Error);
            }
        }


        /// <summary>
        /// Get pretty print of exception.
        /// </summary>
        /// <param name="e">Exception to print</param>
        private string GetExceptionString(Exception e)
        {
            if (e != null)
            {
                var error = new StringBuilder();

                if (!string.IsNullOrEmpty(e.Message))
                {
                    error.AppendLine("Exception message: ");
                    error.AppendLine(e.Message);
                }

                if (!string.IsNullOrEmpty(e.StackTrace))
                {
                    error.AppendLine("Stack trace: ");
                    error.AppendLine(e.StackTrace);
                }

                return error.ToString();
            }

            return null;
        }
    }
}
