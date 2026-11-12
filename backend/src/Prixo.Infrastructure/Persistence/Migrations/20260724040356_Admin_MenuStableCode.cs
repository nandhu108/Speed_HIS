using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Admin_MenuStableCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "admin",
                table: "menus",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            // Backfill existing seeded rows with the same formula DbSeeder.EnsureMenuAsync now
            // uses (path if set, else "{parentId}::{title}"), so the very next seeder run finds
            // them by Code instead of re-inserting duplicates.
            migrationBuilder.Sql("""
                UPDATE admin.menus
                SET code = COALESCE(path, COALESCE(parent_id::text, 'root') || '::' || title)
                WHERE code IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_menus_code",
                schema: "admin",
                table: "menus",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_menus_code",
                schema: "admin",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "admin",
                table: "menus");
        }
    }
}
