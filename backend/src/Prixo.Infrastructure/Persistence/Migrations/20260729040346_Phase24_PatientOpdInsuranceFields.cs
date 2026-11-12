using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase24_PatientOpdInsuranceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "insurance");

            migrationBuilder.AddColumn<long>(
                name: "area_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "city_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "country_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "insurance_required",
                schema: "patient",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_permanent_same_as_present",
                schema: "patient",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_vip",
                schema: "patient",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "language_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "marital_status_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "monthly_income",
                schema: "patient",
                table: "patients",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mother_name",
                schema: "patient",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                schema: "patient",
                table: "patients",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "nationality_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "patient_case_type_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "patient_hc_tag_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permanent_address",
                schema: "patient",
                table: "patients",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "religion_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "remarks",
                schema: "patient",
                table: "patients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "salutation_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sms_opt_in",
                schema: "patient",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "specially_abled_category_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "state_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whats_app_number",
                schema: "patient",
                table: "patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "identity_document_type_id",
                schema: "patient",
                table: "patient_identifiers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "booking_type",
                schema: "opd",
                table: "op_visits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_mlc_case",
                schema: "opd",
                table: "op_visits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "session_id",
                schema: "opd",
                table: "op_visits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "visit_class_id",
                schema: "opd",
                table: "op_visits",
                type: "bigint",
                nullable: true);

            // Best-effort backfill from the old free-text City/State/Nationality columns to the
            // newly added FK columns, matching by case-insensitive trimmed name against the
            // existing master.cities/states/nationalities tables. Unmatched rows are left null
            // rather than treated as an error — this is a one-time, best-effort cutover, not a
            // guaranteed 1:1 migration (legacy free text may not match any master row exactly).
            migrationBuilder.Sql(@"
                UPDATE patient.patients p
                SET city_id = c.id
                FROM master.cities c
                WHERE p.city_id IS NULL AND p.city IS NOT NULL
                  AND trim(lower(c.name)) = trim(lower(p.city)) AND c.is_deleted = false;

                UPDATE patient.patients p
                SET state_id = s.id
                FROM master.states s
                WHERE p.state_id IS NULL AND p.state IS NOT NULL
                  AND trim(lower(s.name)) = trim(lower(p.state)) AND s.is_deleted = false;

                UPDATE patient.patients p
                SET nationality_id = n.id
                FROM master.nationalities n
                WHERE p.nationality_id IS NULL AND p.nationality IS NOT NULL
                  AND trim(lower(n.name)) = trim(lower(p.nationality)) AND n.is_deleted = false;
            ");

            migrationBuilder.DropColumn(
                name: "city",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "nationality",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "patient",
                table: "patients");

            migrationBuilder.CreateTable(
                name: "claim_denial_reasons",
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
                    table.PrimaryKey("pk_claim_denial_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "claim_document_types",
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
                    table.PrimaryKey("pk_claim_document_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_document_types",
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
                    table.PrimaryKey("pk_identity_document_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "marital_statuses",
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
                    table.PrimaryKey("pk_marital_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patient_insurance_policies",
                schema: "insurance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    insurance_company_id = table.Column<long>(type: "bigint", nullable: false),
                    policy_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    policy_valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    policy_valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_insurance_policies", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_insurance_policies_insurance_companies_insurance_co",
                        column: x => x.insurance_company_id,
                        principalSchema: "master",
                        principalTable: "insurance_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_insurance_policies_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "salutations",
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
                    table.PrimaryKey("pk_salutations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "visit_classes",
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
                    table.PrimaryKey("pk_visit_classes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "visit_sessions",
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
                    table.PrimaryKey("pk_visit_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pre_auth_requests",
                schema: "insurance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pre_auth_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    policy_id = table.Column<long>(type: "bigint", nullable: false),
                    treatment_type = table.Column<int>(type: "integer", nullable: false),
                    op_visit_id = table.Column<long>(type: "bigint", nullable: true),
                    admission_id = table.Column<long>(type: "bigint", nullable: true),
                    estimated_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    diagnosis = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    approved_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_pre_auth_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_pre_auth_requests_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pre_auth_requests_op_visits_op_visit_id",
                        column: x => x.op_visit_id,
                        principalSchema: "opd",
                        principalTable: "op_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pre_auth_requests_patient_insurance_policies_policy_id",
                        column: x => x.policy_id,
                        principalSchema: "insurance",
                        principalTable: "patient_insurance_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pre_auth_requests_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pre_auth_requests_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pre_auth_requests_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "claims",
                schema: "insurance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    claim_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    policy_id = table.Column<long>(type: "bigint", nullable: false),
                    pre_auth_request_id = table.Column<long>(type: "bigint", nullable: true),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    approved_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    denial_reason_id = table.Column<long>(type: "bigint", nullable: true),
                    denial_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    settled_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    deduction_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    settlement_reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_claims_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claims_claim_denial_reasons_denial_reason_id",
                        column: x => x.denial_reason_id,
                        principalSchema: "master",
                        principalTable: "claim_denial_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claims_patient_insurance_policies_policy_id",
                        column: x => x.policy_id,
                        principalSchema: "insurance",
                        principalTable: "patient_insurance_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claims_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claims_pre_auth_requests_pre_auth_request_id",
                        column: x => x.pre_auth_request_id,
                        principalSchema: "insurance",
                        principalTable: "pre_auth_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claims_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claims_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pre_auth_enhancements",
                schema: "insurance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pre_auth_request_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pre_auth_enhancements", x => x.id);
                    table.ForeignKey(
                        name: "fk_pre_auth_enhancements_pre_auth_requests_pre_auth_request_id",
                        column: x => x.pre_auth_request_id,
                        principalSchema: "insurance",
                        principalTable: "pre_auth_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pre_auth_enhancements_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pre_auth_enhancements_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "claim_documents",
                schema: "insurance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    claim_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type_id = table.Column<long>(type: "bigint", nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_claim_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_claim_documents_claim_document_types_document_type_id",
                        column: x => x.document_type_id,
                        principalSchema: "master",
                        principalTable: "claim_document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claim_documents_claims_claim_id",
                        column: x => x.claim_id,
                        principalSchema: "insurance",
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_claim_documents_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patients_area_id",
                schema: "patient",
                table: "patients",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_city_id",
                schema: "patient",
                table: "patients",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_country_id",
                schema: "patient",
                table: "patients",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_language_id",
                schema: "patient",
                table: "patients",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_marital_status_id",
                schema: "patient",
                table: "patients",
                column: "marital_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_nationality_id",
                schema: "patient",
                table: "patients",
                column: "nationality_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_patient_case_type_id",
                schema: "patient",
                table: "patients",
                column: "patient_case_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_patient_hc_tag_id",
                schema: "patient",
                table: "patients",
                column: "patient_hc_tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_religion_id",
                schema: "patient",
                table: "patients",
                column: "religion_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_salutation_id",
                schema: "patient",
                table: "patients",
                column: "salutation_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_specially_abled_category_id",
                schema: "patient",
                table: "patients",
                column: "specially_abled_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_state_id",
                schema: "patient",
                table: "patients",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_identifiers_identity_document_type_id",
                schema: "patient",
                table: "patient_identifiers",
                column: "identity_document_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_session_id",
                schema: "opd",
                table: "op_visits",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_visit_class_id",
                schema: "opd",
                table: "op_visits",
                column: "visit_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_claim_denial_reasons_code",
                schema: "master",
                table: "claim_denial_reasons",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_claim_document_types_code",
                schema: "master",
                table: "claim_document_types",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_claim_documents_claim_id",
                schema: "insurance",
                table: "claim_documents",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_claim_documents_document_type_id",
                schema: "insurance",
                table: "claim_documents",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_claim_documents_uploaded_by_user_id",
                schema: "insurance",
                table: "claim_documents",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_bill_id",
                schema: "insurance",
                table: "claims",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_branch_id_claim_number",
                schema: "insurance",
                table: "claims",
                columns: new[] { "branch_id", "claim_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_claims_decided_by_user_id",
                schema: "insurance",
                table: "claims",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_denial_reason_id",
                schema: "insurance",
                table: "claims",
                column: "denial_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_patient_id",
                schema: "insurance",
                table: "claims",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_policy_id",
                schema: "insurance",
                table: "claims",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_pre_auth_request_id",
                schema: "insurance",
                table: "claims",
                column: "pre_auth_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_status",
                schema: "insurance",
                table: "claims",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_claims_submitted_by_user_id",
                schema: "insurance",
                table: "claims",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_document_types_code",
                schema: "master",
                table: "identity_document_types",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_marital_statuses_code",
                schema: "master",
                table: "marital_statuses",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_patient_insurance_policies_insurance_company_id",
                schema: "insurance",
                table: "patient_insurance_policies",
                column: "insurance_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_insurance_policies_patient_id",
                schema: "insurance",
                table: "patient_insurance_policies",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_enhancements_decided_by_user_id",
                schema: "insurance",
                table: "pre_auth_enhancements",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_enhancements_pre_auth_request_id",
                schema: "insurance",
                table: "pre_auth_enhancements",
                column: "pre_auth_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_enhancements_requested_by_user_id",
                schema: "insurance",
                table: "pre_auth_enhancements",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_requests_admission_id",
                schema: "insurance",
                table: "pre_auth_requests",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_requests_branch_id_pre_auth_number",
                schema: "insurance",
                table: "pre_auth_requests",
                columns: new[] { "branch_id", "pre_auth_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_requests_decided_by_user_id",
                schema: "insurance",
                table: "pre_auth_requests",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_requests_op_visit_id",
                schema: "insurance",
                table: "pre_auth_requests",
                column: "op_visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_requests_patient_id",
                schema: "insurance",
                table: "pre_auth_requests",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_requests_policy_id",
                schema: "insurance",
                table: "pre_auth_requests",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_requests_requested_by_user_id",
                schema: "insurance",
                table: "pre_auth_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pre_auth_requests_status",
                schema: "insurance",
                table: "pre_auth_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_salutations_code",
                schema: "master",
                table: "salutations",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_visit_classes_code",
                schema: "master",
                table: "visit_classes",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_visit_sessions_code",
                schema: "master",
                table: "visit_sessions",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_op_visits_visit_classes_visit_class_id",
                schema: "opd",
                table: "op_visits",
                column: "visit_class_id",
                principalSchema: "master",
                principalTable: "visit_classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_op_visits_visit_sessions_session_id",
                schema: "opd",
                table: "op_visits",
                column: "session_id",
                principalSchema: "master",
                principalTable: "visit_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patient_identifiers_identity_document_types_identity_docume",
                schema: "patient",
                table: "patient_identifiers",
                column: "identity_document_type_id",
                principalSchema: "master",
                principalTable: "identity_document_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_areas_area_id",
                schema: "patient",
                table: "patients",
                column: "area_id",
                principalSchema: "master",
                principalTable: "areas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_cities_city_id",
                schema: "patient",
                table: "patients",
                column: "city_id",
                principalSchema: "master",
                principalTable: "cities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_languages_language_id",
                schema: "patient",
                table: "patients",
                column: "language_id",
                principalSchema: "master",
                principalTable: "languages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_marital_statuses_marital_status_id",
                schema: "patient",
                table: "patients",
                column: "marital_status_id",
                principalSchema: "master",
                principalTable: "marital_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_nationalities_country_id",
                schema: "patient",
                table: "patients",
                column: "country_id",
                principalSchema: "master",
                principalTable: "nationalities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_nationalities_nationality_id",
                schema: "patient",
                table: "patients",
                column: "nationality_id",
                principalSchema: "master",
                principalTable: "nationalities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_patient_case_types_patient_case_type_id",
                schema: "patient",
                table: "patients",
                column: "patient_case_type_id",
                principalSchema: "master",
                principalTable: "patient_case_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_patient_hc_tags_patient_hc_tag_id",
                schema: "patient",
                table: "patients",
                column: "patient_hc_tag_id",
                principalSchema: "master",
                principalTable: "patient_hc_tags",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_religions_religion_id",
                schema: "patient",
                table: "patients",
                column: "religion_id",
                principalSchema: "master",
                principalTable: "religions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_salutations_salutation_id",
                schema: "patient",
                table: "patients",
                column: "salutation_id",
                principalSchema: "master",
                principalTable: "salutations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_specially_abled_categories_specially_abled_categor",
                schema: "patient",
                table: "patients",
                column: "specially_abled_category_id",
                principalSchema: "master",
                principalTable: "specially_abled_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patients_states_state_id",
                schema: "patient",
                table: "patients",
                column: "state_id",
                principalSchema: "master",
                principalTable: "states",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_op_visits_visit_classes_visit_class_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropForeignKey(
                name: "fk_op_visits_visit_sessions_session_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropForeignKey(
                name: "fk_patient_identifiers_identity_document_types_identity_docume",
                schema: "patient",
                table: "patient_identifiers");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_areas_area_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_cities_city_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_languages_language_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_marital_statuses_marital_status_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_nationalities_country_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_nationalities_nationality_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_patient_case_types_patient_case_type_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_patient_hc_tags_patient_hc_tag_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_religions_religion_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_salutations_salutation_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_specially_abled_categories_specially_abled_categor",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "fk_patients_states_state_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropTable(
                name: "claim_documents",
                schema: "insurance");

            migrationBuilder.DropTable(
                name: "identity_document_types",
                schema: "master");

            migrationBuilder.DropTable(
                name: "marital_statuses",
                schema: "master");

            migrationBuilder.DropTable(
                name: "pre_auth_enhancements",
                schema: "insurance");

            migrationBuilder.DropTable(
                name: "salutations",
                schema: "master");

            migrationBuilder.DropTable(
                name: "visit_classes",
                schema: "master");

            migrationBuilder.DropTable(
                name: "visit_sessions",
                schema: "master");

            migrationBuilder.DropTable(
                name: "claim_document_types",
                schema: "master");

            migrationBuilder.DropTable(
                name: "claims",
                schema: "insurance");

            migrationBuilder.DropTable(
                name: "claim_denial_reasons",
                schema: "master");

            migrationBuilder.DropTable(
                name: "pre_auth_requests",
                schema: "insurance");

            migrationBuilder.DropTable(
                name: "patient_insurance_policies",
                schema: "insurance");

            migrationBuilder.DropIndex(
                name: "ix_patients_area_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_city_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_country_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_language_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_marital_status_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_nationality_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_patient_case_type_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_patient_hc_tag_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_religion_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_salutation_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_specially_abled_category_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_state_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patient_identifiers_identity_document_type_id",
                schema: "patient",
                table: "patient_identifiers");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_session_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_visit_class_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "area_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "city_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "country_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "insurance_required",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "is_permanent_same_as_present",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "is_vip",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "language_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "marital_status_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "monthly_income",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "mother_name",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "national_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "nationality_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "patient_case_type_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "patient_hc_tag_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "permanent_address",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "religion_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "remarks",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "salutation_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "sms_opt_in",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "specially_abled_category_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "state_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "whats_app_number",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "identity_document_type_id",
                schema: "patient",
                table: "patient_identifiers");

            migrationBuilder.DropColumn(
                name: "booking_type",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "is_mlc_case",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "session_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "visit_class_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "patient",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nationality",
                schema: "patient",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "patient",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
