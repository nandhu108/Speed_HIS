using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Radiology_BillItemLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "bill_item_id",
                schema: "rad",
                table: "rad_order_items",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bill_item_id",
                schema: "rad",
                table: "rad_order_items");
        }
    }
}
