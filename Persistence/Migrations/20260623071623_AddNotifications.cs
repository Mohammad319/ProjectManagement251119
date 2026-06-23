using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepartmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CalcCount = table.Column<int>(type: "int", nullable: false),
                    CalcAllAvailable = table.Column<bool>(type: "bit", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "date", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedBy",
                table: "Notifications",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Tenant_User_Created",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Tenant_User_Read",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UpdatedBy",
                table: "Notifications",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
