using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Admin_MenuRoleLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "menu_role_layouts",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    menu_id = table.Column<long>(type: "bigint", nullable: false),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_role_layouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_role_layouts_menus_menu_id",
                        column: x => x.menu_id,
                        principalSchema: "admin",
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_menu_role_layouts_menus_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "admin",
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_menu_role_layouts_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "admin",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_menu_role_layouts_menu_id",
                schema: "admin",
                table: "menu_role_layouts",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_role_layouts_parent_id",
                schema: "admin",
                table: "menu_role_layouts",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_role_layouts_role_id_menu_id",
                schema: "admin",
                table: "menu_role_layouts",
                columns: new[] { "role_id", "menu_id" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_role_layouts",
                schema: "admin");
        }
    }
}
