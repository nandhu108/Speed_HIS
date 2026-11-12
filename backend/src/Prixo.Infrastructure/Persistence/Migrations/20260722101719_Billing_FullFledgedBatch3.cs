using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Billing_FullFledgedBatch3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "cashier_shift_id",
                schema: "billing",
                table: "bill_payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cashier_shifts",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    opening_float = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    received_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cashier_shifts", x => x.id);
                    table.ForeignKey(
                        name: "fk_cashier_shifts_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cashier_shifts_users_received_by_user_id",
                        column: x => x.received_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cashier_shifts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cashier_shift_mode_amounts",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cashier_shift_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_mode_id = table.Column<long>(type: "bigint", nullable: false),
                    system_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    declared_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    variance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cashier_shift_mode_amounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_cashier_shift_mode_amounts_cashier_shifts_cashier_shift_id",
                        column: x => x.cashier_shift_id,
                        principalSchema: "billing",
                        principalTable: "cashier_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cashier_shift_mode_amounts_payment_modes_payment_mode_id",
                        column: x => x.payment_mode_id,
                        principalSchema: "billing",
                        principalTable: "payment_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bill_payments_cashier_shift_id",
                schema: "billing",
                table: "bill_payments",
                column: "cashier_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shift_mode_amounts_cashier_shift_id_payment_mode_id",
                schema: "billing",
                table: "cashier_shift_mode_amounts",
                columns: new[] { "cashier_shift_id", "payment_mode_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shift_mode_amounts_payment_mode_id",
                schema: "billing",
                table: "cashier_shift_mode_amounts",
                column: "payment_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shifts_branch_id_user_id_status",
                schema: "billing",
                table: "cashier_shifts",
                columns: new[] { "branch_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shifts_received_by_user_id",
                schema: "billing",
                table: "cashier_shifts",
                column: "received_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shifts_user_id",
                schema: "billing",
                table: "cashier_shifts",
                column: "user_id",
                unique: true,
                filter: "is_deleted = false AND status = 1");

            migrationBuilder.AddForeignKey(
                name: "fk_bill_payments_cashier_shifts_cashier_shift_id",
                schema: "billing",
                table: "bill_payments",
                column: "cashier_shift_id",
                principalSchema: "billing",
                principalTable: "cashier_shifts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bill_payments_cashier_shifts_cashier_shift_id",
                schema: "billing",
                table: "bill_payments");

            migrationBuilder.DropTable(
                name: "cashier_shift_mode_amounts",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "cashier_shifts",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "ix_bill_payments_cashier_shift_id",
                schema: "billing",
                table: "bill_payments");

            migrationBuilder.DropColumn(
                name: "cashier_shift_id",
                schema: "billing",
                table: "bill_payments");
        }
    }
}
