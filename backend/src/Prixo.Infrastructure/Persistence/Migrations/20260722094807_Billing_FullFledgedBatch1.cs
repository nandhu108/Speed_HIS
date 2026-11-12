using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Billing_FullFledgedBatch1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "credit_limit",
                schema: "ipd",
                table: "admissions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bill_credit_notes",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    credit_note_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    credit_note_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_credit_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_credit_notes_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_notes",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_notes_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_credit_note_items",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_credit_note_id = table.Column<long>(type: "bigint", nullable: false),
                    bill_item_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_credit_note_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_credit_note_items_bill_credit_notes_bill_credit_note_id",
                        column: x => x.bill_credit_note_id,
                        principalSchema: "billing",
                        principalTable: "bill_credit_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_credit_note_items_bill_items_bill_item_id",
                        column: x => x.bill_item_id,
                        principalSchema: "billing",
                        principalTable: "bill_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bill_credit_note_items_bill_credit_note_id",
                schema: "billing",
                table: "bill_credit_note_items",
                column: "bill_credit_note_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_credit_note_items_bill_item_id",
                schema: "billing",
                table: "bill_credit_note_items",
                column: "bill_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_credit_notes_bill_id",
                schema: "billing",
                table: "bill_credit_notes",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_credit_notes_credit_note_number",
                schema: "billing",
                table: "bill_credit_notes",
                column: "credit_note_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_bill_credit_notes_status",
                schema: "billing",
                table: "bill_credit_notes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_bill_notes_bill_id",
                schema: "billing",
                table: "bill_notes",
                column: "bill_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bill_credit_note_items",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "bill_notes",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "bill_credit_notes",
                schema: "billing");

            migrationBuilder.DropColumn(
                name: "credit_limit",
                schema: "ipd",
                table: "admissions");
        }
    }
}
