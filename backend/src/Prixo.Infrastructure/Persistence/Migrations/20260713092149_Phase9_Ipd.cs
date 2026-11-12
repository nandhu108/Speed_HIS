using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_Ipd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ipd");

            migrationBuilder.AddColumn<long>(
                name: "admission_id",
                schema: "pharmacy",
                table: "issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "admission_id",
                schema: "billing",
                table: "bills",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "master",
                table: "beds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "admission_id",
                schema: "billing",
                table: "advances",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admissions",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    admission_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    admission_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    admission_type = table.Column<int>(type: "integer", nullable: false),
                    primary_consultant_id = table.Column<long>(type: "bigint", nullable: false),
                    referring_op_visit_id = table.Column<long>(type: "bigint", nullable: true),
                    current_bed_id = table.Column<long>(type: "bigint", nullable: false),
                    provisional_diagnosis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    patient_consent_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    discharge_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    discharge_requested_by_doctor_id = table.Column<long>(type: "bigint", nullable: true),
                    discharge_type = table.Column<int>(type: "integer", nullable: true),
                    discharge_type_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    discharged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_admissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_admissions_beds_current_bed_id",
                        column: x => x.current_bed_id,
                        principalSchema: "master",
                        principalTable: "beds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admissions_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admissions_doctors_discharge_requested_by_doctor_id",
                        column: x => x.discharge_requested_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admissions_doctors_primary_consultant_id",
                        column: x => x.primary_consultant_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admissions_op_visits_referring_op_visit_id",
                        column: x => x.referring_op_visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admissions_patient_consents_patient_consent_id",
                        column: x => x.patient_consent_id,
                        principalSchema: "patient",
                        principalTable: "patient_consents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admissions_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_consultants",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_consultants", x => x.id);
                    table.ForeignKey(
                        name: "fk_admission_consultants_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_consultants_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bed_allocations",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    bed_id = table.Column<long>(type: "bigint", nullable: false),
                    bed_charge_per_day = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    transfer_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bed_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_bed_allocations_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bed_allocations_beds_bed_id",
                        column: x => x.bed_id,
                        principalSchema: "master",
                        principalTable: "beds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discharge_clearances",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    clearance_type = table.Column<int>(type: "integer", nullable: false),
                    is_cleared = table.Column<bool>(type: "boolean", nullable: false),
                    cleared_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    cleared_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discharge_clearances", x => x.id);
                    table.ForeignKey(
                        name: "fk_discharge_clearances_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discharge_summaries",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    final_diagnosis = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    course_in_hospital = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    medications_on_discharge = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    follow_up_advice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    condition_at_discharge = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_doctor_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discharge_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_discharge_summaries_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discharge_summaries_doctors_approved_by_doctor_id",
                        column: x => x.approved_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_orders",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    order_type = table.Column<int>(type: "integer", nullable: false),
                    ordered_by_doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    ordered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doctor_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_doctor_orders_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_doctor_orders_doctors_ordered_by_doctor_id",
                        column: x => x.ordered_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "intake_output_entries",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_intake = table.Column<bool>(type: "boolean", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity_ml = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intake_output_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_intake_output_entries_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ip_vitals",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    temperature_f = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    pulse_rate = table.Column<int>(type: "integer", nullable: true),
                    respiratory_rate = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure_systolic = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure_diastolic = table.Column<int>(type: "integer", nullable: true),
                    sp_o2 = table.Column<int>(type: "integer", nullable: true),
                    random_blood_sugar = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ip_vitals", x => x.id);
                    table.ForeignKey(
                        name: "fk_ip_vitals_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procedure_notes",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    procedure_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    performed_by_doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    performed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    findings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedure_notes_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_procedure_notes_doctors_performed_by_doctor_id",
                        column: x => x.performed_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "progress_notes",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    note_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progress_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_progress_notes_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_progress_notes_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "diet_orders",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doctor_order_id = table.Column<long>(type: "bigint", nullable: false),
                    diet_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diet_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_diet_orders_doctor_orders_doctor_order_id",
                        column: x => x.doctor_order_id,
                        principalSchema: "ipd",
                        principalTable: "doctor_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medication_orders",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doctor_order_id = table.Column<long>(type: "bigint", nullable: false),
                    drug_id = table.Column<long>(type: "bigint", nullable: false),
                    dose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    route = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medication_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_medication_orders_doctor_orders_doctor_order_id",
                        column: x => x.doctor_order_id,
                        principalSchema: "ipd",
                        principalTable: "doctor_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medication_orders_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issues_admission_id",
                schema: "pharmacy",
                table: "issues",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_bills_admission_id",
                schema: "billing",
                table: "bills",
                column: "admission_id",
                unique: true,
                filter: "is_deleted = false AND admission_id IS NOT NULL AND status <> 4");

            migrationBuilder.CreateIndex(
                name: "ix_advances_admission_id",
                schema: "billing",
                table: "advances",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_consultants_admission_id",
                schema: "ipd",
                table: "admission_consultants",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_consultants_doctor_id",
                schema: "ipd",
                table: "admission_consultants",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_admission_number",
                schema: "ipd",
                table: "admissions",
                column: "admission_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_branch_id",
                schema: "ipd",
                table: "admissions",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_current_bed_id",
                schema: "ipd",
                table: "admissions",
                column: "current_bed_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_discharge_requested_by_doctor_id",
                schema: "ipd",
                table: "admissions",
                column: "discharge_requested_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_patient_consent_id",
                schema: "ipd",
                table: "admissions",
                column: "patient_consent_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_patient_id_status",
                schema: "ipd",
                table: "admissions",
                columns: new[] { "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_admissions_primary_consultant_id",
                schema: "ipd",
                table: "admissions",
                column: "primary_consultant_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_referring_op_visit_id",
                schema: "ipd",
                table: "admissions",
                column: "referring_op_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_bed_allocations_admission_id",
                schema: "ipd",
                table: "bed_allocations",
                column: "admission_id",
                unique: true,
                filter: "released_at IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_bed_allocations_bed_id",
                schema: "ipd",
                table: "bed_allocations",
                column: "bed_id",
                unique: true,
                filter: "released_at IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_diet_orders_doctor_order_id",
                schema: "ipd",
                table: "diet_orders",
                column: "doctor_order_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_discharge_clearances_admission_id_clearance_type",
                schema: "ipd",
                table: "discharge_clearances",
                columns: new[] { "admission_id", "clearance_type" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_discharge_summaries_admission_id",
                schema: "ipd",
                table: "discharge_summaries",
                column: "admission_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_discharge_summaries_approved_by_doctor_id",
                schema: "ipd",
                table: "discharge_summaries",
                column: "approved_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_orders_admission_id_order_type",
                schema: "ipd",
                table: "doctor_orders",
                columns: new[] { "admission_id", "order_type" });

            migrationBuilder.CreateIndex(
                name: "ix_doctor_orders_ordered_by_doctor_id",
                schema: "ipd",
                table: "doctor_orders",
                column: "ordered_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_intake_output_entries_admission_id_recorded_at",
                schema: "ipd",
                table: "intake_output_entries",
                columns: new[] { "admission_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ip_vitals_admission_id_recorded_at",
                schema: "ipd",
                table: "ip_vitals",
                columns: new[] { "admission_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_medication_orders_doctor_order_id",
                schema: "ipd",
                table: "medication_orders",
                column: "doctor_order_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_medication_orders_drug_id",
                schema: "ipd",
                table: "medication_orders",
                column: "drug_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedure_notes_admission_id",
                schema: "ipd",
                table: "procedure_notes",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedure_notes_performed_by_doctor_id",
                schema: "ipd",
                table: "procedure_notes",
                column: "performed_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_progress_notes_admission_id",
                schema: "ipd",
                table: "progress_notes",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_progress_notes_doctor_id",
                schema: "ipd",
                table: "progress_notes",
                column: "doctor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_advances_admissions_admission_id",
                schema: "billing",
                table: "advances",
                column: "admission_id",
                principalSchema: "ipd",
                principalTable: "admissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_bills_admissions_admission_id",
                schema: "billing",
                table: "bills",
                column: "admission_id",
                principalSchema: "ipd",
                principalTable: "admissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_issues_admissions_admission_id",
                schema: "pharmacy",
                table: "issues",
                column: "admission_id",
                principalSchema: "ipd",
                principalTable: "admissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_advances_admissions_admission_id",
                schema: "billing",
                table: "advances");

            migrationBuilder.DropForeignKey(
                name: "fk_bills_admissions_admission_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_admissions_admission_id",
                schema: "pharmacy",
                table: "issues");

            migrationBuilder.DropTable(
                name: "admission_consultants",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "bed_allocations",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "diet_orders",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "discharge_clearances",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "discharge_summaries",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "intake_output_entries",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "ip_vitals",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "medication_orders",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "procedure_notes",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "progress_notes",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "doctor_orders",
                schema: "ipd");

            migrationBuilder.DropTable(
                name: "admissions",
                schema: "ipd");

            migrationBuilder.DropIndex(
                name: "ix_issues_admission_id",
                schema: "pharmacy",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_bills_admission_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropIndex(
                name: "ix_advances_admission_id",
                schema: "billing",
                table: "advances");

            migrationBuilder.DropColumn(
                name: "admission_id",
                schema: "pharmacy",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "admission_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "master",
                table: "beds");

            migrationBuilder.DropColumn(
                name: "admission_id",
                schema: "billing",
                table: "advances");
        }
    }
}
