using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase23_Reports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "report_definitions",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    permission_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    view_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    link_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    columns_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_subscriptions",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    report_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    recipient_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_subscriptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_definitions_code",
                schema: "reporting",
                table: "report_definitions",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_report_subscriptions_report_code",
                schema: "reporting",
                table: "report_subscriptions",
                column: "report_code");

            migrationBuilder.CreateIndex(
                name: "ix_report_subscriptions_user_id",
                schema: "reporting",
                table: "report_subscriptions",
                column: "user_id");

            // Materialized views (plan §17: "Reporting reads from SQL views / materialized
            // views — never raw OLTP joins scattered in code"). Refreshed by ReportRefreshJob.
            // Doctor attribution mirrors Finance's existing GetRevenueReportAsync (OP visits
            // only, via Bill.OpVisitId) rather than adding IP-consultant revenue attribution,
            // which that report doesn't do either — see docs/modules/reports.md.
            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW reporting.mv_revenue_daily AS
                SELECT
                    b.branch_id AS branch_id,
                    b.bill_date AS revenue_date,
                    s.department_id AS department_id,
                    d.name AS department_name,
                    ov.doctor_id AS doctor_id,
                    u.full_name AS doctor_name,
                    SUM(bi.total_amount) AS amount
                FROM billing.bill_items bi
                JOIN billing.bills b ON b.id = bi.bill_id AND b.is_deleted = false
                LEFT JOIN master.services s ON s.id = bi.service_id AND s.is_deleted = false
                LEFT JOIN master.departments d ON d.id = s.department_id AND d.is_deleted = false
                LEFT JOIN opd.op_visits ov ON ov.id = b.op_visit_id AND ov.is_deleted = false
                LEFT JOIN master.doctors doc ON doc.id = ov.doctor_id AND doc.is_deleted = false
                LEFT JOIN admin.users u ON u.id = doc.user_id AND u.is_deleted = false
                WHERE bi.is_deleted = false AND b.status <> 4
                GROUP BY b.branch_id, b.bill_date, s.department_id, d.name, ov.doctor_id, u.full_name
                """);
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_mv_revenue_daily ON reporting.mv_revenue_daily (branch_id, revenue_date, department_id, doctor_id)");

            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW reporting.mv_daily_collection AS
                SELECT
                    b.branch_id AS branch_id,
                    (bp.paid_at AT TIME ZONE 'UTC')::date AS collection_date,
                    bp.payment_mode_id AS payment_mode_id,
                    pm.name AS payment_mode_name,
                    SUM(bp.amount) AS amount,
                    COUNT(*) AS txn_count
                FROM billing.bill_payments bp
                JOIN billing.bills b ON b.id = bp.bill_id AND b.is_deleted = false
                JOIN billing.payment_modes pm ON pm.id = bp.payment_mode_id AND pm.is_deleted = false
                WHERE bp.is_deleted = false
                GROUP BY b.branch_id, (bp.paid_at AT TIME ZONE 'UTC')::date, bp.payment_mode_id, pm.name
                """);
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_mv_daily_collection ON reporting.mv_daily_collection (branch_id, collection_date, payment_mode_id)");

            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW reporting.mv_op_census AS
                SELECT
                    ov.branch_id AS branch_id,
                    ov.visit_date AS visit_date,
                    ov.doctor_id AS doctor_id,
                    u.full_name AS doctor_name,
                    ov.visit_type AS visit_type,
                    COUNT(*) AS visit_count
                FROM opd.op_visits ov
                JOIN master.doctors doc ON doc.id = ov.doctor_id AND doc.is_deleted = false
                JOIN admin.users u ON u.id = doc.user_id AND u.is_deleted = false
                WHERE ov.is_deleted = false
                GROUP BY ov.branch_id, ov.visit_date, ov.doctor_id, u.full_name, ov.visit_type
                """);
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_mv_op_census ON reporting.mv_op_census (branch_id, visit_date, doctor_id, visit_type)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS reporting.mv_revenue_daily");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS reporting.mv_daily_collection");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS reporting.mv_op_census");

            migrationBuilder.DropTable(
                name: "report_definitions",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_subscriptions",
                schema: "reporting");
        }
    }
}
