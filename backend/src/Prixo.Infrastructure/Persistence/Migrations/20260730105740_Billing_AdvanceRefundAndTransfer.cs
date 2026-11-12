using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Billing_AdvanceRefundAndTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "bill_id",
                schema: "billing",
                table: "refunds",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "advance_id",
                schema: "billing",
                table: "refunds",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "issued_at",
                schema: "billing",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "issued_by_user_id",
                schema: "billing",
                table: "refunds",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_refunds_advance_id",
                schema: "billing",
                table: "refunds",
                column: "advance_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_refunds_bill_or_advance",
                schema: "billing",
                table: "refunds",
                sql: "(bill_id IS NOT NULL AND advance_id IS NULL) OR (bill_id IS NULL AND advance_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_refunds_advances_advance_id",
                schema: "billing",
                table: "refunds",
                column: "advance_id",
                principalSchema: "billing",
                principalTable: "advances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_refunds_advances_advance_id",
                schema: "billing",
                table: "refunds");

            migrationBuilder.DropIndex(
                name: "ix_refunds_advance_id",
                schema: "billing",
                table: "refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_refunds_bill_or_advance",
                schema: "billing",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "advance_id",
                schema: "billing",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "issued_at",
                schema: "billing",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "issued_by_user_id",
                schema: "billing",
                table: "refunds");

            migrationBuilder.AlterColumn<long>(
                name: "bill_id",
                schema: "billing",
                table: "refunds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
