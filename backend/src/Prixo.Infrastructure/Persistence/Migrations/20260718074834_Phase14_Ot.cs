using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_Ot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ot");

            migrationBuilder.CreateTable(
                name: "theatres",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    table.PrimaryKey("pk_theatres", x => x.id);
                    table.ForeignKey(
                        name: "fk_theatres_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    theatre_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_equipment", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipment_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipment_theatres_theatre_id",
                        column: x => x.theatre_id,
                        principalSchema: "ot",
                        principalTable: "theatres",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "surgery_bookings",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    theatre_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    procedure_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    surgeon_id = table.Column<long>(type: "bigint", nullable: false),
                    anaesthetist_id = table.Column<long>(type: "bigint", nullable: true),
                    scheduled_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    scheduled_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actual_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    status_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    patient_consent_id = table.Column<long>(type: "bigint", nullable: true),
                    consent_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consent_verified_by_user_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_surgery_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_surgery_bookings_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_bookings_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_bookings_doctors_anaesthetist_id",
                        column: x => x.anaesthetist_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_bookings_doctors_surgeon_id",
                        column: x => x.surgeon_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_bookings_patient_consents_patient_consent_id",
                        column: x => x.patient_consent_id,
                        principalSchema: "patient",
                        principalTable: "patient_consents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_bookings_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_bookings_theatres_theatre_id",
                        column: x => x.theatre_id,
                        principalSchema: "ot",
                        principalTable: "theatres",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_bookings_users_consent_verified_by_user_id",
                        column: x => x.consent_verified_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consumable_usages",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bill_item_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consumable_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_consumable_usages_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consumable_usages_bill_items_bill_item_id",
                        column: x => x.bill_item_id,
                        principalSchema: "billing",
                        principalTable: "bill_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consumable_usages_surgery_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "ot",
                        principalTable: "surgery_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "implant_usages",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bill_item_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_implant_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_implant_usages_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "pharmacy",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_implant_usages_bill_items_bill_item_id",
                        column: x => x.bill_item_id,
                        principalSchema: "billing",
                        principalTable: "bill_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_implant_usages_surgery_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "ot",
                        principalTable: "surgery_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "intra_op_anaesthesia_records",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    anaesthesia_type = table.Column<int>(type: "integer", nullable: false),
                    administered_by_doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intra_op_anaesthesia_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_intra_op_anaesthesia_records_doctors_administered_by_doctor",
                        column: x => x.administered_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_intra_op_anaesthesia_records_surgery_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "ot",
                        principalTable: "surgery_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operative_notes",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    surgeon_id = table.Column<long>(type: "bigint", nullable: false),
                    procedure_performed = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    findings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    complications = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operative_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_operative_notes_doctors_surgeon_id",
                        column: x => x.surgeon_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_operative_notes_surgery_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "ot",
                        principalTable: "surgery_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_op_notes",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    recovery_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    vitals_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    complications = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    shifted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_op_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_post_op_notes_surgery_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "ot",
                        principalTable: "surgery_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pre_anaesthesia_assessments",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    assessed_by_doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    asa_grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    airway_assessment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    comorbidities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    investigations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    anaesthesia_plan = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pre_anaesthesia_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_pre_anaesthesia_assessments_doctors_assessed_by_doctor_id",
                        column: x => x.assessed_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pre_anaesthesia_assessments_surgery_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "ot",
                        principalTable: "surgery_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "surgery_team_members",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    doctor_id = table.Column<long>(type: "bigint", nullable: true),
                    staff_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgery_team_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_surgery_team_members_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_team_members_surgery_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "ot",
                        principalTable: "surgery_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "surgical_safety_checklists",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_identity_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    site_marked = table.Column<bool>(type: "boolean", nullable: false),
                    anaesthesia_safety_check_complete = table.Column<bool>(type: "boolean", nullable: false),
                    pulse_oximeter_functioning = table.Column<bool>(type: "boolean", nullable: false),
                    known_allergy = table.Column<bool>(type: "boolean", nullable: false),
                    difficult_airway_risk = table.Column<bool>(type: "boolean", nullable: false),
                    risk_of_blood_loss = table.Column<bool>(type: "boolean", nullable: false),
                    sign_in_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sign_in_completed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    team_introduced = table.Column<bool>(type: "boolean", nullable: false),
                    patient_site_procedure_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    critical_events_reviewed = table.Column<bool>(type: "boolean", nullable: false),
                    antibiotic_prophylaxis_given = table.Column<bool>(type: "boolean", nullable: false),
                    imaging_displayed = table.Column<bool>(type: "boolean", nullable: false),
                    time_out_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    time_out_completed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    procedure_name_recorded = table.Column<bool>(type: "boolean", nullable: false),
                    instrument_sponge_needle_count_correct = table.Column<bool>(type: "boolean", nullable: false),
                    specimen_labeled = table.Column<bool>(type: "boolean", nullable: false),
                    equipment_problems_addressed = table.Column<bool>(type: "boolean", nullable: false),
                    recovery_concerns_reviewed = table.Column<bool>(type: "boolean", nullable: false),
                    sign_out_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sign_out_completed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgical_safety_checklists", x => x.id);
                    table.ForeignKey(
                        name: "fk_surgical_safety_checklists_surgery_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "ot",
                        principalTable: "surgery_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgical_safety_checklists_users_sign_in_completed_by_user_",
                        column: x => x.sign_in_completed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgical_safety_checklists_users_sign_out_completed_by_user",
                        column: x => x.sign_out_completed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgical_safety_checklists_users_time_out_completed_by_user",
                        column: x => x.time_out_completed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_consumable_usages_batch_id",
                schema: "ot",
                table: "consumable_usages",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_consumable_usages_bill_item_id",
                schema: "ot",
                table: "consumable_usages",
                column: "bill_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_consumable_usages_booking_id",
                schema: "ot",
                table: "consumable_usages",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_branch_id_code",
                schema: "ot",
                table: "equipment",
                columns: new[] { "branch_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_theatre_id",
                schema: "ot",
                table: "equipment",
                column: "theatre_id");

            migrationBuilder.CreateIndex(
                name: "ix_implant_usages_batch_id",
                schema: "ot",
                table: "implant_usages",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_implant_usages_bill_item_id",
                schema: "ot",
                table: "implant_usages",
                column: "bill_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_implant_usages_booking_id",
                schema: "ot",
                table: "implant_usages",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_intra_op_anaesthesia_records_administered_by_doctor_id",
                schema: "ot",
                table: "intra_op_anaesthesia_records",
                column: "administered_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_intra_op_anaesthesia_records_booking_id",
                schema: "ot",
                table: "intra_op_anaesthesia_records",
                column: "booking_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_operative_notes_booking_id",
                schema: "ot",
                table: "operative_notes",
                column: "booking_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_operative_notes_surgeon_id",
                schema: "ot",
                table: "operative_notes",
                column: "surgeon_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_op_notes_booking_id",
                schema: "ot",
                table: "post_op_notes",
                column: "booking_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_pre_anaesthesia_assessments_assessed_by_doctor_id",
                schema: "ot",
                table: "pre_anaesthesia_assessments",
                column: "assessed_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_anaesthesia_assessments_booking_id",
                schema: "ot",
                table: "pre_anaesthesia_assessments",
                column: "booking_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_admission_id",
                schema: "ot",
                table: "surgery_bookings",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_anaesthetist_id_scheduled_start",
                schema: "ot",
                table: "surgery_bookings",
                columns: new[] { "anaesthetist_id", "scheduled_start" });

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_booking_number",
                schema: "ot",
                table: "surgery_bookings",
                column: "booking_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_branch_id",
                schema: "ot",
                table: "surgery_bookings",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_consent_verified_by_user_id",
                schema: "ot",
                table: "surgery_bookings",
                column: "consent_verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_patient_consent_id",
                schema: "ot",
                table: "surgery_bookings",
                column: "patient_consent_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_patient_id",
                schema: "ot",
                table: "surgery_bookings",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_surgeon_id_scheduled_start",
                schema: "ot",
                table: "surgery_bookings",
                columns: new[] { "surgeon_id", "scheduled_start" });

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_theatre_id_scheduled_start",
                schema: "ot",
                table: "surgery_bookings",
                columns: new[] { "theatre_id", "scheduled_start" });

            migrationBuilder.CreateIndex(
                name: "ix_surgery_team_members_booking_id",
                schema: "ot",
                table: "surgery_team_members",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_team_members_doctor_id",
                schema: "ot",
                table: "surgery_team_members",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgical_safety_checklists_booking_id",
                schema: "ot",
                table: "surgical_safety_checklists",
                column: "booking_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_surgical_safety_checklists_sign_in_completed_by_user_id",
                schema: "ot",
                table: "surgical_safety_checklists",
                column: "sign_in_completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgical_safety_checklists_sign_out_completed_by_user_id",
                schema: "ot",
                table: "surgical_safety_checklists",
                column: "sign_out_completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgical_safety_checklists_time_out_completed_by_user_id",
                schema: "ot",
                table: "surgical_safety_checklists",
                column: "time_out_completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_theatres_branch_id_code",
                schema: "ot",
                table: "theatres",
                columns: new[] { "branch_id", "code" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumable_usages",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "equipment",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "implant_usages",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "intra_op_anaesthesia_records",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "operative_notes",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "post_op_notes",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "pre_anaesthesia_assessments",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "surgery_team_members",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "surgical_safety_checklists",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "surgery_bookings",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "theatres",
                schema: "ot");
        }
    }
}
