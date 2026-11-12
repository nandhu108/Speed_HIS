using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20261026_Patients_AbdmM1M2M3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abdm_care_contexts",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    abha_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    context_type = table.Column<int>(type: "integer", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_linked = table.Column<bool>(type: "boolean", nullable: false),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abdm_care_contexts", x => x.id);
                    table.ForeignKey(
                        name: "fk_abdm_care_contexts_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "abdm_consent_requests",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    consent_request_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    consent_artifact_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    abha_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    abha_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hi_types = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    date_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expiry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    status_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    requester_doctor_id = table.Column<long>(type: "bigint", nullable: true),
                    requester_doctor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abdm_consent_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_abdm_consent_requests_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "abdm_data_transfer_logs",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    consent_artifact_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    care_context_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hi_type = table.Column<int>(type: "integer", nullable: false),
                    recipient_hiu_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    transfer_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fhir_bundle_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abdm_data_transfer_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abdm_external_health_records",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    consent_request_id = table.Column<long>(type: "bigint", nullable: true),
                    hi_type = table.Column<int>(type: "integer", nullable: false),
                    source_facility_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_facility_hip_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    record_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    doctor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    clinical_summary = table.Column<string>(type: "text", nullable: false),
                    fhir_bundle_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abdm_external_health_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_abdm_external_health_records_abdm_consent_requests_consent_",
                        column: x => x.consent_request_id,
                        principalSchema: "patient",
                        principalTable: "abdm_consent_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_abdm_external_health_records_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_abdm_care_contexts_patient_id_reference_number",
                schema: "patient",
                table: "abdm_care_contexts",
                columns: new[] { "patient_id", "reference_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_abdm_consent_requests_consent_request_id",
                schema: "patient",
                table: "abdm_consent_requests",
                column: "consent_request_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_abdm_consent_requests_patient_id_status",
                schema: "patient",
                table: "abdm_consent_requests",
                columns: new[] { "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_abdm_data_transfer_logs_consent_artifact_id",
                schema: "patient",
                table: "abdm_data_transfer_logs",
                column: "consent_artifact_id");

            migrationBuilder.CreateIndex(
                name: "ix_abdm_data_transfer_logs_transaction_id",
                schema: "patient",
                table: "abdm_data_transfer_logs",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_abdm_external_health_records_consent_request_id",
                schema: "patient",
                table: "abdm_external_health_records",
                column: "consent_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_abdm_external_health_records_patient_id",
                schema: "patient",
                table: "abdm_external_health_records",
                column: "patient_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abdm_care_contexts",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "abdm_data_transfer_logs",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "abdm_external_health_records",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "abdm_consent_requests",
                schema: "patient");
        }
    }
}
