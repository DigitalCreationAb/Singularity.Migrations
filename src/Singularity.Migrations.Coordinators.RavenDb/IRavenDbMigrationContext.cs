using Raven.Client.Documents;

namespace Singularity.Migrations.Coordinators.RavenDb;

public interface IRavenDbMigrationContext
{
    string ProjectId { get; }

    string MigrationDbName { get; }

    IDocumentStore DocumentStore { get; }
}