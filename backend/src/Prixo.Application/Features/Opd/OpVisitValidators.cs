using FluentValidation;

namespace Prixo.Application.Features.Opd;

public class RegisterOpVisitRequestValidator : AbstractValidator<RegisterOpVisitRequest>
{
    public RegisterOpVisitRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.DoctorId).GreaterThan(0);
        RuleFor(x => x.DepartmentId)
            .NotNull().WithMessage("Select the department (e.g. General Medicine, General Surgery, Paediatrics).")
            .GreaterThan(0).WithMessage("Select the department (e.g. General Medicine, General Surgery, Paediatrics).");
        RuleFor(x => x.AdvanceAmount).GreaterThan(0).When(x => x.AdvanceAmount is not null);
        RuleFor(x => x.PaymentModeId).NotNull()
            .When(x => x.AdvanceAmount is > 0)
            .WithMessage("Select a payment mode to collect the advance.");
    }
}

public class RegisterMhcVisitRequestValidator : AbstractValidator<RegisterMhcVisitRequest>
{
    public RegisterMhcVisitRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.MhcDoctorId).GreaterThan(0);
        RuleFor(x => x.MhcPackageId).GreaterThan(0);
    }
}

public class CancelVisitRequestValidator : AbstractValidator<CancelVisitRequest>
{
    public CancelVisitRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class SaveVitalsRequestValidator : AbstractValidator<SaveVitalsRequest>
{
    public SaveVitalsRequestValidator()
    {
        RuleFor(x => x.HeightCm).InclusiveBetween(1, 300).When(x => x.HeightCm is not null);
        RuleFor(x => x.WeightKg).InclusiveBetween(0.5m, 500).When(x => x.WeightKg is not null);
        RuleFor(x => x.TemperatureF).InclusiveBetween(80, 115).When(x => x.TemperatureF is not null);
        RuleFor(x => x).Must(x => x.BloodPressureSystolic is null || x.BloodPressureDiastolic is null || x.BloodPressureSystolic > x.BloodPressureDiastolic)
            .WithName("BloodPressure").WithMessage("Systolic pressure must be higher than diastolic.");
        RuleFor(x => x.PulseRate).InclusiveBetween(0, 300).When(x => x.PulseRate is not null);
        RuleFor(x => x.RespiratoryRate).InclusiveBetween(0, 100).When(x => x.RespiratoryRate is not null);
        RuleFor(x => x.BloodPressureSystolic).InclusiveBetween(0, 350).When(x => x.BloodPressureSystolic is not null);
        RuleFor(x => x.BloodPressureDiastolic).InclusiveBetween(0, 250).When(x => x.BloodPressureDiastolic is not null);
        RuleFor(x => x.SpO2).InclusiveBetween(0, 100).When(x => x.SpO2 is not null);
        RuleFor(x => x.RandomBloodSugar).InclusiveBetween(0, 1000).When(x => x.RandomBloodSugar is not null);
    }
}

public class SaveComplaintRequestValidator : AbstractValidator<SaveComplaintRequest>
{
    public SaveComplaintRequestValidator()
    {
        RuleFor(x => x.Complaint).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Duration).MaximumLength(50);
    }
}

public class SaveComplaintsRequestValidator : AbstractValidator<SaveComplaintsRequest>
{
    public SaveComplaintsRequestValidator()
    {
        RuleForEach(x => x.Complaints).SetValidator(new SaveComplaintRequestValidator());
    }
}

public class SaveExaminationRequestValidator : AbstractValidator<SaveExaminationRequest>
{
    public SaveExaminationRequestValidator()
    {
        RuleFor(x => x.HistoryNotes).MaximumLength(4000);
        RuleFor(x => x.ExaminationNotes).MaximumLength(4000);
    }
}

public class SaveDiagnosisRequestValidator : AbstractValidator<SaveDiagnosisRequest>
{
    public SaveDiagnosisRequestValidator()
    {
        RuleFor(x => x.IcdCodeId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class SaveDiagnosesRequestValidator : AbstractValidator<SaveDiagnosesRequest>
{
    public SaveDiagnosesRequestValidator()
    {
        RuleForEach(x => x.Diagnoses).SetValidator(new SaveDiagnosisRequestValidator());
    }
}

public class SavePrescriptionItemRequestValidator : AbstractValidator<SavePrescriptionItemRequest>
{
    public SavePrescriptionItemRequestValidator()
    {
        RuleFor(x => x.DrugId).GreaterThan(0);
        RuleFor(x => x.Dose).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Duration).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Route).MaximumLength(50);
        RuleFor(x => x.Instructions).MaximumLength(300);
    }
}

public class SavePrescriptionRequestValidator : AbstractValidator<SavePrescriptionRequest>
{
    public SavePrescriptionRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleForEach(x => x.Items).SetValidator(new SavePrescriptionItemRequestValidator());
    }
}

public class AddClinicalOrderRequestValidator : AbstractValidator<AddClinicalOrderRequest>
{
    public AddClinicalOrderRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class AddVisitReferralRequestValidator : AbstractValidator<AddVisitReferralRequest>
{
    public AddVisitReferralRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExternalDetails).MaximumLength(300);
        RuleFor(x => x.ReferredToDoctorId).NotNull()
            .When(x => x.Direction == Domain.Enums.ReferralDirection.Internal)
            .WithMessage("Select the doctor to refer to.");
        RuleFor(x => x.ExternalDetails).NotEmpty()
            .When(x => x.Direction == Domain.Enums.ReferralDirection.External)
            .WithMessage("Enter where the patient is being referred to.");
    }
}

public class SaveReferralOpinionRequestValidator : AbstractValidator<SaveReferralOpinionRequest>
{
    public SaveReferralOpinionRequestValidator()
    {
        RuleFor(x => x.OpinionNotes).NotEmpty().MaximumLength(1000);
    }
}

public class SaveFollowUpRequestValidator : AbstractValidator<SaveFollowUpRequest>
{
    public SaveFollowUpRequestValidator()
    {
        RuleFor(x => x.FollowUpDate).GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5)))
            .WithMessage("The follow-up date cannot be in the past.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class SaveTemplateItemRequestValidator : AbstractValidator<SaveTemplateItemRequest>
{
    public SaveTemplateItemRequestValidator()
    {
        RuleFor(x => x.DrugId).GreaterThan(0);
        RuleFor(x => x.Dose).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Duration).NotEmpty().MaximumLength(50);
    }
}

public class SavePrescriptionTemplateRequestValidator : AbstractValidator<SavePrescriptionTemplateRequest>
{
    public SavePrescriptionTemplateRequestValidator()
    {
        RuleFor(x => x.DoctorId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Add at least one drug to the template.");
        RuleForEach(x => x.Items).SetValidator(new SaveTemplateItemRequestValidator());
    }
}
