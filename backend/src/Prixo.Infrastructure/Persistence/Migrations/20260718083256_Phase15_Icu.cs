using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase15_Icu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "icu");

            migrationBuilder.AddColumn<int>(
                name: "central_venous_pressure",
                schema: "ipd",
                table: "ip_vitals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "glasgow_coma_scale",
                schema: "ipd",
                table: "ip_vitals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mean_arterial_pressure",
                schema: "ipd",
                table: "ip_vitals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "urine_output_ml",
                schema: "ipd",
                table: "ip_vitals",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "critical_alert_rules",
                schema: "icu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parameter = table.Column<int>(type: "integer", nullable: false),
                    @operator = table.Column<int>(name: "operator", type: "integer", nullable: false),
                    threshold_value = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("pk_critical_alert_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_critical_alert_rules_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_readings",
                schema: "icu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: true),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parameter_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    raw_payload = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_device_readings", x => x.id);
                    table.ForeignKey(
                        name: "fk_device_readings_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_device_readings_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "icu_severity_assessments",
                schema: "icu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assessed_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    severity_level = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_icu_severity_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_icu_severity_assessments_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_icu_severity_assessments_users_assessed_by_user_id",
                        column: x => x.assessed_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "infusion_chart_entries",
                schema: "icu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    drug_id = table.Column<long>(type: "bigint", nullable: true),
                    drug_name_override = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    stopped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_infusion_chart_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_infusion_chart_entries_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_infusion_chart_entries_drugs_drug_id",
                        column: x => x.drug_id,
                        principalSchema: "master",
                        principalTable: "drugs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_infusion_chart_entries_users_started_by_user_id",
                        column: x => x.started_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ventilator_sessions",
                schema: "icu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    stopped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_ventilator_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_ventilator_sessions_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ventilator_sessions_users_started_by_user_id",
                        column: x => x.started_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "icu_vitals_alerts",
                schema: "icu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_id = table.Column<long>(type: "bigint", nullable: false),
                    ip_vitals_id = table.Column<long>(type: "bigint", nullable: false),
                    critical_alert_rule_id = table.Column<long>(type: "bigint", nullable: false),
                    raised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    acknowledged_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledgement_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_icu_vitals_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_icu_vitals_alerts_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_icu_vitals_alerts_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_icu_vitals_alerts_critical_alert_rules_critical_alert_rule_",
                        column: x => x.critical_alert_rule_id,
                        principalSchema: "icu",
                        principalTable: "critical_alert_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_icu_vitals_alerts_ip_vitals_ip_vitals_id",
                        column: x => x.ip_vitals_id,
                        principalSchema: "ipd",
                        principalTable: "ip_vitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_icu_vitals_alerts_users_acknowledged_by_user_id",
                        column: x => x.acknowledged_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "infusion_rate_entries",
                schema: "icu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    infusion_chart_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    rate_unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("pk_infusion_rate_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_infusion_rate_entries_infusion_chart_entries_infusion_chart",
                        column: x => x.infusion_chart_entry_id,
                        principalSchema: "icu",
                        principalTable: "infusion_chart_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_infusion_rate_entries_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ventilator_observations",
                schema: "icu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    fi_o2percent = table.Column<int>(type: "integer", nullable: false),
                    peep = table.Column<int>(type: "integer", nullable: false),
                    tidal_volume_ml = table.Column<int>(type: "integer", nullable: true),
                    respiratory_rate_set = table.Column<int>(type: "integer", nullable: true),
                    peak_pressure = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    plateau_pressure = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
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
                    table.PrimaryKey("pk_ventilator_observations", x => x.id);
                    table.ForeignKey(
                        name: "fk_ventilator_observations_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ventilator_observations_ventilator_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "icu",
                        principalTable: "ventilator_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_critical_alert_rules_branch_id_parameter_operator",
                schema: "icu",
                table: "critical_alert_rules",
                columns: new[] { "branch_id", "parameter", "operator" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_device_readings_admission_id_recorded_at",
                schema: "icu",
                table: "device_readings",
                columns: new[] { "admission_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_device_readings_branch_id",
                schema: "icu",
                table: "device_readings",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_icu_severity_assessments_admission_id_assessed_at",
                schema: "icu",
                table: "icu_severity_assessments",
                columns: new[] { "admission_id", "assessed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_icu_severity_assessments_assessed_by_user_id",
                schema: "icu",
                table: "icu_severity_assessments",
                column: "assessed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_icu_vitals_alerts_acknowledged_by_user_id",
                schema: "icu",
                table: "icu_vitals_alerts",
                column: "acknowledged_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_icu_vitals_alerts_admission_id_is_acknowledged",
                schema: "icu",
                table: "icu_vitals_alerts",
                columns: new[] { "admission_id", "is_acknowledged" });

            migrationBuilder.CreateIndex(
                name: "ix_icu_vitals_alerts_branch_id",
                schema: "icu",
                table: "icu_vitals_alerts",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_icu_vitals_alerts_critical_alert_rule_id",
                schema: "icu",
                table: "icu_vitals_alerts",
                column: "critical_alert_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_icu_vitals_alerts_ip_vitals_id",
                schema: "icu",
                table: "icu_vitals_alerts",
                column: "ip_vitals_id");

            migrationBuilder.CreateIndex(
                name: "ix_infusion_chart_entries_admission_id_started_at",
                schema: "icu",
                table: "infusion_chart_entries",
                columns: new[] { "admission_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_infusion_chart_entries_drug_id",
                schema: "icu",
                table: "infusion_chart_entries",
                column: "drug_id");

            migrationBuilder.CreateIndex(
                name: "ix_infusion_chart_entries_started_by_user_id",
                schema: "icu",
                table: "infusion_chart_entries",
                column: "started_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_infusion_rate_entries_infusion_chart_entry_id_recorded_at",
                schema: "icu",
                table: "infusion_rate_entries",
                columns: new[] { "infusion_chart_entry_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_infusion_rate_entries_recorded_by_user_id",
                schema: "icu",
                table: "infusion_rate_entries",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventilator_observations_recorded_by_user_id",
                schema: "icu",
                table: "ventilator_observations",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventilator_observations_session_id_recorded_at",
                schema: "icu",
                table: "ventilator_observations",
                columns: new[] { "session_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ventilator_sessions_admission_id_started_at",
                schema: "icu",
                table: "ventilator_sessions",
                columns: new[] { "admission_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ventilator_sessions_started_by_user_id",
                schema: "icu",
                table: "ventilator_sessions",
                column: "started_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_readings",
                schema: "icu");

            migrationBuilder.DropTable(
                name: "icu_severity_assessments",
                schema: "icu");

            migrationBuilder.DropTable(
                name: "icu_vitals_alerts",
                schema: "icu");

            migrationBuilder.DropTable(
                name: "infusion_rate_entries",
                schema: "icu");

            migrationBuilder.DropTable(
                name: "ventilator_observations",
                schema: "icu");

            migrationBuilder.DropTable(
                name: "critical_alert_rules",
                schema: "icu");

            migrationBuilder.DropTable(
                name: "infusion_chart_entries",
                schema: "icu");

            migrationBuilder.DropTable(
                name: "ventilator_sessions",
                schema: "icu");

            migrationBuilder.DropColumn(
                name: "central_venous_pressure",
                schema: "ipd",
                table: "ip_vitals");

            migrationBuilder.DropColumn(
                name: "glasgow_coma_scale",
                schema: "ipd",
                table: "ip_vitals");

            migrationBuilder.DropColumn(
                name: "mean_arterial_pressure",
                schema: "ipd",
                table: "ip_vitals");

            migrationBuilder.DropColumn(
                name: "urine_output_ml",
                schema: "ipd",
                table: "ip_vitals");
        }
    }
}
