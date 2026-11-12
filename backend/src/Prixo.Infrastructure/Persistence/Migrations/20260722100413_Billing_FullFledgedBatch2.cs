using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Billing_FullFledgedBatch2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bill_estimate_templates",
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
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_estimate_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_estimate_templates_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_payout_rules",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    service_category_id = table.Column<long>(type: "bigint", nullable: false),
                    payout_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_doctor_payout_rules", x => x.id);
                    table.CheckConstraint("ck_doctor_payout_rules_value", "value >= 0");
                    table.ForeignKey(
                        name: "fk_doctor_payout_rules_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_doctor_payout_rules_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "master",
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_doctor_payout_rules_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalSchema: "master",
                        principalTable: "service_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_estimate_template_items",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_estimate_template_id = table.Column<long>(type: "bigint", nullable: false),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: true),
                    package_id = table.Column<long>(type: "bigint", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_estimate_template_items", x => x.id);
                    table.CheckConstraint("ck_bill_estimate_template_items_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_bill_estimate_template_items_bill_estimate_templates_bill_e",
                        column: x => x.bill_estimate_template_id,
                        principalSchema: "master",
                        principalTable: "bill_estimate_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_estimate_template_items_packages_package_id",
                        column: x => x.package_id,
                        principalSchema: "master",
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_estimate_template_items_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "master",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimate_template_items_bill_estimate_template_id",
                schema: "master",
                table: "bill_estimate_template_items",
                column: "bill_estimate_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimate_template_items_package_id",
                schema: "master",
                table: "bill_estimate_template_items",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimate_template_items_service_id",
                schema: "master",
                table: "bill_estimate_template_items",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimate_templates_branch_id_code",
                schema: "master",
                table: "bill_estimate_templates",
                columns: new[] { "branch_id", "code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_payout_rules_branch_id_doctor_id_service_category_id",
                schema: "master",
                table: "doctor_payout_rules",
                columns: new[] { "branch_id", "doctor_id", "service_category_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_payout_rules_doctor_id",
                schema: "master",
                table: "doctor_payout_rules",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_payout_rules_service_category_id",
                schema: "master",
                table: "doctor_payout_rules",
                column: "service_category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bill_estimate_template_items",
                schema: "master");

            migrationBuilder.DropTable(
                name: "doctor_payout_rules",
                schema: "master");

            migrationBuilder.DropTable(
                name: "bill_estimate_templates",
                schema: "master");
        }
    }
}
