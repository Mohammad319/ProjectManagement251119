using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Context;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ShardingSingleDbContext))]
    [Migration("20260305113000_TaskTenantParentIndex")]
    public partial class TaskTenantParentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_Tenant_Parent' AND object_id = OBJECT_ID('dbo.Tasks'))
    CREATE INDEX [IX_Tasks_Tenant_Parent] ON [dbo].[Tasks]([TenantId],[ParentTaskId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_Tenant_Parent' AND object_id = OBJECT_ID('dbo.Tasks'))
    DROP INDEX [IX_Tasks_Tenant_Parent] ON [dbo].[Tasks];
");
        }
    }
}
