using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_Lab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lab");

            migrationBuilder.CreateTable(
                name: "lab_orders",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    op_visit_id = table.Column<long>(type: "bigint", nullable: true),
                    clinical_order_id = table.Column<long>(type: "bigint", nullable: true),
                    ordered_by_doctor_id = table.Column<long>(type: "bigint", nullable: true),
                    order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    clinical_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_lab_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_orders_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_orders_clinical_orders_clinical_order_id",
                        column: x => x.clinical_order_id,
                        principalSchema: "opd",
                        principalTable: "clinical_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_orders_doctors_ordered_by_doctor_id",
                        column: x => x.ordered_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_orders_op_visits_op_visit_id",
                        column: x => x.op_visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_orders_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lab_tests",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    sample_type_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: true),
                    method = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tat_hours = table.Column<int>(type: "integer", nullable: false),
                    is_panel = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lab_tests", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_tests_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "master",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_tests_sample_types_sample_type_id",
                        column: x => x.sample_type_id,
                        principalSchema: "master",
                        principalTable: "sample_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_tests_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "master",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_profiles",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "samples",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lab_order_id = table.Column<long>(type: "bigint", nullable: false),
                    sample_type_id = table.Column<long>(type: "bigint", nullable: false),
                    sample_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    collected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    collected_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rejected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_samples", x => x.id);
                    table.ForeignKey(
                        name: "fk_samples_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_samples_lab_orders_lab_order_id",
                        column: x => x.lab_order_id,
                        principalSchema: "lab",
                        principalTable: "lab_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_samples_sample_types_sample_type_id",
                        column: x => x.sample_type_id,
                        principalSchema: "master",
                        principalTable: "sample_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lab_test_parameters",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lab_test_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    dropdown_options = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lab_test_parameters", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_test_parameters_lab_tests_lab_test_id",
                        column: x => x.lab_test_id,
                        principalSchema: "lab",
                        principalTable: "lab_tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "profile_tests",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    test_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    lab_test_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_tests", x => x.id);
                    table.ForeignKey(
                        name: "fk_profile_tests_lab_tests_lab_test_id",
                        column: x => x.lab_test_id,
                        principalSchema: "lab",
                        principalTable: "lab_tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_profile_tests_test_profiles_test_profile_id",
                        column: x => x.test_profile_id,
                        principalSchema: "lab",
                        principalTable: "test_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lab_order_items",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lab_order_id = table.Column<long>(type: "bigint", nullable: false),
                    lab_test_id = table.Column<long>(type: "bigint", nullable: false),
                    test_profile_id = table.Column<long>(type: "bigint", nullable: true),
                    sample_id = table.Column<long>(type: "bigint", nullable: true),
                    bill_item_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lab_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_order_items_lab_orders_lab_order_id",
                        column: x => x.lab_order_id,
                        principalSchema: "lab",
                        principalTable: "lab_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_order_items_lab_tests_lab_test_id",
                        column: x => x.lab_test_id,
                        principalSchema: "lab",
                        principalTable: "lab_tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_order_items_samples_sample_id",
                        column: x => x.sample_id,
                        principalSchema: "lab",
                        principalTable: "samples",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lab_order_items_test_profiles_test_profile_id",
                        column: x => x.test_profile_id,
                        principalSchema: "lab",
                        principalTable: "test_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reference_ranges",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lab_test_parameter_id = table.Column<long>(type: "bigint", nullable: false),
                    sex = table.Column<int>(type: "integer", nullable: true),
                    min_age_years = table.Column<int>(type: "integer", nullable: true),
                    max_age_years = table.Column<int>(type: "integer", nullable: true),
                    normal_low = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    normal_high = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    critical_low = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    critical_high = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    text_normal_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reference_ranges", x => x.id);
                    table.ForeignKey(
                        name: "fk_reference_ranges_lab_test_parameters_lab_test_parameter_id",
                        column: x => x.lab_test_parameter_id,
                        principalSchema: "lab",
                        principalTable: "lab_test_parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lab_results",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lab_order_item_id = table.Column<long>(type: "bigint", nullable: false),
                    entered_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    entered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    verified_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    has_critical_value = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lab_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_lab_results_lab_order_items_lab_order_item_id",
                        column: x => x.lab_order_item_id,
                        principalSchema: "lab",
                        principalTable: "lab_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "result_values",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lab_result_id = table.Column<long>(type: "bigint", nullable: false),
                    lab_test_parameter_id = table.Column<long>(type: "bigint", nullable: false),
                    value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    normal_low = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    normal_high = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    flag = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_result_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_result_values_lab_results_lab_result_id",
                        column: x => x.lab_result_id,
                        principalSchema: "lab",
                        principalTable: "lab_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_result_values_lab_test_parameters_lab_test_parameter_id",
                        column: x => x.lab_test_parameter_id,
                        principalSchema: "lab",
                        principalTable: "lab_test_parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "critical_alerts",
                schema: "lab",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lab_result_id = table.Column<long>(type: "bigint", nullable: false),
                    result_value_id = table.Column<long>(type: "bigint", nullable: false),
                    raised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    acknowledged_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledgement_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_critical_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_critical_alerts_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_critical_alerts_lab_results_lab_result_id",
                        column: x => x.lab_result_id,
                        principalSchema: "lab",
                        principalTable: "lab_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_critical_alerts_result_values_result_value_id",
                        column: x => x.result_value_id,
                        principalSchema: "lab",
                        principalTable: "result_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_critical_alerts_branch_id",
                schema: "lab",
                table: "critical_alerts",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_critical_alerts_is_acknowledged",
                schema: "lab",
                table: "critical_alerts",
                column: "is_acknowledged");

            migrationBuilder.CreateIndex(
                name: "ix_critical_alerts_lab_result_id",
                schema: "lab",
                table: "critical_alerts",
                column: "lab_result_id");

            migrationBuilder.CreateIndex(
                name: "ix_critical_alerts_result_value_id",
                schema: "lab",
                table: "critical_alerts",
                column: "result_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_items_lab_order_id",
                schema: "lab",
                table: "lab_order_items",
                column: "lab_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_items_lab_test_id",
                schema: "lab",
                table: "lab_order_items",
                column: "lab_test_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_items_sample_id",
                schema: "lab",
                table: "lab_order_items",
                column: "sample_id",
                filter: "is_deleted = false AND sample_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_lab_order_items_test_profile_id",
                schema: "lab",
                table: "lab_order_items",
                column: "test_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_branch_id_order_date_status",
                schema: "lab",
                table: "lab_orders",
                columns: new[] { "branch_id", "order_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_clinical_order_id",
                schema: "lab",
                table: "lab_orders",
                column: "clinical_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_op_visit_id",
                schema: "lab",
                table: "lab_orders",
                column: "op_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_order_number",
                schema: "lab",
                table: "lab_orders",
                column: "order_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_ordered_by_doctor_id",
                schema: "lab",
                table: "lab_orders",
                column: "ordered_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_patient_id_order_date",
                schema: "lab",
                table: "lab_orders",
                columns: new[] { "patient_id", "order_date" });

            migrationBuilder.CreateIndex(
                name: "ix_lab_results_lab_order_item_id",
                schema: "lab",
                table: "lab_results",
                column: "lab_order_item_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lab_test_parameters_lab_test_id_code",
                schema: "lab",
                table: "lab_test_parameters",
                columns: new[] { "lab_test_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lab_tests_code",
                schema: "lab",
                table: "lab_tests",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lab_tests_department_id",
                schema: "lab",
                table: "lab_tests",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_tests_sample_type_id",
                schema: "lab",
                table: "lab_tests",
                column: "sample_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_tests_service_id",
                schema: "lab",
                table: "lab_tests",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_profile_tests_lab_test_id",
                schema: "lab",
                table: "profile_tests",
                column: "lab_test_id");

            migrationBuilder.CreateIndex(
                name: "ix_profile_tests_test_profile_id_lab_test_id",
                schema: "lab",
                table: "profile_tests",
                columns: new[] { "test_profile_id", "lab_test_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_reference_ranges_lab_test_parameter_id",
                schema: "lab",
                table: "reference_ranges",
                column: "lab_test_parameter_id");

            migrationBuilder.CreateIndex(
                name: "ix_result_values_lab_result_id",
                schema: "lab",
                table: "result_values",
                column: "lab_result_id");

            migrationBuilder.CreateIndex(
                name: "ix_result_values_lab_test_parameter_id",
                schema: "lab",
                table: "result_values",
                column: "lab_test_parameter_id");

            migrationBuilder.CreateIndex(
                name: "ix_samples_branch_id",
                schema: "lab",
                table: "samples",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_samples_lab_order_id",
                schema: "lab",
                table: "samples",
                column: "lab_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_samples_sample_number",
                schema: "lab",
                table: "samples",
                column: "sample_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_samples_sample_type_id",
                schema: "lab",
                table: "samples",
                column: "sample_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_profiles_code",
                schema: "lab",
                table: "test_profiles",
                column: "code",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "critical_alerts",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "profile_tests",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "reference_ranges",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "result_values",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "lab_results",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "lab_test_parameters",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "lab_order_items",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "lab_tests",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "samples",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "test_profiles",
                schema: "lab");

            migrationBuilder.DropTable(
                name: "lab_orders",
                schema: "lab");
        }
    }
}
