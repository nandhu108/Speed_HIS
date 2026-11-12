using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_Appointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "appointment");

            migrationBuilder.CreateTable(
                name: "appointments",
                schema: "appointment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    appointment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    token_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    booking_type = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    no_show_marked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rescheduled_from_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_appointments", x => x.id);
                    table.ForeignKey(
                        name: "fk_appointments_appointments_rescheduled_from_id",
                        column: x => x.rescheduled_from_id,
                        principalSchema: "appointment",
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointments_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointments_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointments_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_schedules",
                schema: "appointment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    slot_minutes = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_doctor_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_doctor_schedules_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_doctor_schedules_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_queue",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    recipient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    related_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    related_entity_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_queue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schedule_overrides",
                schema: "appointment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    slot_minutes = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("pk_schedule_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_schedule_overrides_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_schedule_overrides_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_branch_id_appointment_date",
                schema: "appointment",
                table: "appointments",
                columns: new[] { "branch_id", "appointment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_doctor_id_appointment_date_start_time",
                schema: "appointment",
                table: "appointments",
                columns: new[] { "doctor_id", "appointment_date", "start_time" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_patient_id",
                schema: "appointment",
                table: "appointments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_rescheduled_from_id",
                schema: "appointment",
                table: "appointments",
                column: "rescheduled_from_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedules_branch_id",
                schema: "appointment",
                table: "doctor_schedules",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedules_doctor_id_branch_id_day_of_week",
                schema: "appointment",
                table: "doctor_schedules",
                columns: new[] { "doctor_id", "branch_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_queue_related_entity_type_related_entity_id",
                schema: "admin",
                table: "notification_queue",
                columns: new[] { "related_entity_type", "related_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_queue_status",
                schema: "admin",
                table: "notification_queue",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_overrides_branch_id",
                schema: "appointment",
                table: "schedule_overrides",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_overrides_doctor_id_date",
                schema: "appointment",
                table: "schedule_overrides",
                columns: new[] { "doctor_id", "date" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointments",
                schema: "appointment");

            migrationBuilder.DropTable(
                name: "doctor_schedules",
                schema: "appointment");

            migrationBuilder.DropTable(
                name: "notification_queue",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "schedule_overrides",
                schema: "appointment");
        }
    }
}
