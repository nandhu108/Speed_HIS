using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10_Nursing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "nursing");

            migrationBuilder.AddColumn<bool>(
                name: "is_high_alert",
                schema: "master",
                table: "drugs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "care_plans",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    problem_statement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    goal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    interventions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    evaluation_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_care_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_care_plans_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_care_plans_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    incident_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    incident_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    immediate_action_taken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    patient_id = table.Column<long>(type: "bigint", nullable: true),
                    admission_id = table.Column<long>(type: "bigint", nullable: true),
                    reported_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_incidents", x => x.id);
                    table.ForeignKey(
                        name: "fk_incidents_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incidents_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incidents_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incidents_users_reported_by_user_id",
                        column: x => x.reported_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incidents_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mar_schedules",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    medication_order_id = table.Column<long>(type: "bigint", nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_high_alert = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mar_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_mar_schedules_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mar_schedules_medication_orders_medication_order_id",
                        column: x => x.medication_order_id,
                        principalSchema: "ipd",
                        principalTable: "medication_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nursing_assessments",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    template_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assessed_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    data_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nursing_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_nursing_assessments_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nursing_assessments_users_assessed_by_user_id",
                        column: x => x.assessed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nursing_notes",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    note_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_nursing_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_nursing_notes_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nursing_notes_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "risk_assessments",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    assessment_type = table.Column<int>(type: "integer", nullable: false),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assessed_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    score_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    total_score = table.Column<int>(type: "integer", nullable: false),
                    risk_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_risk_assessments_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_risk_assessments_users_assessed_by_user_id",
                        column: x => x.assessed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_handovers",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ward_id = table.Column<long>(type: "bigint", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift = table.Column<int>(type: "integer", nullable: false),
                    handover_from_user_id = table.Column<long>(type: "bigint", nullable: false),
                    handover_to_user_id = table.Column<long>(type: "bigint", nullable: true),
                    general_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    handed_over_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_shift_handovers", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_handovers_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shift_handovers_users_handover_from_user_id",
                        column: x => x.handover_from_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shift_handovers_users_handover_to_user_id",
                        column: x => x.handover_to_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shift_handovers_wards_ward_id",
                        column: x => x.ward_id,
                        principalSchema: "master",
                        principalTable: "wards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfer_notes",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    bed_allocation_id = table.Column<long>(type: "bigint", nullable: true),
                    transferred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    handover_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    transferred_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    received_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transfer_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_transfer_notes_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_notes_bed_allocations_bed_allocation_id",
                        column: x => x.bed_allocation_id,
                        principalSchema: "ipd",
                        principalTable: "bed_allocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_notes_users_received_by_user_id",
                        column: x => x.received_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_transfer_notes_users_transferred_by_user_id",
                        column: x => x.transferred_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mar_administrations",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mar_schedule_id = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    action_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    verified_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_mar_administrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_mar_administrations_mar_schedules_mar_schedule_id",
                        column: x => x.mar_schedule_id,
                        principalSchema: "nursing",
                        principalTable: "mar_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mar_administrations_users_action_by_user_id",
                        column: x => x.action_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mar_administrations_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_handover_notes",
                schema: "nursing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shift_handover_id = table.Column<long>(type: "bigint", nullable: false),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    situation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    background = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    assessment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    recommendation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_handover_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_handover_notes_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_handover_notes_shift_handovers_shift_handover_id",
                        column: x => x.shift_handover_id,
                        principalSchema: "nursing",
                        principalTable: "shift_handovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_care_plans_admission_id_status",
                schema: "nursing",
                table: "care_plans",
                columns: new[] { "admission_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_care_plans_created_by_user_id",
                schema: "nursing",
                table: "care_plans",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_admission_id",
                schema: "nursing",
                table: "incidents",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_branch_id_status_incident_date",
                schema: "nursing",
                table: "incidents",
                columns: new[] { "branch_id", "status", "incident_date" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_patient_id",
                schema: "nursing",
                table: "incidents",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_reported_by_user_id",
                schema: "nursing",
                table: "incidents",
                column: "reported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_reviewed_by_user_id",
                schema: "nursing",
                table: "incidents",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mar_administrations_action_by_user_id",
                schema: "nursing",
                table: "mar_administrations",
                column: "action_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mar_administrations_mar_schedule_id",
                schema: "nursing",
                table: "mar_administrations",
                column: "mar_schedule_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_mar_administrations_verified_by_user_id",
                schema: "nursing",
                table: "mar_administrations",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mar_schedules_admission_id_due_at",
                schema: "nursing",
                table: "mar_schedules",
                columns: new[] { "admission_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_mar_schedules_medication_order_id",
                schema: "nursing",
                table: "mar_schedules",
                column: "medication_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_nursing_assessments_admission_id_assessed_at",
                schema: "nursing",
                table: "nursing_assessments",
                columns: new[] { "admission_id", "assessed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_nursing_assessments_assessed_by_user_id",
                schema: "nursing",
                table: "nursing_assessments",
                column: "assessed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_nursing_notes_admission_id_note_at",
                schema: "nursing",
                table: "nursing_notes",
                columns: new[] { "admission_id", "note_at" });

            migrationBuilder.CreateIndex(
                name: "ix_nursing_notes_recorded_by_user_id",
                schema: "nursing",
                table: "nursing_notes",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_handover_notes_admission_id",
                schema: "nursing",
                table: "patient_handover_notes",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_handover_notes_shift_handover_id",
                schema: "nursing",
                table: "patient_handover_notes",
                column: "shift_handover_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_assessments_admission_id_assessment_type_assessed_at",
                schema: "nursing",
                table: "risk_assessments",
                columns: new[] { "admission_id", "assessment_type", "assessed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_risk_assessments_assessed_by_user_id",
                schema: "nursing",
                table: "risk_assessments",
                column: "assessed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_handovers_branch_id",
                schema: "nursing",
                table: "shift_handovers",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_handovers_handover_from_user_id",
                schema: "nursing",
                table: "shift_handovers",
                column: "handover_from_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_handovers_handover_to_user_id",
                schema: "nursing",
                table: "shift_handovers",
                column: "handover_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_handovers_ward_id_shift_date_shift",
                schema: "nursing",
                table: "shift_handovers",
                columns: new[] { "ward_id", "shift_date", "shift" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_notes_admission_id",
                schema: "nursing",
                table: "transfer_notes",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_notes_bed_allocation_id",
                schema: "nursing",
                table: "transfer_notes",
                column: "bed_allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_notes_received_by_user_id",
                schema: "nursing",
                table: "transfer_notes",
                column: "received_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_notes_transferred_by_user_id",
                schema: "nursing",
                table: "transfer_notes",
                column: "transferred_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "care_plans",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "incidents",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "mar_administrations",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "nursing_assessments",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "nursing_notes",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "patient_handover_notes",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "risk_assessments",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "transfer_notes",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "mar_schedules",
                schema: "nursing");

            migrationBuilder.DropTable(
                name: "shift_handovers",
                schema: "nursing");

            migrationBuilder.DropColumn(
                name: "is_high_alert",
                schema: "master",
                table: "drugs");
        }
    }
}
