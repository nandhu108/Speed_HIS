using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase17_Mrd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mrd");

            migrationBuilder.CreateTable(
                name: "coding_worklist",
                schema: "mrd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    coded_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    coded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_coding_worklist", x => x.id);
                    table.ForeignKey(
                        name: "fk_coding_worklist_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_coding_worklist_users_coded_by_user_id",
                        column: x => x.coded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_audits",
                schema: "mrd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    discharge_summary_signed = table.Column<bool>(type: "boolean", nullable: false),
                    consents_present = table.Column<bool>(type: "boolean", nullable: false),
                    progress_notes_complete = table.Column<bool>(type: "boolean", nullable: false),
                    investigation_reports_present = table.Column<bool>(type: "boolean", nullable: false),
                    nursing_records_complete = table.Column<bool>(type: "boolean", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    missing_documents_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    audited_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    audited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_audits", x => x.id);
                    table.ForeignKey(
                        name: "fk_file_audits_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_audits_users_audited_by_user_id",
                        column: x => x.audited_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "record_document_types",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_record_document_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "record_files",
                schema: "mrd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    file_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    current_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    current_custodian_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    destroyed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_record_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_record_files_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_record_files_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_record_files_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_record_files_users_current_custodian_user_id",
                        column: x => x.current_custodian_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retention_policies",
                schema: "mrd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    retention_years = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scanned_documents",
                schema: "mrd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type_id = table.Column<long>(type: "bigint", nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("pk_scanned_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_scanned_documents_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_scanned_documents_record_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalSchema: "master",
                        principalTable: "record_document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_scanned_documents_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_movements",
                schema: "mrd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    record_file_id = table.Column<long>(type: "bigint", nullable: false),
                    moved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    from_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    to_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_custodian_user_id = table.Column<long>(type: "bigint", nullable: true),
                    to_custodian_user_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_file_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_file_movements_record_files_record_file_id",
                        column: x => x.record_file_id,
                        principalSchema: "mrd",
                        principalTable: "record_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_movements_users_from_custodian_user_id",
                        column: x => x.from_custodian_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_movements_users_moved_by_user_id",
                        column: x => x.moved_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_movements_users_to_custodian_user_id",
                        column: x => x.to_custodian_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_requests",
                schema: "mrd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    record_file_id = table.Column<long>(type: "bigint", nullable: false),
                    request_type = table.Column<int>(type: "integer", nullable: false),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    requester_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    requester_organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expected_return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reject_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    issued_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    returned_to_user_id = table.Column<long>(type: "bigint", nullable: true),
                    returned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_file_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_file_requests_record_files_record_file_id",
                        column: x => x.record_file_id,
                        principalSchema: "mrd",
                        principalTable: "record_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_requests_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_requests_users_issued_by_user_id",
                        column: x => x.issued_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_requests_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_requests_users_returned_to_user_id",
                        column: x => x.returned_to_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "destruction_register",
                schema: "mrd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    record_file_id = table.Column<long>(type: "bigint", nullable: false),
                    retention_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    eligible_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    destroyed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    destroyed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    destruction_method = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("pk_destruction_register", x => x.id);
                    table.ForeignKey(
                        name: "fk_destruction_register_record_files_record_file_id",
                        column: x => x.record_file_id,
                        principalSchema: "mrd",
                        principalTable: "record_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_destruction_register_retention_policies_retention_policy_id",
                        column: x => x.retention_policy_id,
                        principalSchema: "mrd",
                        principalTable: "retention_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_destruction_register_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_destruction_register_users_destroyed_by_user_id",
                        column: x => x.destroyed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coding_worklist_admission_id",
                schema: "mrd",
                table: "coding_worklist",
                column: "admission_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_coding_worklist_coded_by_user_id",
                schema: "mrd",
                table: "coding_worklist",
                column: "coded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_destruction_register_approved_by_user_id",
                schema: "mrd",
                table: "destruction_register",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_destruction_register_destroyed_by_user_id",
                schema: "mrd",
                table: "destruction_register",
                column: "destroyed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_destruction_register_record_file_id",
                schema: "mrd",
                table: "destruction_register",
                column: "record_file_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_destruction_register_retention_policy_id",
                schema: "mrd",
                table: "destruction_register",
                column: "retention_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_audits_admission_id",
                schema: "mrd",
                table: "file_audits",
                column: "admission_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_file_audits_audited_by_user_id",
                schema: "mrd",
                table: "file_audits",
                column: "audited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_movements_from_custodian_user_id",
                schema: "mrd",
                table: "file_movements",
                column: "from_custodian_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_movements_moved_by_user_id",
                schema: "mrd",
                table: "file_movements",
                column: "moved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_movements_record_file_id_moved_at",
                schema: "mrd",
                table: "file_movements",
                columns: new[] { "record_file_id", "moved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_file_movements_to_custodian_user_id",
                schema: "mrd",
                table: "file_movements",
                column: "to_custodian_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_requests_approved_by_user_id",
                schema: "mrd",
                table: "file_requests",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_requests_issued_by_user_id",
                schema: "mrd",
                table: "file_requests",
                column: "issued_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_requests_record_file_id_status",
                schema: "mrd",
                table: "file_requests",
                columns: new[] { "record_file_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_file_requests_requested_by_user_id",
                schema: "mrd",
                table: "file_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_requests_returned_to_user_id",
                schema: "mrd",
                table: "file_requests",
                column: "returned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_document_types_code",
                schema: "master",
                table: "record_document_types",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_record_files_admission_id",
                schema: "mrd",
                table: "record_files",
                column: "admission_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_record_files_branch_id",
                schema: "mrd",
                table: "record_files",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_files_created_by_user_id",
                schema: "mrd",
                table: "record_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_files_current_custodian_user_id",
                schema: "mrd",
                table: "record_files",
                column: "current_custodian_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_files_file_number",
                schema: "mrd",
                table: "record_files",
                column: "file_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_retention_policies_code",
                schema: "mrd",
                table: "retention_policies",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_scanned_documents_admission_id",
                schema: "mrd",
                table: "scanned_documents",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_scanned_documents_document_type_id",
                schema: "mrd",
                table: "scanned_documents",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_scanned_documents_uploaded_by_user_id",
                schema: "mrd",
                table: "scanned_documents",
                column: "uploaded_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coding_worklist",
                schema: "mrd");

            migrationBuilder.DropTable(
                name: "destruction_register",
                schema: "mrd");

            migrationBuilder.DropTable(
                name: "file_audits",
                schema: "mrd");

            migrationBuilder.DropTable(
                name: "file_movements",
                schema: "mrd");

            migrationBuilder.DropTable(
                name: "file_requests",
                schema: "mrd");

            migrationBuilder.DropTable(
                name: "scanned_documents",
                schema: "mrd");

            migrationBuilder.DropTable(
                name: "retention_policies",
                schema: "mrd");

            migrationBuilder.DropTable(
                name: "record_files",
                schema: "mrd");

            migrationBuilder.DropTable(
                name: "record_document_types",
                schema: "master");
        }
    }
}
