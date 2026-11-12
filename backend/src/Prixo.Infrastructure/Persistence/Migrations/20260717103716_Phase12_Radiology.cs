using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_Radiology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rad");

            migrationBuilder.CreateTable(
                name: "rad_orders",
                schema: "rad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    op_visit_id = table.Column<long>(type: "bigint", nullable: true),
                    admission_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_rad_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_rad_orders_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_orders_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_orders_clinical_orders_clinical_order_id",
                        column: x => x.clinical_order_id,
                        principalSchema: "opd",
                        principalTable: "clinical_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_orders_doctors_ordered_by_doctor_id",
                        column: x => x.ordered_by_doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_orders_op_visits_op_visit_id",
                        column: x => x.op_visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_orders_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "radiology_tests",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    modality_id = table.Column<long>(type: "bigint", nullable: false),
                    body_part = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requires_contrast = table.Column<bool>(type: "boolean", nullable: false),
                    prep_instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    service_id = table.Column<long>(type: "bigint", nullable: true),
                    tat_hours = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_radiology_tests", x => x.id);
                    table.ForeignKey(
                        name: "fk_radiology_tests_modalities_modality_id",
                        column: x => x.modality_id,
                        principalSchema: "master",
                        principalTable: "modalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_radiology_tests_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "master",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rad_order_items",
                schema: "rad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rad_order_id = table.Column<long>(type: "bigint", nullable: false),
                    radiology_test_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    accession_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    pacs_study_uid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    image_captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    image_captured_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rad_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_rad_order_items_rad_orders_rad_order_id",
                        column: x => x.rad_order_id,
                        principalSchema: "rad",
                        principalTable: "rad_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_order_items_radiology_tests_radiology_test_id",
                        column: x => x.radiology_test_id,
                        principalSchema: "master",
                        principalTable: "radiology_tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_templates",
                schema: "rad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    radiology_test_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    findings_template = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    impression_template = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_templates_radiology_tests_radiology_test_id",
                        column: x => x.radiology_test_id,
                        principalSchema: "master",
                        principalTable: "radiology_tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contrast_consents",
                schema: "rad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rad_order_item_id = table.Column<long>(type: "bigint", nullable: false),
                    signed_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    relationship_to_patient = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    document_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contrast_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_contrast_consents_rad_order_items_rad_order_item_id",
                        column: x => x.rad_order_item_id,
                        principalSchema: "rad",
                        principalTable: "rad_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rad_schedules",
                schema: "rad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rad_order_item_id = table.Column<long>(type: "bigint", nullable: false),
                    modality_id = table.Column<long>(type: "bigint", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    scheduled_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rad_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_rad_schedules_modalities_modality_id",
                        column: x => x.modality_id,
                        principalSchema: "master",
                        principalTable: "modalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_schedules_rad_order_items_rad_order_item_id",
                        column: x => x.rad_order_item_id,
                        principalSchema: "rad",
                        principalTable: "rad_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_schedules_users_scheduled_by_user_id",
                        column: x => x.scheduled_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rad_reports",
                schema: "rad",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rad_order_item_id = table.Column<long>(type: "bigint", nullable: false),
                    template_id = table.Column<long>(type: "bigint", nullable: true),
                    findings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    impression = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    entered_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    entered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    verified_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approver_signature_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rad_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_rad_reports_rad_order_items_rad_order_item_id",
                        column: x => x.rad_order_item_id,
                        principalSchema: "rad",
                        principalTable: "rad_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_reports_rad_report_templates_template_id",
                        column: x => x.template_id,
                        principalSchema: "rad",
                        principalTable: "report_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_reports_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_reports_users_entered_by_user_id",
                        column: x => x.entered_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rad_reports_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contrast_consents_rad_order_item_id",
                schema: "rad",
                table: "contrast_consents",
                column: "rad_order_item_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_rad_order_items_accession_number",
                schema: "rad",
                table: "rad_order_items",
                column: "accession_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_rad_order_items_rad_order_id",
                schema: "rad",
                table: "rad_order_items",
                column: "rad_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_order_items_radiology_test_id",
                schema: "rad",
                table: "rad_order_items",
                column: "radiology_test_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_admission_id",
                schema: "rad",
                table: "rad_orders",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_branch_id_order_date_status",
                schema: "rad",
                table: "rad_orders",
                columns: new[] { "branch_id", "order_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_clinical_order_id",
                schema: "rad",
                table: "rad_orders",
                column: "clinical_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_op_visit_id",
                schema: "rad",
                table: "rad_orders",
                column: "op_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_order_number",
                schema: "rad",
                table: "rad_orders",
                column: "order_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_ordered_by_doctor_id",
                schema: "rad",
                table: "rad_orders",
                column: "ordered_by_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_patient_id_order_date",
                schema: "rad",
                table: "rad_orders",
                columns: new[] { "patient_id", "order_date" });

            migrationBuilder.CreateIndex(
                name: "ix_rad_reports_approved_by_user_id",
                schema: "rad",
                table: "rad_reports",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_reports_entered_by_user_id",
                schema: "rad",
                table: "rad_reports",
                column: "entered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_reports_rad_order_item_id",
                schema: "rad",
                table: "rad_reports",
                column: "rad_order_item_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_rad_reports_template_id",
                schema: "rad",
                table: "rad_reports",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_reports_verified_by_user_id",
                schema: "rad",
                table: "rad_reports",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_rad_schedules_modality_id_scheduled_at",
                schema: "rad",
                table: "rad_schedules",
                columns: new[] { "modality_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_rad_schedules_rad_order_item_id",
                schema: "rad",
                table: "rad_schedules",
                column: "rad_order_item_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_rad_schedules_scheduled_by_user_id",
                schema: "rad",
                table: "rad_schedules",
                column: "scheduled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_radiology_tests_code",
                schema: "master",
                table: "radiology_tests",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_radiology_tests_modality_id",
                schema: "master",
                table: "radiology_tests",
                column: "modality_id");

            migrationBuilder.CreateIndex(
                name: "ix_radiology_tests_service_id",
                schema: "master",
                table: "radiology_tests",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_templates_radiology_test_id",
                schema: "rad",
                table: "report_templates",
                column: "radiology_test_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contrast_consents",
                schema: "rad");

            migrationBuilder.DropTable(
                name: "rad_reports",
                schema: "rad");

            migrationBuilder.DropTable(
                name: "rad_schedules",
                schema: "rad");

            migrationBuilder.DropTable(
                name: "report_templates",
                schema: "rad");

            migrationBuilder.DropTable(
                name: "rad_order_items",
                schema: "rad");

            migrationBuilder.DropTable(
                name: "rad_orders",
                schema: "rad");

            migrationBuilder.DropTable(
                name: "radiology_tests",
                schema: "master");
        }
    }
}
