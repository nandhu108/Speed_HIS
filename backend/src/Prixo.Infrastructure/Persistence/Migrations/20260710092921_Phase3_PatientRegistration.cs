using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_PatientRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "patient");

            migrationBuilder.CreateTable(
                name: "consent_templates",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body_text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uhid = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    estimated_age = table.Column<int>(type: "integer", nullable: true),
                    blood_group = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    alternate_mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pincode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occupation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    patient_category_id = table.Column<long>(type: "bigint", nullable: true),
                    referrer_id = table.Column<long>(type: "bigint", nullable: true),
                    registered_branch_id = table.Column<long>(type: "bigint", nullable: false),
                    photo_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_merged = table.Column<bool>(type: "boolean", nullable: false),
                    merged_into_patient_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patients", x => x.id);
                    table.ForeignKey(
                        name: "fk_patients_branches_registered_branch_id",
                        column: x => x.registered_branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patients_patient_categories_patient_category_id",
                        column: x => x.patient_category_id,
                        principalSchema: "master",
                        principalTable: "patient_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patients_patients_merged_into_patient_id",
                        column: x => x.merged_into_patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patients_referrers_referrer_id",
                        column: x => x.referrer_id,
                        principalSchema: "master",
                        principalTable: "referrers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sequence_counters",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sequence_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_id = table.Column<long>(type: "bigint", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sequence_counters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patient_consents",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    consent_template_id = table.Column<long>(type: "bigint", nullable: false),
                    signed_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    relationship_to_patient = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    document_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_consents_consent_templates_consent_template_id",
                        column: x => x.consent_template_id,
                        principalSchema: "master",
                        principalTable: "consent_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_consents_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_contacts",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    contact_type = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    relationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_contacts_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_documents",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_patient_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_documents_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_identifiers",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_identifiers", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_identifiers_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_merges",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    survivor_patient_id = table.Column<long>(type: "bigint", nullable: false),
                    merged_patient_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_merges", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_merges_patients_merged_patient_id",
                        column: x => x.merged_patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_merges_patients_survivor_patient_id",
                        column: x => x.survivor_patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_consent_templates_code",
                schema: "master",
                table: "consent_templates",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_patient_consents_consent_template_id",
                schema: "patient",
                table: "patient_consents",
                column: "consent_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_consents_patient_id",
                schema: "patient",
                table: "patient_consents",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_contacts_patient_id",
                schema: "patient",
                table: "patient_contacts",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_documents_patient_id",
                schema: "patient",
                table: "patient_documents",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_identifiers_patient_id",
                schema: "patient",
                table: "patient_identifiers",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_merges_merged_patient_id",
                schema: "patient",
                table: "patient_merges",
                column: "merged_patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_merges_survivor_patient_id",
                schema: "patient",
                table: "patient_merges",
                column: "survivor_patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_date_of_birth",
                schema: "patient",
                table: "patients",
                column: "date_of_birth");

            migrationBuilder.CreateIndex(
                name: "ix_patients_merged_into_patient_id",
                schema: "patient",
                table: "patients",
                column: "merged_into_patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_mobile",
                schema: "patient",
                table: "patients",
                column: "mobile");

            migrationBuilder.CreateIndex(
                name: "ix_patients_patient_category_id",
                schema: "patient",
                table: "patients",
                column: "patient_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_referrer_id",
                schema: "patient",
                table: "patients",
                column: "referrer_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_registered_branch_id",
                schema: "patient",
                table: "patients",
                column: "registered_branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_uhid",
                schema: "patient",
                table: "patients",
                column: "uhid",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_sequence_counters_sequence_key_scope_id_year",
                schema: "admin",
                table: "sequence_counters",
                columns: new[] { "sequence_key", "scope_id", "year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_consents",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "patient_contacts",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "patient_documents",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "patient_identifiers",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "patient_merges",
                schema: "patient");

            migrationBuilder.DropTable(
                name: "sequence_counters",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "consent_templates",
                schema: "master");

            migrationBuilder.DropTable(
                name: "patients",
                schema: "patient");
        }
    }
}
