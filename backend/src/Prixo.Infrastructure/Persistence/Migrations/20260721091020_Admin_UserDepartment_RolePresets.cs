using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Admin_UserDepartment_RolePresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "department_id",
                schema: "admin",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "role_presets",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_presets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_preset_roles",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_preset_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_preset_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_preset_roles_role_presets_role_preset_id",
                        column: x => x.role_preset_id,
                        principalSchema: "admin",
                        principalTable: "role_presets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_preset_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "admin",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_department_id",
                schema: "admin",
                table: "users",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_preset_roles_role_id",
                schema: "admin",
                table: "role_preset_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_preset_roles_role_preset_id_role_id",
                schema: "admin",
                table: "role_preset_roles",
                columns: new[] { "role_preset_id", "role_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_role_presets_name",
                schema: "admin",
                table: "role_presets",
                column: "name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_users_departments_department_id",
                schema: "admin",
                table: "users",
                column: "department_id",
                principalSchema: "master",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_departments_department_id",
                schema: "admin",
                table: "users");

            migrationBuilder.DropTable(
                name: "role_preset_roles",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "role_presets",
                schema: "admin");

            migrationBuilder.DropIndex(
                name: "ix_users_department_id",
                schema: "admin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "admin",
                table: "users");
        }
    }
}
