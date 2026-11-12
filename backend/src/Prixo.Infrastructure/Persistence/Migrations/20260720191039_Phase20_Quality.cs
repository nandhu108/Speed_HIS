using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase20_Quality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "quality");

            migrationBuilder.AddColumn<bool>(
                name: "is_sentinel_event",
                schema: "nursing",
                table: "incidents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "root_cause_analysis",
                schema: "nursing",
                table: "incidents",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_module",
                schema: "nursing",
                table: "incidents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_templates",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capas",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<long>(type: "bigint", nullable: false),
                    action_plan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    owner_user_id = table.Column<long>(type: "bigint", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verification_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_capas", x => x.id);
                    table.ForeignKey(
                        name: "fk_capas_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_capas_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_capas_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "committees",
                schema: "quality",
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
                    table.PrimaryKey("pk_committees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "indicators",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    numerator_definition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    denominator_definition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    target_value = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    auto_compute_source = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_indicators", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nabh_standards",
                schema: "quality",
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
                    table.PrimaryKey("pk_nabh_standards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "risks",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    likelihood = table.Column<int>(type: "integer", nullable: false),
                    impact = table.Column<int>(type: "integer", nullable: false),
                    mitigation_plan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    owner_user_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    identified_date = table.Column<DateOnly>(type: "date", nullable: false),
                    review_date = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("pk_risks", x => x.id);
                    table.ForeignKey(
                        name: "fk_risks_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_risks_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_template_items",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    max_score = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_template_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_template_items_audit_templates_template_id",
                        column: x => x.template_id,
                        principalSchema: "quality",
                        principalTable: "audit_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audits",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<long>(type: "bigint", nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    conducted_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    conducted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_audits", x => x.id);
                    table.ForeignKey(
                        name: "fk_audits_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audits_quality_audit_templates_template_id",
                        column: x => x.template_id,
                        principalSchema: "quality",
                        principalTable: "audit_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audits_users_conducted_by_user_id",
                        column: x => x.conducted_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "committee_meetings",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    committee_id = table.Column<long>(type: "bigint", nullable: false),
                    meeting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    minutes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    attendees_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_committee_meetings", x => x.id);
                    table.ForeignKey(
                        name: "fk_committee_meetings_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_committee_meetings_committees_committee_id",
                        column: x => x.committee_id,
                        principalSchema: "quality",
                        principalTable: "committees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "indicator_entries",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    indicator_id = table.Column<long>(type: "bigint", nullable: false),
                    period_month = table.Column<DateOnly>(type: "date", nullable: false),
                    numerator = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    denominator = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    is_auto_computed = table.Column<bool>(type: "boolean", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    entered_by_user_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_indicator_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_indicator_entries_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_indicator_entries_indicators_indicator_id",
                        column: x => x.indicator_id,
                        principalSchema: "quality",
                        principalTable: "indicators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_indicator_entries_users_entered_by_user_id",
                        column: x => x.entered_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidence",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    standard_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    uploaded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_evidence", x => x.id);
                    table.ForeignKey(
                        name: "fk_evidence_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evidence_quality_nabh_standards_standard_id",
                        column: x => x.standard_id,
                        principalSchema: "quality",
                        principalTable: "nabh_standards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evidence_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "non_conformances",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    standard_id = table.Column<long>(type: "bigint", nullable: true),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    raised_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_non_conformances", x => x.id);
                    table.ForeignKey(
                        name: "fk_non_conformances_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_non_conformances_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "master",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_non_conformances_nabh_standards_standard_id",
                        column: x => x.standard_id,
                        principalSchema: "quality",
                        principalTable: "nabh_standards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_non_conformances_users_closed_by_user_id",
                        column: x => x.closed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_non_conformances_users_raised_by_user_id",
                        column: x => x.raised_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_responses",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    audit_id = table.Column<long>(type: "bigint", nullable: false),
                    template_item_id = table.Column<long>(type: "bigint", nullable: false),
                    score = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
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
                    table.PrimaryKey("pk_audit_responses", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_responses_audits_audit_id",
                        column: x => x.audit_id,
                        principalSchema: "quality",
                        principalTable: "audits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_responses_quality_audit_template_items_template_item_",
                        column: x => x.template_item_id,
                        principalSchema: "quality",
                        principalTable: "audit_template_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "committee_action_items",
                schema: "quality",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    meeting_id = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    owner_user_id = table.Column<long>(type: "bigint", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_committee_action_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_committee_action_items_quality_committee_meetings_meeting_id",
                        column: x => x.meeting_id,
                        principalSchema: "quality",
                        principalTable: "committee_meetings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_committee_action_items_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_responses_audit_id_template_item_id",
                schema: "quality",
                table: "audit_responses",
                columns: new[] { "audit_id", "template_item_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_audit_responses_template_item_id",
                schema: "quality",
                table: "audit_responses",
                column: "template_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_template_items_template_id_sort_order",
                schema: "quality",
                table: "audit_template_items",
                columns: new[] { "template_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_templates_code",
                schema: "quality",
                table: "audit_templates",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_audits_branch_id_status_scheduled_date",
                schema: "quality",
                table: "audits",
                columns: new[] { "branch_id", "status", "scheduled_date" });

            migrationBuilder.CreateIndex(
                name: "ix_audits_conducted_by_user_id",
                schema: "quality",
                table: "audits",
                column: "conducted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audits_template_id",
                schema: "quality",
                table: "audits",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_capas_branch_id_status_due_date",
                schema: "quality",
                table: "capas",
                columns: new[] { "branch_id", "status", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_capas_owner_user_id",
                schema: "quality",
                table: "capas",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_capas_source_type_source_id",
                schema: "quality",
                table: "capas",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_capas_verified_by_user_id",
                schema: "quality",
                table: "capas",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_committee_action_items_meeting_id",
                schema: "quality",
                table: "committee_action_items",
                column: "meeting_id");

            migrationBuilder.CreateIndex(
                name: "ix_committee_action_items_owner_user_id",
                schema: "quality",
                table: "committee_action_items",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_committee_meetings_branch_id",
                schema: "quality",
                table: "committee_meetings",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_committee_meetings_committee_id_meeting_date",
                schema: "quality",
                table: "committee_meetings",
                columns: new[] { "committee_id", "meeting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_committees_code",
                schema: "quality",
                table: "committees",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_branch_id",
                schema: "quality",
                table: "evidence",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_standard_id",
                schema: "quality",
                table: "evidence",
                column: "standard_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_uploaded_by_user_id",
                schema: "quality",
                table: "evidence",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_indicator_entries_branch_id",
                schema: "quality",
                table: "indicator_entries",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_indicator_entries_entered_by_user_id",
                schema: "quality",
                table: "indicator_entries",
                column: "entered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_indicator_entries_indicator_id_branch_id_period_month",
                schema: "quality",
                table: "indicator_entries",
                columns: new[] { "indicator_id", "branch_id", "period_month" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_indicators_code",
                schema: "quality",
                table: "indicators",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_nabh_standards_code",
                schema: "quality",
                table: "nabh_standards",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_non_conformances_branch_id_status",
                schema: "quality",
                table: "non_conformances",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_non_conformances_closed_by_user_id",
                schema: "quality",
                table: "non_conformances",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_non_conformances_department_id",
                schema: "quality",
                table: "non_conformances",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_non_conformances_raised_by_user_id",
                schema: "quality",
                table: "non_conformances",
                column: "raised_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_non_conformances_standard_id",
                schema: "quality",
                table: "non_conformances",
                column: "standard_id");

            migrationBuilder.CreateIndex(
                name: "ix_risks_branch_id_status",
                schema: "quality",
                table: "risks",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_risks_owner_user_id",
                schema: "quality",
                table: "risks",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_responses",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "capas",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "committee_action_items",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "evidence",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "indicator_entries",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "non_conformances",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "risks",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "audits",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "audit_template_items",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "committee_meetings",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "indicators",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "nabh_standards",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "audit_templates",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "committees",
                schema: "quality");

            migrationBuilder.DropColumn(
                name: "is_sentinel_event",
                schema: "nursing",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "root_cause_analysis",
                schema: "nursing",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "source_module",
                schema: "nursing",
                table: "incidents");
        }
    }
}
