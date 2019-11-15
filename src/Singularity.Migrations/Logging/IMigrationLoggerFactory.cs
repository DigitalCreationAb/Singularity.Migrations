using System;

namespace Singularity.Migrations.Logging
{
    public interface IMigrationLoggerFactory
    {
        IMigrationLogger CreateLogger(Type source);
    }
}