using Prixo.Application.Common.Interfaces;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Prixo.Infrastructure.Services;

/// <summary>PDF generation via QuestPDF (Community license). Registration card now; bills/reports reuse this later.</summary>
public class QuestPdfService : IPdfService
{
    static QuestPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateRegistrationCard(RegistrationCardData data)
    {
        var qrBytes = GenerateQrCode(data.Uhid);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                    {
                        column.Item().Text(data.BranchAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Row(row =>
                {
                    row.RelativeItem(2).Column(column =>
                    {
                        column.Item().Text("Patient Registration Card").FontSize(11).Bold();
                        column.Item().PaddingTop(6).Text($"UHID: {data.Uhid}").FontSize(12).Bold();
                        column.Item().Text($"Name: {data.FullName}");
                        column.Item().Text($"Gender / Age: {data.Gender} / {data.AgeDisplay}");
                        column.Item().Text($"Mobile: {data.Mobile}");
                        if (!string.IsNullOrWhiteSpace(data.Address))
                        {
                            column.Item().Text($"Address: {data.Address}");
                        }
                        column.Item().PaddingTop(6).Text($"Registered: {data.RegisteredAt:dd-MMM-yyyy}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(90).Column(column =>
                    {
                        if (data.PhotoBytes is { Length: > 0 })
                        {
                            column.Item().Height(80).Width(80).Image(data.PhotoBytes).FitArea();
                            column.Item().PaddingTop(4);
                        }
                        column.Item().Height(80).Width(80).Image(qrBytes).FitArea();
                    });
                });

                page.Footer().AlignCenter().Text("Please bring this card on every visit.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateLoyaltyCard(LoyaltyCardData data)
    {
        var qrBytes = GenerateQrCode(data.CardNumber);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Row(row =>
                {
                    row.RelativeItem(2).Column(column =>
                    {
                        column.Item().Text("Loyalty Membership Card").FontSize(11).Bold();
                        column.Item().PaddingTop(6).Text($"Card No: {data.CardNumber}").FontSize(12).Bold();
                        column.Item().Text($"Name: {data.MemberName}");
                        column.Item().Text($"UHID: {data.MemberUhid}");
                        column.Item().Text($"Type: {data.CardTypeName}" + (data.CategoryName is null ? "" : $" ({data.CategoryName})"));
                        column.Item().PaddingTop(6).Text($"Issued: {data.IssuedDate:dd-MMM-yyyy}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        column.Item().Text($"Valid Until: {data.ExpiryDate:dd-MMM-yyyy}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(90).Column(column =>
                    {
                        if (data.PhotoBytes is { Length: > 0 })
                        {
                            column.Item().Height(80).Width(80).Image(data.PhotoBytes).FitArea();
                            column.Item().PaddingTop(4);
                        }
                        column.Item().Height(80).Width(80).Image(qrBytes).FitArea();
                    });
                });

                page.Footer().AlignCenter().Text("Please present this card at every visit.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateVisitSlip(VisitSlipData data)
    {
        var qrBytes = GenerateQrCode(data.VisitNumber);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Row(row =>
                {
                    row.RelativeItem(2).Column(column =>
                    {
                        column.Item().Text("OPD Visit Slip").FontSize(11).Bold();
                        column.Item().PaddingTop(6).Text($"Token #{data.TokenNumber}").FontSize(20).Bold();
                        column.Item().PaddingTop(6).Text($"Visit No: {data.VisitNumber}");
                        column.Item().Text($"Patient: {data.PatientName} ({data.PatientUhid})");
                        column.Item().Text($"Doctor: {data.DoctorName}");
                        column.Item().Text($"Type: {data.VisitType}");
                        column.Item().PaddingTop(6).Text($"Date: {data.VisitDate:dd-MMM-yyyy}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(80).Height(80).Width(80).Image(qrBytes).FitArea();
                });

                page.Footer().AlignCenter().Text("Please wait for your token to be called.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateVisitSummary(VisitSummaryData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    column.Item().Text("OPD Consultation Summary").FontSize(11).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Visit No: {data.VisitNumber}    Date: {data.VisitDate:dd-MMM-yyyy}").FontSize(9);
                    column.Item().Text($"Patient: {data.PatientName} ({data.PatientUhid})");
                    column.Item().Text($"Doctor: {data.DoctorName}");

                    if (data.Diagnoses.Count > 0)
                    {
                        column.Item().PaddingTop(10).Text("Diagnosis").Bold();
                        foreach (var diagnosis in data.Diagnoses)
                        {
                            column.Item().Text($"• {diagnosis}");
                        }
                    }

                    if (data.PrescriptionLines.Count > 0)
                    {
                        column.Item().PaddingTop(10).Text("Prescription").Bold();
                        foreach (var line in data.PrescriptionLines)
                        {
                            column.Item().Text($"• {line}");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(data.AdviceNotes))
                    {
                        column.Item().PaddingTop(10).Text("Advice").Bold();
                        column.Item().Text(data.AdviceNotes);
                    }

                    if (data.FollowUpDate is { } followUp)
                    {
                        column.Item().PaddingTop(10).Text($"Follow-up: {followUp:dd-MMM-yyyy}").Bold();
                    }
                });

                page.Footer().AlignCenter().Text("This summary does not replace a full medical record.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateBillReceipt(BillReceiptData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                    {
                        column.Item().Text(data.BranchAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().Text("Bill Receipt").FontSize(11).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Bill No: {data.BillNumber}    Date: {data.BillDate:dd-MMM-yyyy}").FontSize(9);
                    column.Item().Text($"Patient: {data.PatientName} ({data.PatientUhid})");

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.4f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Description").Bold();
                            header.Cell().AlignRight().Text("Qty").Bold();
                            header.Cell().AlignRight().Text("Rate").Bold();
                            header.Cell().AlignRight().Text("Disc.").Bold();
                            header.Cell().AlignRight().Text("Tax").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                            header.Cell().ColumnSpan(6).PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });

                        foreach (var item in data.Items)
                        {
                            table.Cell().Text(item.Description);
                            table.Cell().AlignRight().Text(item.Quantity.ToString("0.###"));
                            table.Cell().AlignRight().Text(item.Rate.ToString("0.00"));
                            table.Cell().AlignRight().Text(item.Discount.ToString("0.00"));
                            table.Cell().AlignRight().Text(item.Tax.ToString("0.00"));
                            table.Cell().AlignRight().Text(item.Total.ToString("0.00"));
                        }
                    });

                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().PaddingTop(6).AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Sub Total: {data.SubTotal:0.00}");
                        totals.Item().Text($"Discount: {data.DiscountAmount:0.00}");
                        totals.Item().Text($"Tax: {data.TaxAmount:0.00}");
                        totals.Item().Text($"Total: {data.TotalAmount:0.00}").FontSize(11).Bold();
                        totals.Item().Text($"Paid: {data.PaidAmount:0.00}");
                        totals.Item().Text($"Balance: {data.BalanceAmount:0.00}").Bold();
                    });

                    if (data.ReceiptNumber is not null)
                    {
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        column.Item().PaddingTop(6).Text($"Receipt No: {data.ReceiptNumber}").Bold();
                        if (data.PaymentModeName is not null)
                        {
                            column.Item().Text($"Payment Mode: {data.PaymentModeName}");
                        }
                        if (data.PaymentAmount is { } amount)
                        {
                            column.Item().Text($"Amount Received: {amount:0.00}");
                        }
                    }
                });

                page.Footer().AlignCenter()
                    .Text(string.IsNullOrWhiteSpace(data.FooterNote) ? "Thank you. Please retain this receipt for your records." : data.FooterNote)
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateBillEstimate(BillEstimateData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                    {
                        column.Item().Text(data.BranchAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().Text("Cost Estimate").FontSize(11).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Estimate No: {data.EstimateNumber}    Date: {data.EstimateDate:dd-MMM-yyyy}").FontSize(9);
                    column.Item().Text($"Patient: {data.PatientName} ({data.PatientUhid})");

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.4f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Description").Bold();
                            header.Cell().AlignRight().Text("Qty").Bold();
                            header.Cell().AlignRight().Text("Rate").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                            header.Cell().ColumnSpan(4).PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });

                        foreach (var item in data.Items)
                        {
                            table.Cell().Text(item.Description);
                            table.Cell().AlignRight().Text(item.Quantity.ToString("0.###"));
                            table.Cell().AlignRight().Text(item.Rate.ToString("0.00"));
                            table.Cell().AlignRight().Text(item.Total.ToString("0.00"));
                        }
                    });

                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    column.Item().PaddingTop(6).AlignRight().Text($"Estimated Total: {data.TotalAmount:0.00}").FontSize(11).Bold();

                    if (!string.IsNullOrWhiteSpace(data.Notes))
                    {
                        column.Item().PaddingTop(10).Text($"Notes: {data.Notes}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().AlignCenter()
                    .Text("This is an estimate only, not a bill — actual charges may vary.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateLabReport(LabReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                    {
                        column.Item().Text(data.BranchAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().Text("Laboratory Report").FontSize(11).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Patient: {data.PatientName} ({data.PatientUhid})");
                        row.RelativeItem().AlignRight().Text($"Order No: {data.OrderNumber}");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Age / Gender: {data.PatientAgeDisplay} / {data.PatientGender}");
                        row.RelativeItem().AlignRight().Text($"Order Date: {data.OrderDate:dd-MMM-yyyy}");
                    });
                    if (!string.IsNullOrWhiteSpace(data.ReferringDoctorName))
                    {
                        column.Item().Text($"Referring Doctor: {data.ReferringDoctorName}");
                    }

                    foreach (var section in data.Sections)
                    {
                        column.Item().PaddingTop(12).Text(section.TestName).FontSize(10).Bold();
                        if (!string.IsNullOrWhiteSpace(section.Method))
                        {
                            column.Item().Text($"Method: {section.Method}").FontSize(7).FontColor(Colors.Grey.Darken1);
                        }

                        column.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.8f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Parameter").Bold();
                                header.Cell().AlignRight().Text("Result").Bold();
                                header.Cell().Text("Unit").Bold();
                                header.Cell().Text("Reference Range").Bold();
                                header.Cell().ColumnSpan(4).PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                            });

                            foreach (var line in section.Values)
                            {
                                table.Cell().Text(line.ParameterName);
                                table.Cell().AlignRight().Text(text =>
                                {
                                    var span = text.Span(line.Value);
                                    if (line.IsCritical)
                                    {
                                        span.FontColor(Colors.Red.Darken2).Bold();
                                    }
                                    else if (line.IsAbnormal)
                                    {
                                        span.FontColor(Colors.Orange.Darken2).Bold();
                                    }
                                });
                                table.Cell().Text(line.Unit ?? "-");
                                table.Cell().Text(line.RangeDisplay ?? "-");
                            }
                        });

                        if (!string.IsNullOrWhiteSpace(section.Notes))
                        {
                            column.Item().PaddingTop(2).Text($"Notes: {section.Notes}").FontSize(8).Italic();
                        }
                    }

                    if (data.ApprovedAt is { } approvedAt)
                    {
                        column.Item().PaddingTop(16).Text($"Approved: {approvedAt:dd-MMM-yyyy HH:mm}" +
                            (data.ApprovedByName is { } name ? $" by {name}" : string.Empty)).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().AlignCenter().Text("This is a computer-generated report.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GeneratePharmacyInvoice(PharmacyInvoiceData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                    {
                        column.Item().Text(data.BranchAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().Text("Pharmacy Invoice").FontSize(11).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Invoice No: {data.SaleNumber}    Date: {data.SaleDate:dd-MMM-yyyy}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(data.PatientName))
                    {
                        column.Item().Text($"Patient: {data.PatientName}");
                    }

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.4f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Drug").Bold();
                            header.Cell().Text("Batch").Bold();
                            header.Cell().Text("Expiry").Bold();
                            header.Cell().AlignRight().Text("Qty").Bold();
                            header.Cell().AlignRight().Text("Rate").Bold();
                            header.Cell().AlignRight().Text("Tax").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                            header.Cell().ColumnSpan(7).PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        });

                        foreach (var item in data.Items)
                        {
                            table.Cell().Text(item.DrugName);
                            table.Cell().Text(item.BatchNumber);
                            table.Cell().Text(item.ExpiryDate.ToString("MM/yyyy"));
                            table.Cell().AlignRight().Text(item.Quantity.ToString("0.###"));
                            table.Cell().AlignRight().Text(item.Rate.ToString("0.00"));
                            table.Cell().AlignRight().Text(item.Tax.ToString("0.00"));
                            table.Cell().AlignRight().Text(item.Total.ToString("0.00"));
                        }
                    });

                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().PaddingTop(6).AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Sub Total: {data.SubTotal:0.00}");
                        totals.Item().Text($"Discount: {data.DiscountAmount:0.00}");
                        totals.Item().Text($"Tax: {data.TaxAmount:0.00}");
                        totals.Item().Text($"Total: {data.TotalAmount:0.00}").FontSize(11).Bold();
                    });
                });

                page.Footer().AlignCenter().Text("Thank you. Please retain this invoice for your records.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateAdmissionSlip(AdmissionSlipData data)
    {
        var qrBytes = GenerateQrCode(data.AdmissionNumber);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                    {
                        column.Item().Text(data.BranchAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().Text("Admission Slip").FontSize(11).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Row(row =>
                {
                    row.RelativeItem(2).Column(column =>
                    {
                        column.Item().Text($"Admission No: {data.AdmissionNumber}").FontSize(12).Bold();
                        column.Item().PaddingTop(6).Text($"Patient: {data.PatientName} ({data.PatientUhid})");
                        column.Item().Text($"Gender / Age: {data.PatientGender} / {data.PatientAgeDisplay}");
                        column.Item().Text($"Admission Type: {data.AdmissionType}");
                        column.Item().Text($"Consultant: {data.PrimaryConsultantName}");
                        column.Item().Text($"Ward / Room / Bed: {data.WardName} / {data.RoomName} / {data.BedCode}");
                        column.Item().PaddingTop(6).Text($"Admitted: {data.AdmissionDate:dd-MMM-yyyy HH:mm}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        if (data.InitialDepositAmount > 0)
                        {
                            column.Item().PaddingTop(6).Text($"Initial Deposit: {data.InitialDepositAmount:0.00}").Bold();
                        }
                    });

                    row.ConstantItem(80).Height(80).Width(80).Image(qrBytes).FitArea();
                });

                page.Footer().AlignCenter().Text("Please retain this slip for the duration of the stay.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateDischargeSummaryPdf(DischargeSummaryPdfData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                    {
                        column.Item().Text(data.BranchAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().Text("Discharge Summary").FontSize(12).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Admission No: {data.AdmissionNumber}").Bold();
                    column.Item().Text($"Patient: {data.PatientName} ({data.PatientUhid})");
                    column.Item().Text($"Gender / Age: {data.PatientGender} / {data.PatientAgeDisplay}");
                    column.Item().Text($"Consultant: {data.PrimaryConsultantName}");
                    column.Item().Text($"Admitted: {data.AdmissionDate:dd-MMM-yyyy}    Discharged: {data.DischargedAt:dd-MMM-yyyy}");

                    column.Item().PaddingTop(10).Text("Final Diagnosis").Bold();
                    column.Item().Text(data.FinalDiagnosis);

                    if (!string.IsNullOrWhiteSpace(data.CourseInHospital))
                    {
                        column.Item().PaddingTop(10).Text("Course in Hospital").Bold();
                        column.Item().Text(data.CourseInHospital);
                    }
                    if (!string.IsNullOrWhiteSpace(data.MedicationsOnDischarge))
                    {
                        column.Item().PaddingTop(10).Text("Medications on Discharge").Bold();
                        column.Item().Text(data.MedicationsOnDischarge);
                    }
                    if (!string.IsNullOrWhiteSpace(data.FollowUpAdvice))
                    {
                        column.Item().PaddingTop(10).Text("Follow-up Advice").Bold();
                        column.Item().Text(data.FollowUpAdvice);
                    }
                    if (!string.IsNullOrWhiteSpace(data.ConditionAtDischarge))
                    {
                        column.Item().PaddingTop(10).Text($"Condition at Discharge: {data.ConditionAtDischarge}").Bold();
                    }

                    if (data.ApprovedByName is not null)
                    {
                        column.Item().PaddingTop(20).Text($"Approved by: {data.ApprovedByName}").FontSize(9);
                        if (data.ApprovedAt is { } approvedAt)
                        {
                            column.Item().Text($"on {approvedAt:dd-MMM-yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                    }
                });

                page.Footer().AlignCenter().Text("This is a system-generated discharge summary.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateDischargeSlip(DischargeSlipData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    column.Item().Text("Discharge Slip").FontSize(11).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text($"Admission No: {data.AdmissionNumber}").Bold();
                    column.Item().Text($"Patient: {data.PatientName} ({data.PatientUhid})");
                    column.Item().Text($"Ward / Room / Bed: {data.WardName} / {data.RoomName} / {data.BedCode}");
                    column.Item().Text($"Admitted: {data.AdmissionDate:dd-MMM-yyyy}");
                    column.Item().Text($"Discharged: {data.DischargedAt:dd-MMM-yyyy HH:mm}");
                    column.Item().Text($"Discharge Type: {data.DischargeType}");

                    column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    column.Item().PaddingTop(6).Column(totals =>
                    {
                        totals.Item().Text($"Bill Total: {data.BillTotalAmount:0.00}");
                        totals.Item().Text($"Bill Balance: {data.BillBalanceAmount:0.00}").Bold();
                    });
                });

                page.Footer().AlignCenter().Text("Please carry this slip along with the discharge summary.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateRadiologyReport(RadiologyReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.BranchName).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                    {
                        column.Item().Text(data.BranchAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().Text("Radiology Report").FontSize(12).SemiBold();
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Patient: {data.PatientName} ({data.PatientUhid})");
                        row.RelativeItem().AlignRight().Text($"Order No: {data.OrderNumber}");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Age / Gender: {data.PatientAgeDisplay} / {data.PatientGender}");
                        row.RelativeItem().AlignRight().Text($"Accession No: {data.AccessionNumber}");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Study: {data.TestName} ({data.ModalityName}) — {data.BodyPart}");
                        row.RelativeItem().AlignRight().Text($"Order Date: {data.OrderDate:dd-MMM-yyyy}");
                    });
                    if (!string.IsNullOrWhiteSpace(data.ReferringDoctorName))
                    {
                        column.Item().Text($"Referring Doctor: {data.ReferringDoctorName}");
                    }

                    column.Item().PaddingTop(12).Text("Findings").Bold();
                    column.Item().Text(data.Findings);

                    column.Item().PaddingTop(10).Text("Impression").Bold();
                    column.Item().Text(data.Impression);

                    if (data.ApprovedByName is not null)
                    {
                        column.Item().PaddingTop(20).Text($"Approved by: {data.ApprovedByName}").FontSize(9);
                        if (data.ApprovedAt is { } approvedAt)
                        {
                            column.Item().Text($"on {approvedAt:dd-MMM-yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                    }
                });

                page.Footer().AlignCenter().Text("This is a system-generated radiology report.")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateTabularReport(TabularReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.Title).FontSize(14).Bold();
                    if (data.DateRangeLabel is not null)
                    {
                        column.Item().Text(data.DateRangeLabel).FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in data.ColumnLabels)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var label in data.ColumnLabels)
                        {
                            header.Cell().Text(label).Bold();
                        }
                        header.Cell().ColumnSpan((uint)data.ColumnLabels.Count).PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    foreach (var row in data.Rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().Text(cell);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.DefaultTextStyle(TextStyle.Default.FontSize(7).FontColor(Colors.Grey.Darken1));
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static byte[] GenerateQrCode(string content)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(10);
    }
}
