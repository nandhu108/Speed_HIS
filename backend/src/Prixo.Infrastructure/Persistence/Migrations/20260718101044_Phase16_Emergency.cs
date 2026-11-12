using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase16_Emergency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "er");

            migrationBuilder.AddColumn<bool>(
                name: "is_observation",
                schema: "master",
                table: "wards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "emergency_visit_id",
                schema: "rad",
                table: "rad_orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "emergency_visit_id",
                schema: "lab",
                table: "lab_orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "emergency_visit_id",
                schema: "billing",
                table: "bills",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "referring_emergency_visit_id",
                schema: "ipd",
                table: "admissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "emergency_visits",
                schema: "er",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    assigned_doctor_id = table.Column<long>(type: "bigint", nullable: true),
                    visit_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    token_number = table.Column<int>(type: "integer", nullable: false),
                    arrived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    arrival_mode = table.Column<int>(type: "integer", nullable: false),
                    is_mlc = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    registered_by_user_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_emergency_visits", x => x.id);
                    table.ForeignKey(
                        name: "fk_emergency_visits_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_emergency_visits_doctors_assigned_doctor_id",
                        column: x => x.assigned_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_emergency_visits_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_emergency_visits_users_registered_by_user_id",
                        column: x => x.registered_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accident_case_records",
                schema: "er",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    emergency_visit_id = table.Column<long>(type: "bigint", nullable: false),
                    accident_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    accident_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accident_location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    vehicle_involved = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accident_case_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_accident_case_records_emergency_visits_emergency_visit_id",
                        column: x => x.emergency_visit_id,
                        principalSchema: "er",
                        principalTable: "emergency_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_accident_case_records_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "emergency_assessments",
                schema: "er",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    emergency_visit_id = table.Column<long>(type: "bigint", nullable: false),
                    assessed_by_doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    history = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    examination_findings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    provisional_diagnosis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    treatment_given = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    disposition = table.Column<int>(type: "integer", nullable: true),
                    disposition_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    transferred_to_facility = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    transfer_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emergency_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_emergency_assessments_doctors_assessed_by_doctor_id",
                        column: x => x.assessed_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_emergency_assessments_emergency_visits_emergency_visit_id",
                        column: x => x.emergency_visit_id,
                        principalSchema: "er",
                        principalTable: "emergency_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mlc_records",
                schema: "er",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    emergency_visit_id = table.Column<long>(type: "bigint", nullable: false),
                    mlc_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    police_station_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    police_informed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fir_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    brought_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    brought_by_designation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    injury_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mlc_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_mlc_records_emergency_visits_emergency_visit_id",
                        column: x => x.emergency_visit_id,
                        principalSchema: "er",
                        principalTable: "emergency_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mlc_records_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "observation_allocations",
                schema: "er",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    emergency_visit_id = table.Column<long>(type: "bigint", nullable: false),
                    bed_id = table.Column<long>(type: "bigint", nullable: false),
                    allocated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_observation_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_observation_allocations_beds_bed_id",
                        column: x => x.bed_id,
                        principalSchema: "master",
                        principalTable: "beds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_observation_allocations_emergency_visits_emergency_visit_id",
                        column: x => x.emergency_visit_id,
                        principalSchema: "er",
                        principalTable: "emergency_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "triages",
                schema: "er",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    emergency_visit_id = table.Column<long>(type: "bigint", nullable: false),
                    triaged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    triaged_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    esi_level = table.Column<int>(type: "integer", nullable: false),
                    chief_complaint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    temperature_f = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    pulse_rate = table.Column<int>(type: "integer", nullable: true),
                    respiratory_rate = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure_systolic = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure_diastolic = table.Column<int>(type: "integer", nullable: true),
                    sp_o2 = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_triages", x => x.id);
                    table.ForeignKey(
                        name: "fk_triages_emergency_visits_emergency_visit_id",
                        column: x => x.emergency_visit_id,
                        principalSchema: "er",
                        principalTable: "emergency_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_triages_users_triaged_by_user_id",
                        column: x => x.triaged_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_emergency_visit_id",
                schema: "rad",
                table: "rad_orders",
                column: "emergency_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_emergency_visit_id",
                schema: "lab",
                table: "lab_orders",
                column: "emergency_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_bills_emergency_visit_id",
                schema: "billing",
                table: "bills",
                column: "emergency_visit_id",
                unique: true,
                filter: "is_deleted = false AND emergency_visit_id IS NOT NULL AND status <> 4");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_referring_emergency_visit_id",
                schema: "ipd",
                table: "admissions",
                column: "referring_emergency_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_accident_case_records_emergency_visit_id",
                schema: "er",
                table: "accident_case_records",
                column: "emergency_visit_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_accident_case_records_recorded_by_user_id",
                schema: "er",
                table: "accident_case_records",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_assessments_assessed_by_doctor_id",
                schema: "er",
                table: "emergency_assessments",
                column: "assessed_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_assessments_emergency_visit_id",
                schema: "er",
                table: "emergency_assessments",
                column: "emergency_visit_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_visits_assigned_doctor_id",
                schema: "er",
                table: "emergency_visits",
                column: "assigned_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_visits_branch_id_arrived_at_status",
                schema: "er",
                table: "emergency_visits",
                columns: new[] { "branch_id", "arrived_at", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_emergency_visits_patient_id",
                schema: "er",
                table: "emergency_visits",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_visits_registered_by_user_id",
                schema: "er",
                table: "emergency_visits",
                column: "registered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_visits_visit_number",
                schema: "er",
                table: "emergency_visits",
                column: "visit_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_mlc_records_emergency_visit_id",
                schema: "er",
                table: "mlc_records",
                column: "emergency_visit_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_mlc_records_mlc_number",
                schema: "er",
                table: "mlc_records",
                column: "mlc_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_mlc_records_recorded_by_user_id",
                schema: "er",
                table: "mlc_records",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_observation_allocations_bed_id",
                schema: "er",
                table: "observation_allocations",
                column: "bed_id",
                unique: true,
                filter: "is_deleted = false AND released_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_observation_allocations_emergency_visit_id",
                schema: "er",
                table: "observation_allocations",
                column: "emergency_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_triages_emergency_visit_id",
                schema: "er",
                table: "triages",
                column: "emergency_visit_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_triages_triaged_by_user_id",
                schema: "er",
                table: "triages",
                column: "triaged_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_admissions_emergency_visits_referring_emergency_visit_id",
                schema: "ipd",
                table: "admissions",
                column: "referring_emergency_visit_id",
                principalSchema: "er",
                principalTable: "emergency_visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_bills_emergency_visits_emergency_visit_id",
                schema: "billing",
                table: "bills",
                column: "emergency_visit_id",
                principalSchema: "er",
                principalTable: "emergency_visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_lab_orders_emergency_visits_emergency_visit_id",
                schema: "lab",
                table: "lab_orders",
                column: "emergency_visit_id",
                principalSchema: "er",
                principalTable: "emergency_visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_rad_orders_emergency_visits_emergency_visit_id",
                schema: "rad",
                table: "rad_orders",
                column: "emergency_visit_id",
                principalSchema: "er",
                principalTable: "emergency_visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_admissions_emergency_visits_referring_emergency_visit_id",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.DropForeignKey(
                name: "fk_bills_emergency_visits_emergency_visit_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropForeignKey(
                name: "fk_lab_orders_emergency_visits_emergency_visit_id",
                schema: "lab",
                table: "lab_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_rad_orders_emergency_visits_emergency_visit_id",
                schema: "rad",
                table: "rad_orders");

            migrationBuilder.DropTable(
                name: "accident_case_records",
                schema: "er");

            migrationBuilder.DropTable(
                name: "emergency_assessments",
                schema: "er");

            migrationBuilder.DropTable(
                name: "mlc_records",
                schema: "er");

            migrationBuilder.DropTable(
                name: "observation_allocations",
                schema: "er");

            migrationBuilder.DropTable(
                name: "triages",
                schema: "er");

            migrationBuilder.DropTable(
                name: "emergency_visits",
                schema: "er");

            migrationBuilder.DropIndex(
                name: "ix_rad_orders_emergency_visit_id",
                schema: "rad",
                table: "rad_orders");

            migrationBuilder.DropIndex(
                name: "ix_lab_orders_emergency_visit_id",
                schema: "lab",
                table: "lab_orders");

            migrationBuilder.DropIndex(
                name: "ix_bills_emergency_visit_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropIndex(
                name: "ix_admissions_referring_emergency_visit_id",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.DropColumn(
                name: "is_observation",
                schema: "master",
                table: "wards");

            migrationBuilder.DropColumn(
                name: "emergency_visit_id",
                schema: "rad",
                table: "rad_orders");

            migrationBuilder.DropColumn(
                name: "emergency_visit_id",
                schema: "lab",
                table: "lab_orders");

            migrationBuilder.DropColumn(
                name: "emergency_visit_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "referring_emergency_visit_id",
                schema: "ipd",
                table: "admissions");
        }
    }
}
