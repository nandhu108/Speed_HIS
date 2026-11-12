using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260819_Patients_AbdmAbha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abdm_verification_sessions",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    abdm_txn_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    masked_target = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_consumed = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abdm_verification_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_abdm_verification_sessions_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "abha_profiles",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    abha_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    abha_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    verification_method = table.Column<int>(type: "integer", nullable: false),
                    kyc_verified = table.Column<bool>(type: "boolean", nullable: false),
                    linked_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    linked_gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    linked_year_of_birth = table.Column<int>(type: "integer", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abha_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_abha_profiles_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_abdm_verification_sessions_patient_id",
                schema: "patient",
                table: "abdm_verification_sessions",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_abha_profiles_abha_number",
                schema: "patient",
                table: "abha_profiles",
                column: "abha_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_abha_profiles_patient_id",
                schema: "patient",
                table: "abha_profiles",
                column: "patient_id",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abdm_verification_sessions",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "abha_profiles",
                schema: "patient");
        }
    }
}
