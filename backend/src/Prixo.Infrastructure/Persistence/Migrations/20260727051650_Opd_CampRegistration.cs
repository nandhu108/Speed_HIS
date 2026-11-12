using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Opd_CampRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "camp_id",
                schema: "opd",
                table: "op_visits",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_camp_id",
                schema: "opd",
                table: "op_visits",
                column: "camp_id");

            migrationBuilder.AddForeignKey(
                name: "fk_op_visits_camps_camp_id",
                schema: "opd",
                table: "op_visits",
                column: "camp_id",
                principalSchema: "master",
                principalTable: "camps",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_op_visits_camps_camp_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_camp_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "camp_id",
                schema: "opd",
                table: "op_visits");
        }
    }
}
