using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Opd_MhcRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "doctor_id",
                schema: "opd",
                table: "op_visits",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "category",
                schema: "opd",
                table: "op_visits",
                type: "integer",
                nullable: false,
                defaultValue: 1); // VisitCategory.Opd — every pre-existing visit is an OPD visit.

            migrationBuilder.AddColumn<long>(
                name: "mhc_doctor_id",
                schema: "opd",
                table: "op_visits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "mhc_package_id",
                schema: "opd",
                table: "op_visits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "follow_up_window_days",
                schema: "master",
                table: "mhc_doctors",
                type: "integer",
                nullable: false,
                defaultValue: 14); // Matches MhcDoctor.FollowUpWindowDays' C# default, same as Doctor.FollowUpWindowDays.

            migrationBuilder.CreateTable(
                name: "mhc_packages",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mhc_packages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_mhc_doctor_id",
                schema: "opd",
                table: "op_visits",
                column: "mhc_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_mhc_package_id",
                schema: "opd",
                table: "op_visits",
                column: "mhc_package_id");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_patient_id_mhc_doctor_id_visit_date",
                schema: "opd",
                table: "op_visits",
                columns: new[] { "patient_id", "mhc_doctor_id", "visit_date" });

            migrationBuilder.CreateIndex(
                name: "ix_mhc_packages_code",
                schema: "master",
                table: "mhc_packages",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_op_visits_mhc_doctors_mhc_doctor_id",
                schema: "opd",
                table: "op_visits",
                column: "mhc_doctor_id",
                principalSchema: "master",
                principalTable: "mhc_doctors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_op_visits_mhc_packages_mhc_package_id",
                schema: "opd",
                table: "op_visits",
                column: "mhc_package_id",
                principalSchema: "master",
                principalTable: "mhc_packages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_op_visits_mhc_doctors_mhc_doctor_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropForeignKey(
                name: "fk_op_visits_mhc_packages_mhc_package_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropTable(
                name: "mhc_packages",
                schema: "master");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_mhc_doctor_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_mhc_package_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_patient_id_mhc_doctor_id_visit_date",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "category",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "mhc_doctor_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "mhc_package_id",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropColumn(
                name: "follow_up_window_days",
                schema: "master",
                table: "mhc_doctors");

            migrationBuilder.AlterColumn<long>(
                name: "doctor_id",
                schema: "opd",
                table: "op_visits",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
