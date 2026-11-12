using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase18_Finance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.AddColumn<long>(
                name: "linked_account_id",
                schema: "billing",
                table: "payment_modes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<int>(type: "integer", nullable: false),
                    parent_account_id = table.Column<long>(type: "bigint", nullable: true),
                    is_system_account = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounts_accounts_parent_account_id",
                        column: x => x.parent_account_id,
                        principalSchema: "finance",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payables",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    grn_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    invoice_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_payables", x => x.id);
                    table.ForeignKey(
                        name: "fk_payables_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payables_grns_grn_id",
                        column: x => x.grn_id,
                        principalSchema: "pharmacy",
                        principalTable: "grns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payables_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "master",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receivables",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    corporate_id = table.Column<long>(type: "bigint", nullable: false),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    bill_date = table.Column<DateOnly>(type: "date", nullable: false),
                    bill_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    collected_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_receivables", x => x.id);
                    table.ForeignKey(
                        name: "fk_receivables_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receivables_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receivables_corporates_corporate_id",
                        column: x => x.corporate_id,
                        principalSchema: "master",
                        principalTable: "corporates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vouchers",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    voucher_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    voucher_type = table.Column<int>(type: "integer", nullable: false),
                    voucher_date = table.Column<DateOnly>(type: "date", nullable: false),
                    narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    posted_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reversal_of_voucher_id = table.Column<long>(type: "bigint", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_vouchers", x => x.id);
                    table.ForeignKey(
                        name: "fk_vouchers_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vouchers_users_posted_by_user_id",
                        column: x => x.posted_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vouchers_vouchers_reversal_of_voucher_id",
                        column: x => x.reversal_of_voucher_id,
                        principalSchema: "finance",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "voucher_lines",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    voucher_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<long>(type: "bigint", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    credit_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    narration = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voucher_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_voucher_lines_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "finance",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_voucher_lines_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalSchema: "finance",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    voucher_id = table.Column<long>(type: "bigint", nullable: false),
                    voucher_line_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<long>(type: "bigint", nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    credit_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    narration = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("pk_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_ledger_entries_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "finance",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_voucher_lines_voucher_line_id",
                        column: x => x.voucher_line_id,
                        principalSchema: "finance",
                        principalTable: "voucher_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalSchema: "finance",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_modes_linked_account_id",
                schema: "billing",
                table: "payment_modes",
                column: "linked_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_accounts_code",
                schema: "finance",
                table: "accounts",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_accounts_parent_account_id",
                schema: "finance",
                table: "accounts",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_account_id_entry_date",
                schema: "finance",
                table: "ledger_entries",
                columns: new[] { "account_id", "entry_date" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_branch_id",
                schema: "finance",
                table: "ledger_entries",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_voucher_id",
                schema: "finance",
                table: "ledger_entries",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_voucher_line_id",
                schema: "finance",
                table: "ledger_entries",
                column: "voucher_line_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_payables_branch_id_status",
                schema: "finance",
                table: "payables",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payables_grn_id",
                schema: "finance",
                table: "payables",
                column: "grn_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_payables_vendor_id",
                schema: "finance",
                table: "payables",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_receivables_bill_id",
                schema: "finance",
                table: "receivables",
                column: "bill_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_receivables_branch_id_status",
                schema: "finance",
                table: "receivables",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_receivables_corporate_id",
                schema: "finance",
                table: "receivables",
                column: "corporate_id");

            migrationBuilder.CreateIndex(
                name: "ix_voucher_lines_account_id",
                schema: "finance",
                table: "voucher_lines",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_voucher_lines_voucher_id",
                schema: "finance",
                table: "voucher_lines",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_branch_id_voucher_date_voucher_type",
                schema: "finance",
                table: "vouchers",
                columns: new[] { "branch_id", "voucher_date", "voucher_type" });

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_posted_by_user_id",
                schema: "finance",
                table: "vouchers",
                column: "posted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_reversal_of_voucher_id",
                schema: "finance",
                table: "vouchers",
                column: "reversal_of_voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_voucher_number",
                schema: "finance",
                table: "vouchers",
                column: "voucher_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_modes_accounts_linked_account_id",
                schema: "billing",
                table: "payment_modes",
                column: "linked_account_id",
                principalSchema: "finance",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_modes_accounts_linked_account_id",
                schema: "billing",
                table: "payment_modes");

            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "payables",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "receivables",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "voucher_lines",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "vouchers",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "ix_payment_modes_linked_account_id",
                schema: "billing",
                table: "payment_modes");

            migrationBuilder.DropColumn(
                name: "linked_account_id",
                schema: "billing",
                table: "payment_modes");
        }
    }
}
