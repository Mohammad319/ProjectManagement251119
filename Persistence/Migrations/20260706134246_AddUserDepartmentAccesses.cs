using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDepartmentAccesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDepartmentAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDepartmentAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDepartmentAccesses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDepartmentAccesses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDepartmentAccesses_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDepartmentAccesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO UserDepartmentAccesses (UserId, DepartmentId, IsPrimary, TenantId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                SELECT u.Id, u.DepartmentId, CAST(1 AS bit), u.TenantId, SYSUTCDATETIME(), NULL, NULL, NULL
                FROM Users u
                INNER JOIN Departments d ON d.Id = u.DepartmentId AND d.TenantId = u.TenantId
                WHERE u.DepartmentId IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM UserDepartmentAccesses existing
                      WHERE existing.TenantId = u.TenantId
                        AND existing.UserId = u.Id
                        AND existing.DepartmentId = u.DepartmentId
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentAccesses_CreatedBy",
                table: "UserDepartmentAccesses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentAccesses_DepartmentId",
                table: "UserDepartmentAccesses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentAccesses_Tenant_Department",
                table: "UserDepartmentAccesses",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentAccesses_UpdatedBy",
                table: "UserDepartmentAccesses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentAccesses_UserId",
                table: "UserDepartmentAccesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_UserDepartmentAccesses_Tenant_User_Department",
                table: "UserDepartmentAccesses",
                columns: new[] { "TenantId", "UserId", "DepartmentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDepartmentAccesses");
        }
    }
}
