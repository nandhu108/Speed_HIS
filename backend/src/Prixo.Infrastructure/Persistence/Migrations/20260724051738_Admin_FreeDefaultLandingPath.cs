using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Admin_FreeDefaultLandingPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "front_office_landing_mode",
                schema: "admin",
                table: "branches");

            migrationBuilder.AddColumn<string>(
                name: "default_landing_path",
                schema: "admin",
                table: "roles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_landing_path",
                schema: "admin",
                table: "branches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_landing_path",
                schema: "admin",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "default_landing_path",
                schema: "admin",
                table: "branches");

            migrationBuilder.AddColumn<int>(
                name: "front_office_landing_mode",
                schema: "admin",
                table: "branches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
