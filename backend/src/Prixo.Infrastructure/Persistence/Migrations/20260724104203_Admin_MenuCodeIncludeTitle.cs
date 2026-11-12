using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Admin_MenuCodeIncludeTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DbSeeder.EnsureMenuAsync's Code formula now always includes Title (previously
            // Path alone for leaf items) — two different seeded items can legitimately share a
            // Path (e.g. "IP Registration" and "Admissions" both open /ipd/admissions), which
            // used to collide and silently skip the second insert. Recompute existing seeded
            // rows (Code IS NOT NULL — admin-created rows stay NULL, untouched) to match, so
            // the next seeder run still finds them instead of inserting duplicates.
            migrationBuilder.Sql("""
                UPDATE admin.menus
                SET code = COALESCE(path, parent_id::text, 'root') || '::' || title
                WHERE code IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
