using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20261112_Masters_DepartmentTypeAndVisitDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "department_id",
                schema: "opd",
                table: "op_visits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                schema: "master",
                table: "departments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<long>(
                name: "department_id",
                schema: "ipd",
                table: "admissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_department_id",
                schema: "opd",
                table: "op_visits",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_department_id",
                schema: "ipd",
                table: "admissions",
                column: "department_id");

            // Backfill (rows that pre-date the columns). Departments default to Clinical (1); the
            // Speed Multi workbook's "Diagnostic" units (Laboratory, Radiology) were seeded with
            // a "Diagnostic department" description and become NonClinical (2). Existing OP visits
            // and admissions take their doctor's first mapped department where there is one;
            // others stay NULL (the field is only enforced on new registrations).
            migrationBuilder.Sql("""
                UPDATE master.departments SET type = 2 WHERE description = 'Diagnostic department';

                UPDATE opd.op_visits v
                SET department_id = (
                    SELECT dd.department_id FROM master.doctor_departments dd
                    WHERE dd.doctor_id = v.doctor_id AND dd.is_deleted = false
                    ORDER BY dd.id LIMIT 1)
                WHERE v.department_id IS NULL AND v.doctor_id IS NOT NULL;

                UPDATE ipd.admissions a
                SET department_id = (
                    SELECT dd.department_id FROM master.doctor_departments dd
                    WHERE dd.doctor_id = a.primary_consultant_id AND dd.is_deleted = false
                    ORDER BY dd.id LIMIT 1)
                WHERE a.department_id IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_admissions_departments_department_id",
                schema: "ipd",
                table: "admissions",
                column: "department_id",
                principalSchema: "master",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_op_visits_departments_department_id",
                schema: "opd",
                table: "op_visits",
                column: "department_id",
                principalSchema: "master",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_admissions_departments_department_id",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.DropForeignKey(
                name: "fk_op_visits_departments_department_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_department_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropIndex(
                name: "ix_admissions_department_id",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "type",
                schema: "master",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "ipd",
                table: "admissions");
        }
    }
}
