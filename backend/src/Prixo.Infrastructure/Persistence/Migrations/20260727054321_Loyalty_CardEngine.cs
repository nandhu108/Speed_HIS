using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Loyalty_CardEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "loyalty");

            migrationBuilder.CreateTable(
                name: "loyalty_card_categories",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("pk_loyalty_card_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "member_relation_tags",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("pk_member_relation_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_card_types",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    member_limit = table.Column<int>(type: "integer", nullable: false),
                    loyalty_card_category_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_card_types", x => x.id);
                    table.CheckConstraint("ck_loyalty_card_types_member_limit", "member_limit >= 0");
                    table.ForeignKey(
                        name: "fk_loyalty_card_types_loyalty_card_categories_loyalty_card_cat",
                        column: x => x.loyalty_card_category_id,
                        principalSchema: "master",
                        principalTable: "loyalty_card_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_card_prices",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loyalty_card_type_id = table.Column<long>(type: "bigint", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("pk_loyalty_card_prices", x => x.id);
                    table.CheckConstraint("ck_loyalty_card_prices_effective_range", "effective_to IS NULL OR effective_to >= effective_from");
                    table.ForeignKey(
                        name: "fk_loyalty_card_prices_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loyalty_card_prices_loyalty_card_types_loyalty_card_type_id",
                        column: x => x.loyalty_card_type_id,
                        principalSchema: "master",
                        principalTable: "loyalty_card_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_cards",
                schema: "loyalty",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    loyalty_card_type_id = table.Column<long>(type: "bigint", nullable: false),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: false),
                    duration_months = table.Column<int>(type: "integer", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    deactivate_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duplicate_issue_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_loyalty_cards", x => x.id);
                    table.CheckConstraint("ck_loyalty_cards_duration", "duration_months > 0");
                    table.ForeignKey(
                        name: "fk_loyalty_cards_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loyalty_cards_loyalty_card_types_loyalty_card_type_id",
                        column: x => x.loyalty_card_type_id,
                        principalSchema: "master",
                        principalTable: "loyalty_card_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loyalty_cards_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_card_price_items",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loyalty_card_price_id = table.Column<long>(type: "bigint", nullable: false),
                    duration_months = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_card_price_items", x => x.id);
                    table.CheckConstraint("ck_loyalty_card_price_items_amount", "amount >= 0");
                    table.CheckConstraint("ck_loyalty_card_price_items_duration", "duration_months > 0");
                    table.ForeignKey(
                        name: "fk_loyalty_card_price_items_loyalty_card_prices_loyalty_card_p",
                        column: x => x.loyalty_card_price_id,
                        principalSchema: "master",
                        principalTable: "loyalty_card_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_card_members",
                schema: "loyalty",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loyalty_card_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    member_relation_tag_id = table.Column<long>(type: "bigint", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_card_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_loyalty_card_members_loyalty_cards_loyalty_card_id",
                        column: x => x.loyalty_card_id,
                        principalSchema: "loyalty",
                        principalTable: "loyalty_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loyalty_card_members_member_relation_tags_member_relation_t",
                        column: x => x.member_relation_tag_id,
                        principalSchema: "master",
                        principalTable: "member_relation_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loyalty_card_members_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_categories_code",
                schema: "master",
                table: "loyalty_card_categories",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_members_loyalty_card_id_patient_id",
                schema: "loyalty",
                table: "loyalty_card_members",
                columns: new[] { "loyalty_card_id", "patient_id" },
                unique: true,
                filter: "is_deleted = false AND removed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_members_member_relation_tag_id",
                schema: "loyalty",
                table: "loyalty_card_members",
                column: "member_relation_tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_members_patient_id",
                schema: "loyalty",
                table: "loyalty_card_members",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_price_items_loyalty_card_price_id_duration_mon",
                schema: "master",
                table: "loyalty_card_price_items",
                columns: new[] { "loyalty_card_price_id", "duration_months" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_prices_branch_id_loyalty_card_type_id_effectiv",
                schema: "master",
                table: "loyalty_card_prices",
                columns: new[] { "branch_id", "loyalty_card_type_id", "effective_from" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_prices_loyalty_card_type_id",
                schema: "master",
                table: "loyalty_card_prices",
                column: "loyalty_card_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_types_code",
                schema: "master",
                table: "loyalty_card_types",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_card_types_loyalty_card_category_id",
                schema: "master",
                table: "loyalty_card_types",
                column: "loyalty_card_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_cards_branch_id_card_number",
                schema: "loyalty",
                table: "loyalty_cards",
                columns: new[] { "branch_id", "card_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_cards_loyalty_card_type_id",
                schema: "loyalty",
                table: "loyalty_cards",
                column: "loyalty_card_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_cards_patient_id",
                schema: "loyalty",
                table: "loyalty_cards",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_relation_tags_code",
                schema: "master",
                table: "member_relation_tags",
                column: "code",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loyalty_card_members",
                schema: "loyalty");

            migrationBuilder.DropTable(
                name: "loyalty_card_price_items",
                schema: "master");

            migrationBuilder.DropTable(
                name: "loyalty_cards",
                schema: "loyalty");

            migrationBuilder.DropTable(
                name: "member_relation_tags",
                schema: "master");

            migrationBuilder.DropTable(
                name: "loyalty_card_prices",
                schema: "master");

            migrationBuilder.DropTable(
                name: "loyalty_card_types",
                schema: "master");

            migrationBuilder.DropTable(
                name: "loyalty_card_categories",
                schema: "master");
        }
    }
}
