using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Masters_BedTypePricingAndTrialBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tariff_items_tariff_id_service_id",
                schema: "master",
                table: "tariff_items");

            migrationBuilder.AddColumn<long>(
                name: "room_type_id",
                schema: "master",
                table: "tariff_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "room_type_charges",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_type_id = table.Column<long>(type: "bigint", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_room_type_charges", x => x.id);
                    table.CheckConstraint("ck_room_type_charges_effective_range", "effective_to IS NULL OR effective_to >= effective_from");
                    table.CheckConstraint("ck_room_type_charges_rate", "rate >= 0");
                    table.ForeignKey(
                        name: "fk_room_type_charges_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "admin",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_room_type_charges_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalSchema: "master",
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tariff_items_room_type_id",
                schema: "master",
                table: "tariff_items",
                column: "room_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_tariff_items_tariff_id_service_id_room_type_id",
                schema: "master",
                table: "tariff_items",
                columns: new[] { "tariff_id", "service_id", "room_type_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_room_type_charges_branch_id",
                schema: "master",
                table: "room_type_charges",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_room_type_charges_room_type_id",
                schema: "master",
                table: "room_type_charges",
                column: "room_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tariff_items_room_types_room_type_id",
                schema: "master",
                table: "tariff_items",
                column: "room_type_id",
                principalSchema: "master",
                principalTable: "room_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tariff_items_room_types_room_type_id",
                schema: "master",
                table: "tariff_items");

            migrationBuilder.DropTable(
                name: "room_type_charges",
                schema: "master");

            migrationBuilder.DropIndex(
                name: "ix_tariff_items_room_type_id",
                schema: "master",
                table: "tariff_items");

            migrationBuilder.DropIndex(
                name: "ix_tariff_items_tariff_id_service_id_room_type_id",
                schema: "master",
                table: "tariff_items");

            migrationBuilder.DropColumn(
                name: "room_type_id",
                schema: "master",
                table: "tariff_items");

            migrationBuilder.CreateIndex(
                name: "ix_tariff_items_tariff_id_service_id",
                schema: "master",
                table: "tariff_items",
                columns: new[] { "tariff_id", "service_id" },
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
