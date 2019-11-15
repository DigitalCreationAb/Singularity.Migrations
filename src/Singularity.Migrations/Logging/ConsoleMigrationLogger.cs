using System;

namespace Singularity.Migrations.Logging
{
    public class ConsoleMigrationLogger : IMigrationLogger
    {
        public void Trace(string message, params object[] formatArgs)
        {
            Trace(null, message, formatArgs);
        }

        public void Trace(Exception error, string message, params object[] formatArgs)
        {
            Write(error, message, formatArgs, "trace");
        }

        public void Debug(string message, params object[] formatArgs)
        {
            Debug(null, message, formatArgs);
        }

        public void Debug(Exception error, string message, params object[] formatArgs)
        {
            Write(error, message, formatArgs, "debug");
        }

        public void Information(string message, params object[] formatArgs)
        {
            Information(null, message, formatArgs);
        }

        public void Information(Exception error, string message, params object[] formatArgs)
        {
            Write(error, message, formatArgs, "information");
        }

        public void Warning(string message, params object[] formatArgs)
        {
            Warning(null, message, formatArgs);
        }

        public void Warning(Exception error, string message, params object[] formatArgs)
        {
            Write(error, message, formatArgs, "warning");
        }

        public void Error(string message, params object[] formatArgs)
        {
            Error(null, message, formatArgs);
        }

        public void Error(Exception error, string message, params object[] formatArgs)
        {
            Write(error, message, formatArgs, nameof(error));
        }

        public void Fatal(string message, params object[] formatArgs)
        {
            Fatal(null, message, formatArgs);
        }

        public void Fatal(Exception error, string message, params object[] formatArgs)
        {
            Write(error, message, formatArgs, "fatal");
        }

        private static void Write(
            Exception error,
            string message,
            object[] formatArgs,
            string logLevel)
        {
            Console.WriteLine("[" + logLevel + "] - " + message, formatArgs);
            
            for (var exception = error; exception != null; exception = exception.InnerException)
            {
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.StackTrace);
            }
        }
    }
}