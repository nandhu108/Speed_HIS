using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase13_StoresInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_batches_drug_id_store_id_batch_number",
                schema: "pharmacy",
                table: "batches");

            migrationBuilder.AddColumn<int>(
                name: "store_type",
                schema: "pharmacy",
                table: "stores",
                type: "integer",
                nullable: false,
                defaultValue: 1); // StoreType.Pharmacy — every pre-Phase-13 store was a pharmacy store

            migrationBuilder.AlterColumn<long>(
                name: "drug_id",
                schema: "pharmacy",
                table: "purchase_request_items",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "item_id",
                schema: "pharmacy",
                table: "purchase_request_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "drug_id",
                schema: "pharmacy",
                table: "purchase_order_items",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "item_id",
                schema: "pharmacy",
                table: "purchase_order_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "indent_id",
                schema: "pharmacy",
                table: "issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "mrp",
                schema: "pharmacy",
                table: "grn_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "expiry_date",
                schema: "pharmacy",
                table: "grn_items",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<long>(
                name: "drug_id",
                schema: "pharmacy",
                table: "grn_items",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "item_id",
                schema: "pharmacy",
                table: "grn_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "mrp",
                schema: "pharmacy",
                table: "batches",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "expiry_date",
                schema: "pharmacy",
                table: "batches",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<long>(
                name: "drug_id",
                schema: "pharmacy",
                table: "batches",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "item_id",
                schema: "pharmacy",
                table: "batches",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "indents",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    department_id = table.Column<long>(type: "bigint", nullable: false),
                    indent_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    indent_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_indents", x => x.id);
                    table.ForeignKey(
                        name: "fk_indents_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_indents_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "master",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_indents_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_indents_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_indents_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "indent_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    indent_id = table.Column<long>(type: "bigint", nullable: false),
                    drug_id = table.Column<long>(type: "bigint", nullable: true),
                    item_id = table.Column<long>(type: "bigint", nullable: true),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    approved_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_indent_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_indent_items_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_indent_items_indents_indent_id",
                        column: x => x.indent_id,
                        principalSchema: "pharmacy",
                        principalTable: "indents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_indent_items_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "master",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_items_item_id",
                schema: "pharmacy",
                table: "purchase_request_items",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_item_id",
                schema: "pharmacy",
                table: "purchase_order_items",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_indent_id",
                schema: "pharmacy",
                table: "issues",
                column: "indent_id");

            migrationBuilder.CreateIndex(
                name: "ix_grn_items_item_id",
                schema: "pharmacy",
                table: "grn_items",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_drug_id_store_id_batch_number",
                schema: "pharmacy",
                table: "batches",
                columns: new[] { "drug_id", "store_id", "batch_number" },
                unique: true,
                filter: "is_deleted = false AND drug_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_batches_item_id_store_id_batch_number",
                schema: "pharmacy",
                table: "batches",
                columns: new[] { "item_id", "store_id", "batch_number" },
                unique: true,
                filter: "is_deleted = false AND item_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_indent_items_drug_id",
                schema: "pharmacy",
                table: "indent_items",
                column: "drug_id");

            migrationBuilder.CreateIndex(
                name: "ix_indent_items_indent_id",
                schema: "pharmacy",
                table: "indent_items",
                column: "indent_id");

            migrationBuilder.CreateIndex(
                name: "ix_indent_items_item_id",
                schema: "pharmacy",
                table: "indent_items",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_indents_branch_id",
                schema: "pharmacy",
                table: "indents",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_indents_decided_by_user_id",
                schema: "pharmacy",
                table: "indents",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_indents_department_id",
                schema: "pharmacy",
                table: "indents",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_indents_indent_number",
                schema: "pharmacy",
                table: "indents",
                column: "indent_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_indents_requested_by_user_id",
                schema: "pharmacy",
                table: "indents",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_indents_status",
                schema: "pharmacy",
                table: "indents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_indents_store_id",
                schema: "pharmacy",
                table: "indents",
                column: "store_id");

            migrationBuilder.AddForeignKey(
                name: "fk_batches_items_item_id",
                schema: "pharmacy",
                table: "batches",
                column: "item_id",
                principalSchema: "master",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_grn_items_items_item_id",
                schema: "pharmacy",
                table: "grn_items",
                column: "item_id",
                principalSchema: "master",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_issues_indents_indent_id",
                schema: "pharmacy",
                table: "issues",
                column: "indent_id",
                principalSchema: "pharmacy",
                principalTable: "indents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_order_items_items_item_id",
                schema: "pharmacy",
                table: "purchase_order_items",
                column: "item_id",
                principalSchema: "master",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_request_items_items_item_id",
                schema: "pharmacy",
                table: "purchase_request_items",
                column: "item_id",
                principalSchema: "master",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_batches_items_item_id",
                schema: "pharmacy",
                table: "batches");

            migrationBuilder.DropForeignKey(
                name: "fk_grn_items_items_item_id",
                schema: "pharmacy",
                table: "grn_items");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_indents_indent_id",
                schema: "pharmacy",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_order_items_items_item_id",
                schema: "pharmacy",
                table: "purchase_order_items");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_request_items_items_item_id",
                schema: "pharmacy",
                table: "purchase_request_items");

            migrationBuilder.DropTable(
                name: "indent_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "indents",
                schema: "pharmacy");

            migrationBuilder.DropIndex(
                name: "ix_purchase_request_items_item_id",
                schema: "pharmacy",
                table: "purchase_request_items");

            migrationBuilder.DropIndex(
                name: "ix_purchase_order_items_item_id",
                schema: "pharmacy",
                table: "purchase_order_items");

            migrationBuilder.DropIndex(
                name: "ix_issues_indent_id",
                schema: "pharmacy",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_grn_items_item_id",
                schema: "pharmacy",
                table: "grn_items");

            migrationBuilder.DropIndex(
                name: "ix_batches_drug_id_store_id_batch_number",
                schema: "pharmacy",
                table: "batches");

            migrationBuilder.DropIndex(
                name: "ix_batches_item_id_store_id_batch_number",
                schema: "pharmacy",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "store_type",
                schema: "pharmacy",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "item_id",
                schema: "pharmacy",
                table: "purchase_request_items");

            migrationBuilder.DropColumn(
                name: "item_id",
                schema: "pharmacy",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "indent_id",
                schema: "pharmacy",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "item_id",
                schema: "pharmacy",
                table: "grn_items");

            migrationBuilder.DropColumn(
                name: "item_id",
                schema: "pharmacy",
                table: "batches");

            migrationBuilder.AlterColumn<long>(
                name: "drug_id",
                schema: "pharmacy",
                table: "purchase_request_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "drug_id",
                schema: "pharmacy",
                table: "purchase_order_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "mrp",
                schema: "pharmacy",
                table: "grn_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "expiry_date",
                schema: "pharmacy",
                table: "grn_items",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "drug_id",
                schema: "pharmacy",
                table: "grn_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "mrp",
                schema: "pharmacy",
                table: "batches",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "expiry_date",
                schema: "pharmacy",
                table: "batches",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "drug_id",
                schema: "pharmacy",
                table: "batches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_batches_drug_id_store_id_batch_number",
                schema: "pharmacy",
                table: "batches",
                columns: new[] { "drug_id", "store_id", "batch_number" },
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
