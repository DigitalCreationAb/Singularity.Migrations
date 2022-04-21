using System.Threading.Tasks;

namespace Singularity.Migrations;

public interface IMigration<in TContext>
{
    long Version { get; }
    Task Up(TContext context);
    Task Down(TContext context);
}