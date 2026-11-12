using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase11_Emr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "emr");

            migrationBuilder.CreateTable(
                name: "allergies",
                schema: "emr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    allergen_type = table.Column<int>(type: "integer", nullable: false),
                    allergen_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reaction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allergies", x => x.id);
                    table.ForeignKey(
                        name: "fk_allergies_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_allergies_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "emr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    attachment_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    virus_scan_status = table.Column<int>(type: "integer", nullable: false),
                    uploaded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_attachments_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attachments_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clinical_notes",
                schema: "emr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    note_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    content = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    note_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    op_visit_id = table.Column<long>(type: "bigint", nullable: true),
                    admission_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clinical_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_clinical_notes_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_clinical_notes_op_visits_op_visit_id",
                        column: x => x.op_visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_clinical_notes_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_clinical_notes_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "family_histories",
                schema: "emr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    relation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_family_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_family_histories_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_family_histories_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "immunizations",
                schema: "emr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    vaccine_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dose_number = table.Column<int>(type: "integer", nullable: true),
                    administered_date = table.Column<DateOnly>(type: "date", nullable: false),
                    next_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    administered_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_immunizations", x => x.id);
                    table.ForeignKey(
                        name: "fk_immunizations_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_immunizations_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medical_histories",
                schema: "emr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    diagnosed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medical_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_medical_histories_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medical_histories_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "problem_list",
                schema: "emr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    icd_code_id = table.Column<long>(type: "bigint", nullable: true),
                    diagnosis_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    identified_date = table.Column<DateOnly>(type: "date", nullable: false),
                    resolved_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_problem_list", x => x.id);
                    table.ForeignKey(
                        name: "fk_problem_list_icd_codes_icd_code_id",
                        column: x => x.icd_code_id,
                        principalSchema: "master",
                        principalTable: "icd_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_problem_list_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_problem_list_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "surgical_histories",
                schema: "emr",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    procedure_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    procedure_date = table.Column<DateOnly>(type: "date", nullable: true),
                    surgeon = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgical_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_surgical_histories_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgical_histories_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_allergies_patient_id_is_active",
                schema: "emr",
                table: "allergies",
                columns: new[] { "patient_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_allergies_recorded_by_user_id",
                schema: "emr",
                table: "allergies",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_patient_id",
                schema: "emr",
                table: "attachments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_uploaded_by_user_id",
                schema: "emr",
                table: "attachments",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_clinical_notes_admission_id",
                schema: "emr",
                table: "clinical_notes",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_clinical_notes_op_visit_id",
                schema: "emr",
                table: "clinical_notes",
                column: "op_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_clinical_notes_patient_id_note_at",
                schema: "emr",
                table: "clinical_notes",
                columns: new[] { "patient_id", "note_at" });

            migrationBuilder.CreateIndex(
                name: "ix_clinical_notes_recorded_by_user_id",
                schema: "emr",
                table: "clinical_notes",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_histories_patient_id",
                schema: "emr",
                table: "family_histories",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_histories_recorded_by_user_id",
                schema: "emr",
                table: "family_histories",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_immunizations_patient_id",
                schema: "emr",
                table: "immunizations",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_immunizations_recorded_by_user_id",
                schema: "emr",
                table: "immunizations",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_medical_histories_patient_id",
                schema: "emr",
                table: "medical_histories",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_medical_histories_recorded_by_user_id",
                schema: "emr",
                table: "medical_histories",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_problem_list_icd_code_id",
                schema: "emr",
                table: "problem_list",
                column: "icd_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_problem_list_patient_id_status",
                schema: "emr",
                table: "problem_list",
                columns: new[] { "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_problem_list_recorded_by_user_id",
                schema: "emr",
                table: "problem_list",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgical_histories_patient_id",
                schema: "emr",
                table: "surgical_histories",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgical_histories_recorded_by_user_id",
                schema: "emr",
                table: "surgical_histories",
                column: "recorded_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allergies",
                schema: "emr");

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "emr");

            migrationBuilder.DropTable(
                name: "clinical_notes",
                schema: "emr");

            migrationBuilder.DropTable(
                name: "family_histories",
                schema: "emr");

            migrationBuilder.DropTable(
                name: "immunizations",
                schema: "emr");

            migrationBuilder.DropTable(
                name: "medical_histories",
                schema: "emr");

            migrationBuilder.DropTable(
                name: "problem_list",
                schema: "emr");

            migrationBuilder.DropTable(
                name: "surgical_histories",
                schema: "emr");
        }
    }
}
