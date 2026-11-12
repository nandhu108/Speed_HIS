using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Patients_Block_Ipd_AdmissionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "blocked_at",
                schema: "patient",
                table: "patients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "blocked_by_user_id",
                schema: "patient",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "blocked_reason",
                schema: "patient",
                table: "patients",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_blocked",
                schema: "patient",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "admission_requests",
                schema: "ipd",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_consultant_id = table.Column<long>(type: "bigint", nullable: true),
                    ward_id = table.Column<long>(type: "bigint", nullable: true),
                    expected_admission_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    requested_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_admission_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_admission_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_admission_requests_admissions_approved_admission_id",
                        column: x => x.approved_admission_id,
                        principalSchema: "ipd",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_requests_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_requests_doctors_requested_consultant_id",
                        column: x => x.requested_consultant_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_requests_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_requests_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_requests_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalSchema: "admin",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_requests_wards_ward_id",
                        column: x => x.ward_id,
                        principalSchema: "master",
                        principalTable: "wards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patients_blocked_by_user_id",
                schema: "patient",
                table: "patients",
                column: "blocked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_requests_approved_admission_id",
                schema: "ipd",
                table: "admission_requests",
                column: "approved_admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_requests_branch_id_status",
                schema: "ipd",
                table: "admission_requests",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_requests_decided_by_user_id",
                schema: "ipd",
                table: "admission_requests",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_requests_patient_id",
                schema: "ipd",
                table: "admission_requests",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_requests_requested_by_user_id",
                schema: "ipd",
                table: "admission_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_requests_requested_consultant_id",
                schema: "ipd",
                table: "admission_requests",
                column: "requested_consultant_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_requests_ward_id",
                schema: "ipd",
                table: "admission_requests",
                column: "ward_id");

            migrationBuilder.AddForeignKey(
                name: "fk_patients_users_blocked_by_user_id",
                schema: "patient",
                table: "patients",
                column: "blocked_by_user_id",
                principalSchema: "admin",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_patients_users_blocked_by_user_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropTable(
                name: "admission_requests",
                schema: "ipd");

            migrationBuilder.DropIndex(
                name: "ix_patients_blocked_by_user_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "blocked_at",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "blocked_by_user_id",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "blocked_reason",
                schema: "patient",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "is_blocked",
                schema: "patient",
                table: "patients");
        }
    }
}
