using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Context;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ShardingSingleDbContext))]
    [Migration("20260305111500_OfferComputedMoneyColumns")]
    public partial class OfferComputedMoneyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add persisted computed columns based on the JSON stored in Offers.Metadata
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Offers', 'CostValue') IS NULL
    ALTER TABLE [dbo].[Offers] ADD [CostValue] AS TRY_CONVERT(decimal(18,2), JSON_VALUE([Metadata], '$.Cost')) PERSISTED;

IF COL_LENGTH('dbo.Offers', 'BaseCostValue') IS NULL
    ALTER TABLE [dbo].[Offers] ADD [BaseCostValue] AS TRY_CONVERT(decimal(18,2), JSON_VALUE([Metadata], '$.BaseCost')) PERSISTED;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Offers_Tenant_CostValue' AND object_id = OBJECT_ID('dbo.Offers'))
    CREATE INDEX [IX_Offers_Tenant_CostValue] ON [dbo].[Offers]([TenantId],[CostValue]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Offers_Tenant_BaseCostValue' AND object_id = OBJECT_ID('dbo.Offers'))
    CREATE INDEX [IX_Offers_Tenant_BaseCostValue] ON [dbo].[Offers]([TenantId],[BaseCostValue]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Offers_Tenant_CostValue' AND object_id = OBJECT_ID('dbo.Offers'))
    DROP INDEX [IX_Offers_Tenant_CostValue] ON [dbo].[Offers];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Offers_Tenant_BaseCostValue' AND object_id = OBJECT_ID('dbo.Offers'))
    DROP INDEX [IX_Offers_Tenant_BaseCostValue] ON [dbo].[Offers];

IF COL_LENGTH('dbo.Offers', 'CostValue') IS NOT NULL
    ALTER TABLE [dbo].[Offers] DROP COLUMN [CostValue];

IF COL_LENGTH('dbo.Offers', 'BaseCostValue') IS NOT NULL
    ALTER TABLE [dbo].[Offers] DROP COLUMN [BaseCostValue];
");
        }
    }
}
