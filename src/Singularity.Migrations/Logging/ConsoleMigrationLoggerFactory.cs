using System;

namespace Singularity.Migrations.Logging
{
    public class ConsoleMigrationLoggerFactory : IMigrationLoggerFactory
    {
        public IMigrationLogger CreateLogger(Type source)
        {
            return new ConsoleMigrationLogger();
        }
    }
}