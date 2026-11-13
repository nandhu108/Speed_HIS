using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20261113_GoLive_QaFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "admission_id",
                schema: "lab",
                table: "lab_orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "bill_item_id",
                schema: "pharmacy",
                table: "issue_items",
                type: "bigint",
                nullable: true);

            // Discharge clearances are now per department (IPD.DISCHARGECLEAR.BILLING/PHARMACY/NURSING/
            // INVESTIGATION). The generic IPD.DISCHARGE.CLEAR is kept only as an administrator override, so
            // take it away from the Head Nurse role (role seeding is additive and would not remove it).
            migrationBuilder.Sql("""
                UPDATE admin.role_permissions rp
                SET is_deleted = true, updated_at = now()
                FROM admin.roles r, admin.permissions p
                WHERE rp.role_id = r.id AND rp.permission_id = p.id
                  AND r.code = 'HEAD_NURSE' AND p.code = 'IPD.DISCHARGE.CLEAR' AND rp.is_deleted = false;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_admission_id",
                schema: "lab",
                table: "lab_orders",
                column: "admission_id");

            migrationBuilder.AddForeignKey(
                name: "fk_lab_orders_admissions_admission_id",
                schema: "lab",
                table: "lab_orders",
                column: "admission_id",
                principalSchema: "ipd",
                principalTable: "admissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_lab_orders_admissions_admission_id",
                schema: "lab",
                table: "lab_orders");

            migrationBuilder.DropIndex(
                name: "ix_lab_orders_admission_id",
                schema: "lab",
                table: "lab_orders");

            migrationBuilder.DropColumn(
                name: "admission_id",
                schema: "lab",
                table: "lab_orders");

            migrationBuilder.DropColumn(
                name: "bill_item_id",
                schema: "pharmacy",
                table: "issue_items");
        }
    }
}
