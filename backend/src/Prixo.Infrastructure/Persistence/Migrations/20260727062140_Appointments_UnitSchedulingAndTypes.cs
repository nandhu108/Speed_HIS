using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Appointments_UnitSchedulingAndTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "appointment_type_id",
                schema: "appointment",
                table: "appointments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "appointment_types",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_slot_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointment_types", x => x.id);
                    table.CheckConstraint("ck_appointment_types_slot_minutes", "default_slot_minutes > 0");
                });

            migrationBuilder.CreateTable(
                name: "unit_doctors",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unit_doctors", x => x.id);
                    table.ForeignKey(
                        name: "fk_unit_doctors_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_doctors_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "master",
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_schedules",
                schema: "appointment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
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
                    table.PrimaryKey("pk_unit_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_unit_schedules_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unit_schedules_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "master",
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_appointment_type_id",
                schema: "appointment",
                table: "appointments",
                column: "appointment_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_types_code",
                schema: "master",
                table: "appointment_types",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_unit_doctors_doctor_id",
                schema: "master",
                table: "unit_doctors",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_doctors_unit_id_doctor_id",
                schema: "master",
                table: "unit_doctors",
                columns: new[] { "unit_id", "doctor_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_unit_schedules_branch_id",
                schema: "appointment",
                table: "unit_schedules",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_unit_schedules_unit_id_branch_id_day_of_week",
                schema: "appointment",
                table: "unit_schedules",
                columns: new[] { "unit_id", "branch_id", "day_of_week" });

            migrationBuilder.AddForeignKey(
                name: "fk_appointments_appointment_types_appointment_type_id",
                schema: "appointment",
                table: "appointments",
                column: "appointment_type_id",
                principalSchema: "master",
                principalTable: "appointment_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_appointments_appointment_types_appointment_type_id",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "appointment_types",
                schema: "master");

            migrationBuilder.DropTable(
                name: "unit_doctors",
                schema: "master");

            migrationBuilder.DropTable(
                name: "unit_schedules",
                schema: "appointment");

            migrationBuilder.DropIndex(
                name: "ix_appointments_appointment_type_id",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "appointment_type_id",
                schema: "appointment",
                table: "appointments");
        }
    }
}
