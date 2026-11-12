using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Appointments_TokenManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "emergency_visit_id",
                schema: "appointment",
                table: "appointments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "skip_count",
                schema: "appointment",
                table: "appointments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "skipped_at",
                schema: "appointment",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "transferred_at",
                schema: "appointment",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "transferred_from_doctor_id",
                schema: "appointment",
                table: "appointments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointments_emergency_visit_id",
                schema: "appointment",
                table: "appointments",
                column: "emergency_visit_id",
                unique: true,
                filter: "is_deleted = false AND emergency_visit_id IS NOT NULL AND status <> 4");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_transferred_from_doctor_id",
                schema: "appointment",
                table: "appointments",
                column: "transferred_from_doctor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_appointments_doctors_transferred_from_doctor_id",
                schema: "appointment",
                table: "appointments",
                column: "transferred_from_doctor_id",
                principalSchema: "master",
                principalTable: "doctors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_appointments_emergency_visits_emergency_visit_id",
                schema: "appointment",
                table: "appointments",
                column: "emergency_visit_id",
                principalSchema: "er",
                principalTable: "emergency_visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_appointments_doctors_transferred_from_doctor_id",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "fk_appointments_emergency_visits_emergency_visit_id",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "ix_appointments_emergency_visit_id",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "ix_appointments_transferred_from_doctor_id",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "emergency_visit_id",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "skip_count",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "skipped_at",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "transferred_at",
                schema: "appointment",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "transferred_from_doctor_id",
                schema: "appointment",
                table: "appointments");
        }
    }
}
