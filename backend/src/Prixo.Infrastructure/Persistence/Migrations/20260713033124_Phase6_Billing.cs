using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_Billing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "bills",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    op_visit_id = table.Column<long>(type: "bigint", nullable: true),
                    bill_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    bill_date = table.Column<DateOnly>(type: "date", nullable: false),
                    bill_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bill_discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_credit_bill = table.Column<bool>(type: "boolean", nullable: false),
                    corporate_id = table.Column<long>(type: "bigint", nullable: true),
                    insurance_company_id = table.Column<long>(type: "bigint", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_bills", x => x.id);
                    table.ForeignKey(
                        name: "fk_bills_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_corporates_corporate_id",
                        column: x => x.corporate_id,
                        principalSchema: "master",
                        principalTable: "corporates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_insurance_companies_insurance_company_id",
                        column: x => x.insurance_company_id,
                        principalSchema: "master",
                        principalTable: "insurance_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_op_visits_op_visit_id",
                        column: x => x.op_visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "day_closes",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_day_closes", x => x.id);
                    table.ForeignKey(
                        name: "fk_day_closes_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_modes",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    requires_reference = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bill_items",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: true),
                    package_id = table.Column<long>(type: "bigint", nullable: true),
                    clinical_order_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_items_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_items_clinical_orders_clinical_order_id",
                        column: x => x.clinical_order_id,
                        principalSchema: "opd",
                        principalTable: "clinical_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_items_packages_package_id",
                        column: x => x.package_id,
                        principalSchema: "master",
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_items_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "master",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "advances",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_mode_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_advances", x => x.id);
                    table.ForeignKey(
                        name: "fk_advances_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_advances_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_advances_payment_modes_payment_mode_id",
                        column: x => x.payment_mode_id,
                        principalSchema: "billing",
                        principalTable: "payment_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_payments",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_mode_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receipt_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_payments_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_payments_payment_modes_payment_mode_id",
                        column: x => x.payment_mode_id,
                        principalSchema: "billing",
                        principalTable: "payment_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "day_close_mode_amounts",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    day_close_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_day_close_mode_amounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_day_close_mode_amounts_day_closes_day_close_id",
                        column: x => x.day_close_id,
                        principalSchema: "billing",
                        principalTable: "day_closes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_day_close_mode_amounts_payment_modes_payment_mode_id",
                        column: x => x.payment_mode_id,
                        principalSchema: "billing",
                        principalTable: "payment_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_approvals",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    bill_item_id = table.Column<long>(type: "bigint", nullable: true),
                    discount_type_id = table.Column<long>(type: "bigint", nullable: true),
                    requested_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    requested_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_discount_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_discount_approvals_bill_items_bill_item_id",
                        column: x => x.bill_item_id,
                        principalSchema: "billing",
                        principalTable: "bill_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discount_approvals_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discount_approvals_discount_types_discount_type_id",
                        column: x => x.discount_type_id,
                        principalSchema: "master",
                        principalTable: "discount_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    bill_payment_id = table.Column<long>(type: "bigint", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_refunds", x => x.id);
                    table.ForeignKey(
                        name: "fk_refunds_bill_payments_bill_payment_id",
                        column: x => x.bill_payment_id,
                        principalSchema: "billing",
                        principalTable: "bill_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refunds_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_advances_branch_id_received_at",
                schema: "billing",
                table: "advances",
                columns: new[] { "branch_id", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_advances_patient_id",
                schema: "billing",
                table: "advances",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_advances_payment_mode_id",
                schema: "billing",
                table: "advances",
                column: "payment_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_items_bill_id",
                schema: "billing",
                table: "bill_items",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_items_clinical_order_id",
                schema: "billing",
                table: "bill_items",
                column: "clinical_order_id",
                filter: "is_deleted = false AND clinical_order_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bill_items_package_id",
                schema: "billing",
                table: "bill_items",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_items_service_id",
                schema: "billing",
                table: "bill_items",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_payments_bill_id",
                schema: "billing",
                table: "bill_payments",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_payments_payment_mode_id",
                schema: "billing",
                table: "bill_payments",
                column: "payment_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_payments_receipt_number",
                schema: "billing",
                table: "bill_payments",
                column: "receipt_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_bills_bill_number",
                schema: "billing",
                table: "bills",
                column: "bill_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_bills_branch_id_bill_date_status",
                schema: "billing",
                table: "bills",
                columns: new[] { "branch_id", "bill_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bills_corporate_id",
                schema: "billing",
                table: "bills",
                column: "corporate_id");

            migrationBuilder.CreateIndex(
                name: "ix_bills_insurance_company_id",
                schema: "billing",
                table: "bills",
                column: "insurance_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_bills_op_visit_id",
                schema: "billing",
                table: "bills",
                column: "op_visit_id",
                unique: true,
                filter: "is_deleted = false AND op_visit_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bills_patient_id",
                schema: "billing",
                table: "bills",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_day_close_mode_amounts_day_close_id_payment_mode_id",
                schema: "billing",
                table: "day_close_mode_amounts",
                columns: new[] { "day_close_id", "payment_mode_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_day_close_mode_amounts_payment_mode_id",
                schema: "billing",
                table: "day_close_mode_amounts",
                column: "payment_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_day_closes_branch_id_date",
                schema: "billing",
                table: "day_closes",
                columns: new[] { "branch_id", "date" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_discount_approvals_bill_id",
                schema: "billing",
                table: "discount_approvals",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_approvals_bill_item_id",
                schema: "billing",
                table: "discount_approvals",
                column: "bill_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_approvals_discount_type_id",
                schema: "billing",
                table: "discount_approvals",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_approvals_status",
                schema: "billing",
                table: "discount_approvals",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_payment_modes_code",
                schema: "billing",
                table: "payment_modes",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_bill_id",
                schema: "billing",
                table: "refunds",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_bill_payment_id",
                schema: "billing",
                table: "refunds",
                column: "bill_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_refunds_status",
                schema: "billing",
                table: "refunds",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "advances",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "day_close_mode_amounts",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "discount_approvals",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "refunds",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "day_closes",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "bill_items",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "bill_payments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "bills",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payment_modes",
                schema: "billing");
        }
    }
}
