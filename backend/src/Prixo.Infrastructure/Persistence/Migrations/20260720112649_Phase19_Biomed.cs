using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase19_Biomed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_equipment",
                schema: "ot",
                table: "equipment");

            migrationBuilder.EnsureSchema(
                name: "biomed");

            migrationBuilder.AddPrimaryKey(
                name: "pk_equipment1",
                schema: "ot",
                table: "equipment",
                column: "id");

            migrationBuilder.CreateTable(
                name: "equipment",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    vendor_id = table.Column<long>(type: "bigint", nullable: true),
                    criticality = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    current_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: true),
                    purchase_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    warranty_expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_equipment", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipment_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipment_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "master",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipment_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "master",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "amc_contracts",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_type = table.Column<int>(type: "integer", nullable: false),
                    contract_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    renewal_reminder_days = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_amc_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_amc_contracts_biomed_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "biomed",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_amc_contracts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "master",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "breakdown_complaints",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_id = table.Column<long>(type: "bigint", nullable: false),
                    complaint_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reported_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    reported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_to_user_id = table.Column<long>(type: "bigint", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    downtime_minutes = table.Column<int>(type: "integer", nullable: true),
                    resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_breakdown_complaints", x => x.id);
                    table.ForeignKey(
                        name: "fk_breakdown_complaints_biomed_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "biomed",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_breakdown_complaints_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_breakdown_complaints_users_reported_by_user_id",
                        column: x => x.reported_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "calibrations",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_id = table.Column<long>(type: "bigint", nullable: false),
                    calibration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    next_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    performed_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    certificate_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    certificate_file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_calibrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_calibrations_biomed_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "biomed",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_location_logs",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_id = table.Column<long>(type: "bigint", nullable: false),
                    moved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    from_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    to_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    moved_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_location_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipment_location_logs_biomed_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "biomed",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipment_location_logs_users_moved_by_user_id",
                        column: x => x.moved_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pm_schedules",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_id = table.Column<long>(type: "bigint", nullable: false),
                    frequency_days = table.Column<int>(type: "integer", nullable: false),
                    next_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_generated_date = table.Column<DateOnly>(type: "date", nullable: true),
                    checklist_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_pm_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_pm_schedules_biomed_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "biomed",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_orders",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_id = table.Column<long>(type: "bigint", nullable: false),
                    work_order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    work_order_type = table.Column<int>(type: "integer", nullable: false),
                    pm_schedule_id = table.Column<long>(type: "bigint", nullable: true),
                    breakdown_complaint_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_to_user_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_work_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_orders_biomed_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "biomed",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_orders_breakdown_complaints_breakdown_complaint_id",
                        column: x => x.breakdown_complaint_id,
                        principalSchema: "biomed",
                        principalTable: "breakdown_complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_orders_pm_schedules_pm_schedule_id",
                        column: x => x.pm_schedule_id,
                        principalSchema: "biomed",
                        principalTable: "pm_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_orders_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_histories",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_id = table.Column<long>(type: "bigint", nullable: false),
                    work_order_id = table.Column<long>(type: "bigint", nullable: true),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    service_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    performed_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
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
                    table.PrimaryKey("pk_service_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_histories_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "biomed",
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_histories_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "biomed",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spare_usages",
                schema: "biomed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_spare_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_spare_usages_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_spare_usages_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "biomed",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_amc_contracts_equipment_id",
                schema: "biomed",
                table: "amc_contracts",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_amc_contracts_vendor_id",
                schema: "biomed",
                table: "amc_contracts",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_complaints_assigned_to_user_id",
                schema: "biomed",
                table: "breakdown_complaints",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_complaints_complaint_number",
                schema: "biomed",
                table: "breakdown_complaints",
                column: "complaint_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_complaints_equipment_id_status",
                schema: "biomed",
                table: "breakdown_complaints",
                columns: new[] { "equipment_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_complaints_reported_by_user_id",
                schema: "biomed",
                table: "breakdown_complaints",
                column: "reported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_calibrations_equipment_id_next_due_date",
                schema: "biomed",
                table: "calibrations",
                columns: new[] { "equipment_id", "next_due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_branch_id",
                schema: "biomed",
                table: "equipment",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_department_id",
                schema: "biomed",
                table: "equipment",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_equipment_code",
                schema: "biomed",
                table: "equipment",
                column: "equipment_code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_vendor_id",
                schema: "biomed",
                table: "equipment",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_location_logs_equipment_id_moved_at",
                schema: "biomed",
                table: "equipment_location_logs",
                columns: new[] { "equipment_id", "moved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_location_logs_moved_by_user_id",
                schema: "biomed",
                table: "equipment_location_logs",
                column: "moved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pm_schedules_equipment_id_next_due_date",
                schema: "biomed",
                table: "pm_schedules",
                columns: new[] { "equipment_id", "next_due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_service_histories_equipment_id_service_date",
                schema: "biomed",
                table: "service_histories",
                columns: new[] { "equipment_id", "service_date" });

            migrationBuilder.CreateIndex(
                name: "ix_service_histories_work_order_id",
                schema: "biomed",
                table: "service_histories",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_spare_usages_batch_id",
                schema: "biomed",
                table: "spare_usages",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_spare_usages_work_order_id",
                schema: "biomed",
                table: "spare_usages",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_assigned_to_user_id",
                schema: "biomed",
                table: "work_orders",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_breakdown_complaint_id",
                schema: "biomed",
                table: "work_orders",
                column: "breakdown_complaint_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_equipment_id_status",
                schema: "biomed",
                table: "work_orders",
                columns: new[] { "equipment_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_pm_schedule_id",
                schema: "biomed",
                table: "work_orders",
                column: "pm_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_work_order_number",
                schema: "biomed",
                table: "work_orders",
                column: "work_order_number",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "amc_contracts",
                schema: "biomed");

            migrationBuilder.DropTable(
                name: "calibrations",
                schema: "biomed");

            migrationBuilder.DropTable(
                name: "equipment_location_logs",
                schema: "biomed");

            migrationBuilder.DropTable(
                name: "service_histories",
                schema: "biomed");

            migrationBuilder.DropTable(
                name: "spare_usages",
                schema: "biomed");

            migrationBuilder.DropTable(
                name: "work_orders",
                schema: "biomed");

            migrationBuilder.DropTable(
                name: "breakdown_complaints",
                schema: "biomed");

            migrationBuilder.DropTable(
                name: "pm_schedules",
                schema: "biomed");

            migrationBuilder.DropTable(
                name: "equipment",
                schema: "biomed");

            migrationBuilder.DropPrimaryKey(
                name: "pk_equipment1",
                schema: "ot",
                table: "equipment");

            migrationBuilder.AddPrimaryKey(
                name: "pk_equipment",
                schema: "ot",
                table: "equipment",
                column: "id");
        }
    }
}
