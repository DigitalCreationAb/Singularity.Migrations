using System;

namespace Singularity.Migrations.Logging
{
    public interface IMigrationLogger
    {
        void Trace(string message, params object[] formatArgs);

        void Trace(Exception error, string message, params object[] formatArgs);

        void Debug(string message, params object[] formatArgs);

        void Debug(Exception error, string message, params object[] formatArgs);

        void Information(string message, params object[] formatArgs);

        void Information(Exception error, string message, params object[] formatArgs);

        void Warning(string message, params object[] formatArgs);

        void Warning(Exception error, string message, params object[] formatArgs);

        void Error(string message, params object[] formatArgs);

        void Error(Exception error, string message, params object[] formatArgs);

        void Fatal(string message, params object[] formatArgs);

        void Fatal(Exception error, string message, params object[] formatArgs);
    }
}