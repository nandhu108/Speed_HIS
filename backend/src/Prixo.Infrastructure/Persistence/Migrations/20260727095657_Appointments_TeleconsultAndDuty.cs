using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Appointments_TeleconsultAndDuty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "consultation_mode",
                schema: "appointment",
                table: "appointments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "teleconsult_link",
                schema: "appointment",
                table: "appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consultation_mode",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "teleconsult_link",
                schema: "appointment",
                table: "appointments");
        }
    }
}
