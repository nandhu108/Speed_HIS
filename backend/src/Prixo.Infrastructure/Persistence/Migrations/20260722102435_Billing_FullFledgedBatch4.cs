using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Billing_FullFledgedBatch4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bill_estimates",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    bill_estimate_template_id = table.Column<long>(type: "bigint", nullable: true),
                    estimate_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estimate_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_bill_estimates", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_estimates_bill_estimate_templates_bill_estimate_templa",
                        column: x => x.bill_estimate_template_id,
                        principalSchema: "master",
                        principalTable: "bill_estimate_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_estimates_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_estimates_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "patient",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_estimate_items",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_estimate_id = table.Column<long>(type: "bigint", nullable: false),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: true),
                    package_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_estimate_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_estimate_items_bill_estimates_bill_estimate_id",
                        column: x => x.bill_estimate_id,
                        principalSchema: "billing",
                        principalTable: "bill_estimates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_estimate_items_packages_package_id",
                        column: x => x.package_id,
                        principalSchema: "master",
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_estimate_items_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "master",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimate_items_bill_estimate_id",
                schema: "billing",
                table: "bill_estimate_items",
                column: "bill_estimate_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimate_items_package_id",
                schema: "billing",
                table: "bill_estimate_items",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimate_items_service_id",
                schema: "billing",
                table: "bill_estimate_items",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimates_bill_estimate_template_id",
                schema: "billing",
                table: "bill_estimates",
                column: "bill_estimate_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimates_branch_id",
                schema: "billing",
                table: "bill_estimates",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimates_estimate_number",
                schema: "billing",
                table: "bill_estimates",
                column: "estimate_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimates_patient_id",
                schema: "billing",
                table: "bill_estimates",
                column: "patient_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bill_estimate_items",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "bill_estimates",
                schema: "billing");
        }
    }
}
