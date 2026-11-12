using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_Pharmacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pharmacy");

            migrationBuilder.AddColumn<decimal>(
                name: "reorder_level",
                schema: "master",
                table: "drugs",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "purchase_requests",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    request_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_purchase_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_requests_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stores",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_stores", x => x.id);
                    table.ForeignKey(
                        name: "fk_stores_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    purchase_request_id = table.Column<long>(type: "bigint", nullable: true),
                    po_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_purchase_requests_purchase_request_id",
                        column: x => x.purchase_request_id,
                        principalSchema: "pharmacy",
                        principalTable: "purchase_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "master",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_request_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_request_id = table.Column<long>(type: "bigint", nullable: false),
                    drug_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_request_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_request_items_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_request_items_purchase_requests_purchase_request_id",
                        column: x => x.purchase_request_id,
                        principalSchema: "pharmacy",
                        principalTable: "purchase_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "batches",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    drug_id = table.Column<long>(type: "bigint", nullable: false),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    mrp = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    purchase_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
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
                    table.PrimaryKey("pk_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_batches_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_batches_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_batches_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "issues",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    issue_type = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    patient_id = table.Column<long>(type: "bigint", nullable: true),
                    issue_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("pk_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_issues_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issues_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "master",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issues_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issues_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_sales",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: true),
                    prescription_id = table.Column<long>(type: "bigint", nullable: true),
                    sale_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sale_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_mode_id = table.Column<long>(type: "bigint", nullable: true),
                    bill_item_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_pharmacy_sales", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_sales_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pharmacy_sales_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pharmacy_sales_payment_modes_payment_mode_id",
                        column: x => x.payment_mode_id,
                        principalSchema: "billing",
                        principalTable: "payment_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pharmacy_sales_prescriptions_prescription_id",
                        column: x => x.prescription_id,
                        principalSchema: "opd",
                        principalTable: "prescriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pharmacy_sales_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "racks",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("pk_racks", x => x.id);
                    table.ForeignKey(
                        name: "fk_racks_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_racks_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustments",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    adjustment_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    adjustment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_stock_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_adjustments_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adjustments_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_audits",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    audit_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    audit_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_stock_audits", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_audits_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_audits_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfers",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    from_store_id = table.Column<long>(type: "bigint", nullable: false),
                    to_branch_id = table.Column<long>(type: "bigint", nullable: false),
                    to_store_id = table.Column<long>(type: "bigint", nullable: false),
                    transfer_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    transfer_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_stock_transfers", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_transfers_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_transfers_branches_to_branch_id",
                        column: x => x.to_branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_transfers_stores_from_store_id",
                        column: x => x.from_store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_transfers_stores_to_store_id",
                        column: x => x.to_store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "grns",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<long>(type: "bigint", nullable: true),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    grn_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    received_date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    supplier_invoice_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_grns", x => x.id);
                    table.ForeignKey(
                        name: "fk_grns_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grns_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "pharmacy",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grns_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grns_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "master",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<long>(type: "bigint", nullable: false),
                    drug_id = table.Column<long>(type: "bigint", nullable: false),
                    ordered_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_items_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_order_items_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "pharmacy",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_ledger",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    movement_type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    running_balance = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_id = table.Column<long>(type: "bigint", nullable: false),
                    movement_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("pk_stock_ledger", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_ledger_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_ledger_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_ledger_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "pharmacy",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "issue_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issue_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_items_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issue_items_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "pharmacy",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "issue_returns",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issue_id = table.Column<long>(type: "bigint", nullable: false),
                    return_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("pk_issue_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_returns_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issue_returns_issues_issue_id",
                        column: x => x.issue_id,
                        principalSchema: "pharmacy",
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_sale_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pharmacy_sale_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_pharmacy_sale_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_sale_items_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pharmacy_sale_items_pharmacy_sales_pharmacy_sale_id",
                        column: x => x.pharmacy_sale_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_returns",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pharmacy_sale_id = table.Column<long>(type: "bigint", nullable: false),
                    return_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_sales_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_returns_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_returns_pharmacy_sales_pharmacy_sale_id",
                        column: x => x.pharmacy_sale_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustment_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_adjustment_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity_change = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_adjustment_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_adjustment_items_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_adjustment_items_stock_adjustments_stock_adjustment_id",
                        column: x => x.stock_adjustment_id,
                        principalSchema: "pharmacy",
                        principalTable: "stock_adjustments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_audit_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_audit_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    system_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    counted_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    variance = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_audit_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_audit_items_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_audit_items_stock_audits_stock_audit_id",
                        column: x => x.stock_audit_id,
                        principalSchema: "pharmacy",
                        principalTable: "stock_audits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfer_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_transfer_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    received_batch_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_transfer_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_transfer_items_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_transfer_items_batches_received_batch_id",
                        column: x => x.received_batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_transfer_items_stock_transfers_stock_transfer_id",
                        column: x => x.stock_transfer_id,
                        principalSchema: "pharmacy",
                        principalTable: "stock_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "grn_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grn_id = table.Column<long>(type: "bigint", nullable: false),
                    drug_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    mrp = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    purchase_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    free_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grn_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_grn_items_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grn_items_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grn_items_grns_grn_id",
                        column: x => x.grn_id,
                        principalSchema: "pharmacy",
                        principalTable: "grns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_returns",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grn_id = table.Column<long>(type: "bigint", nullable: true),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    return_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_purchase_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_returns_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_returns_grns_grn_id",
                        column: x => x.grn_id,
                        principalSchema: "pharmacy",
                        principalTable: "grns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_returns_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "master",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "issue_return_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issue_return_id = table.Column<long>(type: "bigint", nullable: false),
                    issue_item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_return_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_return_items_issue_items_issue_item_id",
                        column: x => x.issue_item_id,
                        principalSchema: "pharmacy",
                        principalTable: "issue_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_issue_return_items_issue_returns_issue_return_id",
                        column: x => x.issue_return_id,
                        principalSchema: "pharmacy",
                        principalTable: "issue_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_return_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_return_id = table.Column<long>(type: "bigint", nullable: false),
                    pharmacy_sale_item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
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
                    table.PrimaryKey("pk_sales_return_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_return_items_pharmacy_sale_items_pharmacy_sale_item_id",
                        column: x => x.pharmacy_sale_item_id,
                        principalSchema: "pharmacy",
                        principalTable: "pharmacy_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_return_items_sales_returns_sales_return_id",
                        column: x => x.sales_return_id,
                        principalSchema: "pharmacy",
                        principalTable: "sales_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_return_items",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_return_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_return_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_return_items_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_return_items_purchase_returns_purchase_return_id",
                        column: x => x.purchase_return_id,
                        principalSchema: "pharmacy",
                        principalTable: "purchase_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_batches_branch_id",
                schema: "pharmacy",
                table: "batches",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_drug_id_store_id_batch_number",
                schema: "pharmacy",
                table: "batches",
                columns: new[] { "drug_id", "store_id", "batch_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_batches_store_id_expiry_date",
                schema: "pharmacy",
                table: "batches",
                columns: new[] { "store_id", "expiry_date" });

            migrationBuilder.CreateIndex(
                name: "ix_grn_items_batch_id",
                schema: "pharmacy",
                table: "grn_items",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_grn_items_drug_id",
                schema: "pharmacy",
                table: "grn_items",
                column: "drug_id");

            migrationBuilder.CreateIndex(
                name: "ix_grn_items_grn_id",
                schema: "pharmacy",
                table: "grn_items",
                column: "grn_id");

            migrationBuilder.CreateIndex(
                name: "ix_grns_branch_id",
                schema: "pharmacy",
                table: "grns",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_grns_grn_number",
                schema: "pharmacy",
                table: "grns",
                column: "grn_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_grns_purchase_order_id",
                schema: "pharmacy",
                table: "grns",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_grns_store_id",
                schema: "pharmacy",
                table: "grns",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_grns_vendor_id",
                schema: "pharmacy",
                table: "grns",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_items_batch_id",
                schema: "pharmacy",
                table: "issue_items",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_items_issue_id",
                schema: "pharmacy",
                table: "issue_items",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_return_items_issue_item_id",
                schema: "pharmacy",
                table: "issue_return_items",
                column: "issue_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_return_items_issue_return_id",
                schema: "pharmacy",
                table: "issue_return_items",
                column: "issue_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_returns_branch_id",
                schema: "pharmacy",
                table: "issue_returns",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_returns_issue_id",
                schema: "pharmacy",
                table: "issue_returns",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_returns_return_number",
                schema: "pharmacy",
                table: "issue_returns",
                column: "return_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_issues_branch_id_issue_date_issue_type",
                schema: "pharmacy",
                table: "issues",
                columns: new[] { "branch_id", "issue_date", "issue_type" });

            migrationBuilder.CreateIndex(
                name: "ix_issues_department_id",
                schema: "pharmacy",
                table: "issues",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_issue_number",
                schema: "pharmacy",
                table: "issues",
                column: "issue_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_issues_patient_id",
                schema: "pharmacy",
                table: "issues",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_store_id",
                schema: "pharmacy",
                table: "issues",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_items_batch_id",
                schema: "pharmacy",
                table: "pharmacy_sale_items",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sale_items_pharmacy_sale_id",
                schema: "pharmacy",
                table: "pharmacy_sale_items",
                column: "pharmacy_sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_branch_id_sale_date",
                schema: "pharmacy",
                table: "pharmacy_sales",
                columns: new[] { "branch_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_patient_id",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_payment_mode_id",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "payment_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_prescription_id",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_sale_number",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "sale_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_store_id",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_drug_id",
                schema: "pharmacy",
                table: "purchase_order_items",
                column: "drug_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_purchase_order_id",
                schema: "pharmacy",
                table: "purchase_order_items",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_branch_id",
                schema: "pharmacy",
                table: "purchase_orders",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_po_number",
                schema: "pharmacy",
                table: "purchase_orders",
                column: "po_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_purchase_request_id",
                schema: "pharmacy",
                table: "purchase_orders",
                column: "purchase_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_vendor_id",
                schema: "pharmacy",
                table: "purchase_orders",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_items_drug_id",
                schema: "pharmacy",
                table: "purchase_request_items",
                column: "drug_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_request_items_purchase_request_id",
                schema: "pharmacy",
                table: "purchase_request_items",
                column: "purchase_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requests_branch_id",
                schema: "pharmacy",
                table: "purchase_requests",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requests_request_number",
                schema: "pharmacy",
                table: "purchase_requests",
                column: "request_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_items_batch_id",
                schema: "pharmacy",
                table: "purchase_return_items",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_items_purchase_return_id",
                schema: "pharmacy",
                table: "purchase_return_items",
                column: "purchase_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_branch_id",
                schema: "pharmacy",
                table: "purchase_returns",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_grn_id",
                schema: "pharmacy",
                table: "purchase_returns",
                column: "grn_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_return_number",
                schema: "pharmacy",
                table: "purchase_returns",
                column: "return_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_vendor_id",
                schema: "pharmacy",
                table: "purchase_returns",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_racks_branch_id",
                schema: "pharmacy",
                table: "racks",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_racks_store_id_code",
                schema: "pharmacy",
                table: "racks",
                columns: new[] { "store_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_items_pharmacy_sale_item_id",
                schema: "pharmacy",
                table: "sales_return_items",
                column: "pharmacy_sale_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_items_sales_return_id",
                schema: "pharmacy",
                table: "sales_return_items",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_branch_id",
                schema: "pharmacy",
                table: "sales_returns",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_pharmacy_sale_id",
                schema: "pharmacy",
                table: "sales_returns",
                column: "pharmacy_sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_return_number",
                schema: "pharmacy",
                table: "sales_returns",
                column: "return_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustment_items_batch_id",
                schema: "pharmacy",
                table: "stock_adjustment_items",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustment_items_stock_adjustment_id",
                schema: "pharmacy",
                table: "stock_adjustment_items",
                column: "stock_adjustment_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_adjustment_number",
                schema: "pharmacy",
                table: "stock_adjustments",
                column: "adjustment_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_branch_id",
                schema: "pharmacy",
                table: "stock_adjustments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_status",
                schema: "pharmacy",
                table: "stock_adjustments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_store_id",
                schema: "pharmacy",
                table: "stock_adjustments",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_audit_items_batch_id",
                schema: "pharmacy",
                table: "stock_audit_items",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_audit_items_stock_audit_id",
                schema: "pharmacy",
                table: "stock_audit_items",
                column: "stock_audit_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_audits_audit_number",
                schema: "pharmacy",
                table: "stock_audits",
                column: "audit_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_audits_branch_id",
                schema: "pharmacy",
                table: "stock_audits",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_audits_store_id",
                schema: "pharmacy",
                table: "stock_audits",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_batch_id_movement_at",
                schema: "pharmacy",
                table: "stock_ledger",
                columns: new[] { "batch_id", "movement_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_branch_id",
                schema: "pharmacy",
                table: "stock_ledger",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_reference_type_reference_id",
                schema: "pharmacy",
                table: "stock_ledger",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_store_id",
                schema: "pharmacy",
                table: "stock_ledger",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfer_items_batch_id",
                schema: "pharmacy",
                table: "stock_transfer_items",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfer_items_received_batch_id",
                schema: "pharmacy",
                table: "stock_transfer_items",
                column: "received_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfer_items_stock_transfer_id",
                schema: "pharmacy",
                table: "stock_transfer_items",
                column: "stock_transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_branch_id",
                schema: "pharmacy",
                table: "stock_transfers",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_from_store_id",
                schema: "pharmacy",
                table: "stock_transfers",
                column: "from_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_status",
                schema: "pharmacy",
                table: "stock_transfers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_to_branch_id",
                schema: "pharmacy",
                table: "stock_transfers",
                column: "to_branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_to_store_id",
                schema: "pharmacy",
                table: "stock_transfers",
                column: "to_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_transfer_number",
                schema: "pharmacy",
                table: "stock_transfers",
                column: "transfer_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stores_branch_id_code",
                schema: "pharmacy",
                table: "stores",
                columns: new[] { "branch_id", "code" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grn_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "issue_return_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "purchase_order_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "purchase_request_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "purchase_return_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "racks",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "sales_return_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "stock_adjustment_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "stock_audit_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "stock_ledger",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "stock_transfer_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "issue_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "issue_returns",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "purchase_returns",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_sale_items",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "sales_returns",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "stock_adjustments",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "stock_audits",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "stock_transfers",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "issues",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "grns",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "batches",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "pharmacy_sales",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "stores",
                schema: "pharmacy");

            migrationBuilder.DropTable(
                name: "purchase_requests",
                schema: "pharmacy");

            migrationBuilder.DropColumn(
                name: "reorder_level",
                schema: "master",
                table: "drugs");
        }
    }
}
