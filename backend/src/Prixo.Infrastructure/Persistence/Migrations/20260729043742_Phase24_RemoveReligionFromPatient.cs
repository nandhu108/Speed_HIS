using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase24_RemoveReligionFromPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_patients_religions_religion_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_religion_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "religion_id",
                schema: "patient",
                table: "patients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "religion_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_patients_religion_id",
                schema: "patient",
                table: "patients",
                column: "religion_id");

            migrationBuilder.AddForeignKey(
                name: "fk_patients_religions_religion_id",
                schema: "patient",
                table: "patients",
                column: "religion_id",
                principalSchema: "master",
                principalTable: "religions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
