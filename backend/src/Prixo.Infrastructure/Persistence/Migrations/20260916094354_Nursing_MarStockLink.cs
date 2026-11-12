using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Nursing_MarStockLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "batch_id",
                schema: "nursing",
                table: "mar_administrations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                schema: "nursing",
                table: "mar_administrations",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mar_administrations_batch_id",
                schema: "nursing",
                table: "mar_administrations",
                column: "batch_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mar_administrations_batches_batch_id",
                schema: "nursing",
                table: "mar_administrations",
                column: "batch_id",
                principalSchema: "pharmacy",
                principalTable: "batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mar_administrations_batches_batch_id",
                schema: "nursing",
                table: "mar_administrations");

            migrationBuilder.DropIndex(
                name: "ix_mar_administrations_batch_id",
                schema: "nursing",
                table: "mar_administrations");

            migrationBuilder.DropColumn(
                name: "batch_id",
                schema: "nursing",
                table: "mar_administrations");

            migrationBuilder.DropColumn(
                name: "quantity",
                schema: "nursing",
                table: "mar_administrations");
        }
    }
}
