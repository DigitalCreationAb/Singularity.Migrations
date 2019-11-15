using System.Data.SqlClient;

namespace Singularity.Migrations.Coordinators.SqlServer
{
    public interface ISqlServerMigrationContext
    {
        string Key { get; }
        string MigrationTableName { get; }
        SqlConnection Connection { get; }
    }
}