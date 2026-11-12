using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prixo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_BranchScopedDocumentNumberUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_work_orders_work_order_number",
                schema: "biomed",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "ix_vouchers_voucher_number",
                schema: "finance",
                table: "vouchers");

            migrationBuilder.DropIndex(
                name: "ix_surgery_bookings_booking_number",
                schema: "ot",
                table: "surgery_bookings");

            migrationBuilder.DropIndex(
                name: "ix_surgery_bookings_branch_id",
                schema: "ot",
                table: "surgery_bookings");

            migrationBuilder.DropIndex(
                name: "ix_stock_transfers_branch_id",
                schema: "pharmacy",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "ix_stock_transfers_transfer_number",
                schema: "pharmacy",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "ix_stock_audits_audit_number",
                schema: "pharmacy",
                table: "stock_audits");

            migrationBuilder.DropIndex(
                name: "ix_stock_audits_branch_id",
                schema: "pharmacy",
                table: "stock_audits");

            migrationBuilder.DropIndex(
                name: "ix_stock_adjustments_adjustment_number",
                schema: "pharmacy",
                table: "stock_adjustments");

            migrationBuilder.DropIndex(
                name: "ix_stock_adjustments_branch_id",
                schema: "pharmacy",
                table: "stock_adjustments");

            migrationBuilder.DropIndex(
                name: "ix_samples_branch_id",
                schema: "lab",
                table: "samples");

            migrationBuilder.DropIndex(
                name: "ix_samples_sample_number",
                schema: "lab",
                table: "samples");

            migrationBuilder.DropIndex(
                name: "ix_sales_returns_branch_id",
                schema: "pharmacy",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ix_sales_returns_return_number",
                schema: "pharmacy",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ix_record_files_branch_id",
                schema: "mrd",
                table: "record_files");

            migrationBuilder.DropIndex(
                name: "ix_record_files_file_number",
                schema: "mrd",
                table: "record_files");

            migrationBuilder.DropIndex(
                name: "ix_rad_orders_order_number",
                schema: "rad",
                table: "rad_orders");

            migrationBuilder.DropIndex(
                name: "ix_purchase_returns_branch_id",
                schema: "pharmacy",
                table: "purchase_returns");

            migrationBuilder.DropIndex(
                name: "ix_purchase_returns_return_number",
                schema: "pharmacy",
                table: "purchase_returns");

            migrationBuilder.DropIndex(
                name: "ix_purchase_requests_branch_id",
                schema: "pharmacy",
                table: "purchase_requests");

            migrationBuilder.DropIndex(
                name: "ix_purchase_requests_request_number",
                schema: "pharmacy",
                table: "purchase_requests");

            migrationBuilder.DropIndex(
                name: "ix_purchase_orders_branch_id",
                schema: "pharmacy",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_purchase_orders_po_number",
                schema: "pharmacy",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_pharmacy_sales_sale_number",
                schema: "pharmacy",
                table: "pharmacy_sales");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_visit_number",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropIndex(
                name: "ix_lab_orders_order_number",
                schema: "lab",
                table: "lab_orders");

            migrationBuilder.DropIndex(
                name: "ix_issues_issue_number",
                schema: "pharmacy",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_issue_returns_branch_id",
                schema: "pharmacy",
                table: "issue_returns");

            migrationBuilder.DropIndex(
                name: "ix_issue_returns_return_number",
                schema: "pharmacy",
                table: "issue_returns");

            migrationBuilder.DropIndex(
                name: "ix_indents_branch_id",
                schema: "pharmacy",
                table: "indents");

            migrationBuilder.DropIndex(
                name: "ix_indents_indent_number",
                schema: "pharmacy",
                table: "indents");

            migrationBuilder.DropIndex(
                name: "ix_grns_branch_id",
                schema: "pharmacy",
                table: "grns");

            migrationBuilder.DropIndex(
                name: "ix_grns_grn_number",
                schema: "pharmacy",
                table: "grns");

            migrationBuilder.DropIndex(
                name: "ix_emergency_visits_visit_number",
                schema: "er",
                table: "emergency_visits");

            migrationBuilder.DropIndex(
                name: "ix_breakdown_complaints_complaint_number",
                schema: "biomed",
                table: "breakdown_complaints");

            migrationBuilder.DropIndex(
                name: "ix_bills_bill_number",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropIndex(
                name: "ix_bill_estimates_branch_id",
                schema: "billing",
                table: "bill_estimates");

            migrationBuilder.DropIndex(
                name: "ix_bill_estimates_estimate_number",
                schema: "billing",
                table: "bill_estimates");

            migrationBuilder.DropIndex(
                name: "ix_admissions_admission_number",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.DropIndex(
                name: "ix_admissions_branch_id",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_branch_id_work_order_number",
                schema: "biomed",
                table: "work_orders",
                columns: new[] { "branch_id", "work_order_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_branch_id_voucher_number",
                schema: "finance",
                table: "vouchers",
                columns: new[] { "branch_id", "voucher_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_branch_id_booking_number",
                schema: "ot",
                table: "surgery_bookings",
                columns: new[] { "branch_id", "booking_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_branch_id_transfer_number",
                schema: "pharmacy",
                table: "stock_transfers",
                columns: new[] { "branch_id", "transfer_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_audits_branch_id_audit_number",
                schema: "pharmacy",
                table: "stock_audits",
                columns: new[] { "branch_id", "audit_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_branch_id_adjustment_number",
                schema: "pharmacy",
                table: "stock_adjustments",
                columns: new[] { "branch_id", "adjustment_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_samples_branch_id_sample_number",
                schema: "lab",
                table: "samples",
                columns: new[] { "branch_id", "sample_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_branch_id_return_number",
                schema: "pharmacy",
                table: "sales_returns",
                columns: new[] { "branch_id", "return_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_record_files_branch_id_file_number",
                schema: "mrd",
                table: "record_files",
                columns: new[] { "branch_id", "file_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_branch_id_order_number",
                schema: "rad",
                table: "rad_orders",
                columns: new[] { "branch_id", "order_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_branch_id_return_number",
                schema: "pharmacy",
                table: "purchase_returns",
                columns: new[] { "branch_id", "return_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requests_branch_id_request_number",
                schema: "pharmacy",
                table: "purchase_requests",
                columns: new[] { "branch_id", "request_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_branch_id_po_number",
                schema: "pharmacy",
                table: "purchase_orders",
                columns: new[] { "branch_id", "po_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_branch_id_sale_number",
                schema: "pharmacy",
                table: "pharmacy_sales",
                columns: new[] { "branch_id", "sale_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_branch_id_visit_number",
                schema: "opd",
                table: "op_visits",
                columns: new[] { "branch_id", "visit_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_branch_id_order_number",
                schema: "lab",
                table: "lab_orders",
                columns: new[] { "branch_id", "order_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_issues_branch_id_issue_number",
                schema: "pharmacy",
                table: "issues",
                columns: new[] { "branch_id", "issue_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_issue_returns_branch_id_return_number",
                schema: "pharmacy",
                table: "issue_returns",
                columns: new[] { "branch_id", "return_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_indents_branch_id_indent_number",
                schema: "pharmacy",
                table: "indents",
                columns: new[] { "branch_id", "indent_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_grns_branch_id_grn_number",
                schema: "pharmacy",
                table: "grns",
                columns: new[] { "branch_id", "grn_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_visits_branch_id_visit_number",
                schema: "er",
                table: "emergency_visits",
                columns: new[] { "branch_id", "visit_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_complaints_branch_id_complaint_number",
                schema: "biomed",
                table: "breakdown_complaints",
                columns: new[] { "branch_id", "complaint_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_bills_branch_id_bill_number",
                schema: "billing",
                table: "bills",
                columns: new[] { "branch_id", "bill_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_bill_estimates_branch_id_estimate_number",
                schema: "billing",
                table: "bill_estimates",
                columns: new[] { "branch_id", "estimate_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_branch_id_admission_number",
                schema: "ipd",
                table: "admissions",
                columns: new[] { "branch_id", "admission_number" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_work_orders_branch_id_work_order_number",
                schema: "biomed",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "ix_vouchers_branch_id_voucher_number",
                schema: "finance",
                table: "vouchers");

            migrationBuilder.DropIndex(
                name: "ix_surgery_bookings_branch_id_booking_number",
                schema: "ot",
                table: "surgery_bookings");

            migrationBuilder.DropIndex(
                name: "ix_stock_transfers_branch_id_transfer_number",
                schema: "pharmacy",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "ix_stock_audits_branch_id_audit_number",
                schema: "pharmacy",
                table: "stock_audits");

            migrationBuilder.DropIndex(
                name: "ix_stock_adjustments_branch_id_adjustment_number",
                schema: "pharmacy",
                table: "stock_adjustments");

            migrationBuilder.DropIndex(
                name: "ix_samples_branch_id_sample_number",
                schema: "lab",
                table: "samples");

            migrationBuilder.DropIndex(
                name: "ix_sales_returns_branch_id_return_number",
                schema: "pharmacy",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ix_record_files_branch_id_file_number",
                schema: "mrd",
                table: "record_files");

            migrationBuilder.DropIndex(
                name: "ix_rad_orders_branch_id_order_number",
                schema: "rad",
                table: "rad_orders");

            migrationBuilder.DropIndex(
                name: "ix_purchase_returns_branch_id_return_number",
                schema: "pharmacy",
                table: "purchase_returns");

            migrationBuilder.DropIndex(
                name: "ix_purchase_requests_branch_id_request_number",
                schema: "pharmacy",
                table: "purchase_requests");

            migrationBuilder.DropIndex(
                name: "ix_purchase_orders_branch_id_po_number",
                schema: "pharmacy",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_pharmacy_sales_branch_id_sale_number",
                schema: "pharmacy",
                table: "pharmacy_sales");

            migrationBuilder.DropIndex(
                name: "ix_op_visits_branch_id_visit_number",
                schema: "opd",
                table: "op_visits");

            migrationBuilder.DropIndex(
                name: "ix_lab_orders_branch_id_order_number",
                schema: "lab",
                table: "lab_orders");

            migrationBuilder.DropIndex(
                name: "ix_issues_branch_id_issue_number",
                schema: "pharmacy",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_issue_returns_branch_id_return_number",
                schema: "pharmacy",
                table: "issue_returns");

            migrationBuilder.DropIndex(
                name: "ix_indents_branch_id_indent_number",
                schema: "pharmacy",
                table: "indents");

            migrationBuilder.DropIndex(
                name: "ix_grns_branch_id_grn_number",
                schema: "pharmacy",
                table: "grns");

            migrationBuilder.DropIndex(
                name: "ix_emergency_visits_branch_id_visit_number",
                schema: "er",
                table: "emergency_visits");

            migrationBuilder.DropIndex(
                name: "ix_breakdown_complaints_branch_id_complaint_number",
                schema: "biomed",
                table: "breakdown_complaints");

            migrationBuilder.DropIndex(
                name: "ix_bills_branch_id_bill_number",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropIndex(
                name: "ix_bill_estimates_branch_id_estimate_number",
                schema: "billing",
                table: "bill_estimates");

            migrationBuilder.DropIndex(
                name: "ix_admissions_branch_id_admission_number",
                schema: "ipd",
                table: "admissions");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_work_order_number",
                schema: "biomed",
                table: "work_orders",
                column: "work_order_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_voucher_number",
                schema: "finance",
                table: "vouchers",
                column: "voucher_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_booking_number",
                schema: "ot",
                table: "surgery_bookings",
                column: "booking_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_bookings_branch_id",
                schema: "ot",
                table: "surgery_bookings",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_branch_id",
                schema: "pharmacy",
                table: "stock_transfers",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_transfer_number",
                schema: "pharmacy",
                table: "stock_transfers",
                column: "transfer_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_audits_audit_number",
                schema: "pharmacy",
                table: "stock_audits",
                column: "audit_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_audits_branch_id",
                schema: "pharmacy",
                table: "stock_audits",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_adjustment_number",
                schema: "pharmacy",
                table: "stock_adjustments",
                column: "adjustment_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_branch_id",
                schema: "pharmacy",
                table: "stock_adjustments",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_samples_branch_id",
                schema: "lab",
                table: "samples",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_samples_sample_number",
                schema: "lab",
                table: "samples",
                column: "sample_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_branch_id",
                schema: "pharmacy",
                table: "sales_returns",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_return_number",
                schema: "pharmacy",
                table: "sales_returns",
                column: "return_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_record_files_branch_id",
                schema: "mrd",
                table: "record_files",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_files_file_number",
                schema: "mrd",
                table: "record_files",
                column: "file_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_rad_orders_order_number",
                schema: "rad",
                table: "rad_orders",
                column: "order_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_branch_id",
                schema: "pharmacy",
                table: "purchase_returns",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_return_number",
                schema: "pharmacy",
                table: "purchase_returns",
                column: "return_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requests_branch_id",
                schema: "pharmacy",
                table: "purchase_requests",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requests_request_number",
                schema: "pharmacy",
                table: "purchase_requests",
                column: "request_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_branch_id",
                schema: "pharmacy",
                table: "purchase_orders",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_po_number",
                schema: "pharmacy",
                table: "purchase_orders",
                column: "po_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_sales_sale_number",
                schema: "pharmacy",
                table: "pharmacy_sales",
                column: "sale_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_op_visits_visit_number",
                schema: "opd",
                table: "op_visits",
                column: "visit_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lab_orders_order_number",
                schema: "lab",
                table: "lab_orders",
                column: "order_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_issues_issue_number",
                schema: "pharmacy",
                table: "issues",
                column: "issue_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_issue_returns_branch_id",
                schema: "pharmacy",
                table: "issue_returns",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_returns_return_number",
                schema: "pharmacy",
                table: "issue_returns",
                column: "return_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_indents_branch_id",
                schema: "pharmacy",
                table: "indents",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_indents_indent_number",
                schema: "pharmacy",
                table: "indents",
                column: "indent_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_grns_branch_id",
                schema: "pharmacy",
                table: "grns",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_grns_grn_number",
                schema: "pharmacy",
                table: "grns",
                column: "grn_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_visits_visit_number",
                schema: "er",
                table: "emergency_visits",
                column: "visit_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_complaints_complaint_number",
                schema: "biomed",
                table: "breakdown_complaints",
                column: "complaint_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_bills_bill_number",
                schema: "billing",
                table: "bills",
                column: "bill_number",
                unique: true,
                filter: "is_deleted = false");

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
                name: "ix_admissions_admission_number",
                schema: "ipd",
                table: "admissions",
                column: "admission_number",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_branch_id",
                schema: "ipd",
                table: "admissions",
                column: "branch_id");
        }
    }
}
