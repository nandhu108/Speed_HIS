using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_Opd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "opd");

            migrationBuilder.CreateTable(
                name: "op_visits",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    appointment_id = table.Column<long>(type: "bigint", nullable: true),
                    visit_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    visit_date = table.Column<DateOnly>(type: "date", nullable: false),
                    token_number = table.Column<int>(type: "integer", nullable: false),
                    visit_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    consultation_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    in_consult_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_op_visits", x => x.id);
                    table.ForeignKey(
                        name: "fk_op_visits_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "appointment",
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_op_visits_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_op_visits_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_op_visits_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prescription_templates",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescription_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescription_templates_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clinical_orders",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    order_type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_clinical_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_clinical_orders_op_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "follow_ups",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    follow_up_date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("pk_follow_ups", x => x.id);
                    table.ForeignKey(
                        name: "fk_follow_ups_op_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescriptions_op_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "referrals",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    referred_to_doctor_id = table.Column<long>(type: "bigint", nullable: true),
                    external_details = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referrals", x => x.id);
                    table.ForeignKey(
                        name: "fk_referrals_doctors_referred_to_doctor_id",
                        column: x => x.referred_to_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_referrals_op_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visit_complaints",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    complaint = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    duration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visit_complaints", x => x.id);
                    table.ForeignKey(
                        name: "fk_visit_complaints_op_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visit_diagnoses",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    icd_code_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_visit_diagnoses", x => x.id);
                    table.ForeignKey(
                        name: "fk_visit_diagnoses_icd_codes_icd_code_id",
                        column: x => x.icd_code_id,
                        principalSchema: "master",
                        principalTable: "icd_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_visit_diagnoses_op_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visit_examinations",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    history_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    examination_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visit_examinations", x => x.id);
                    table.ForeignKey(
                        name: "fk_visit_examinations_op_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visit_vitals",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    height_cm = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    bmi = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    temperature_f = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    pulse_rate = table.Column<int>(type: "integer", nullable: true),
                    respiratory_rate = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure_systolic = table.Column<int>(type: "integer", nullable: true),
                    blood_pressure_diastolic = table.Column<int>(type: "integer", nullable: true),
                    sp_o2 = table.Column<int>(type: "integer", nullable: true),
                    random_blood_sugar = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visit_vitals", x => x.id);
                    table.ForeignKey(
                        name: "fk_visit_vitals_op_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prescription_template_items",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prescription_template_id = table.Column<long>(type: "bigint", nullable: false),
                    drug_id = table.Column<long>(type: "bigint", nullable: false),
                    dose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    duration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    route = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    instructions = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescription_template_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescription_template_items_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prescription_template_items_prescription_templates_prescrip",
                        column: x => x.prescription_template_id,
                        principalSchema: "opd",
                        principalTable: "prescription_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prescription_items",
                schema: "opd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prescription_id = table.Column<long>(type: "bigint", nullable: false),
                    drug_id = table.Column<long>(type: "bigint", nullable: false),
                    dose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    duration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    route = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    instructions = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescription_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescription_items_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prescription_items_prescriptions_prescription_id",
                        column: x => x.prescription_id,
                        principalSchema: "opd",
                        principalTable: "prescriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clinical_orders_visit_id_order_type",
                schema: "opd",
                table: "clinical_orders",
                columns: new[] { "visit_id", "order_type" });

            migrationBuilder.CreateIndex(
                name: "ix_follow_ups_visit_id",
                schema: "opd",
                table: "follow_ups",
                column: "visit_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_appointment_id",
                schema: "opd",
                table: "op_visits",
                column: "appointment_id",
                unique: true,
                filter: "is_deleted = false AND appointment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_branch_id_visit_date_status",
                schema: "opd",
                table: "op_visits",
                columns: new[] { "branch_id", "visit_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_doctor_id",
                schema: "opd",
                table: "op_visits",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_patient_id_doctor_id_visit_date",
                schema: "opd",
                table: "op_visits",
                columns: new[] { "patient_id", "doctor_id", "visit_date" });

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_visit_number",
                schema: "opd",
                table: "op_visits",
                column: "visit_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_items_drug_id",
                schema: "opd",
                table: "prescription_items",
                column: "drug_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_items_prescription_id",
                schema: "opd",
                table: "prescription_items",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_template_items_drug_id",
                schema: "opd",
                table: "prescription_template_items",
                column: "drug_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_template_items_prescription_template_id",
                schema: "opd",
                table: "prescription_template_items",
                column: "prescription_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_templates_doctor_id_name",
                schema: "opd",
                table: "prescription_templates",
                columns: new[] { "doctor_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_visit_id",
                schema: "opd",
                table: "prescriptions",
                column: "visit_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_referrals_referred_to_doctor_id",
                schema: "opd",
                table: "referrals",
                column: "referred_to_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_referrals_visit_id",
                schema: "opd",
                table: "referrals",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_visit_complaints_visit_id",
                schema: "opd",
                table: "visit_complaints",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_visit_diagnoses_icd_code_id",
                schema: "opd",
                table: "visit_diagnoses",
                column: "icd_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_visit_diagnoses_visit_id",
                schema: "opd",
                table: "visit_diagnoses",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_visit_examinations_visit_id",
                schema: "opd",
                table: "visit_examinations",
                column: "visit_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_visit_vitals_visit_id",
                schema: "opd",
                table: "visit_vitals",
                column: "visit_id",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clinical_orders",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "follow_ups",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "prescription_items",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "prescription_template_items",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "referrals",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "visit_complaints",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "visit_diagnoses",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "visit_examinations",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "visit_vitals",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "prescriptions",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "prescription_templates",
                schema: "opd");

            migrationBuilder.DropTable(
                name: "op_visits",
                schema: "opd");
        }
    }
}
