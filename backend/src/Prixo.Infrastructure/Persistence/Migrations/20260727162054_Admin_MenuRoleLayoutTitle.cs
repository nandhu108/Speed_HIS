using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Admin_MenuRoleLayoutTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "admin",
                table: "menu_role_layouts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "title",
                schema: "admin",
                table: "menu_role_layouts");
        }
    }
}
