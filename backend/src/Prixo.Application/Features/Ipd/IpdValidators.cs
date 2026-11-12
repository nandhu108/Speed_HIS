using FluentValidation;

namespace Prixo.Application.Features.Ipd;

public class AdmitPatientRequestValidator : AbstractValidator<AdmitPatientRequest>
{
    public AdmitPatientRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.PrimaryConsultantId).GreaterThan(0);
        RuleFor(x => x.DepartmentId)
            .NotNull().WithMessage("Select the department (e.g. General Medicine, General Surgery, Paediatrics).")
            .GreaterThan(0).WithMessage("Select the department (e.g. General Medicine, General Surgery, Paediatrics).");
        RuleFor(x => x.BedId).GreaterThan(0);
        RuleFor(x => x.InitialDepositAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ConsentSignedByName).NotEmpty().When(x => x.ConsentTemplateId is not null);
    }
}

public class TransferBedRequestValidator : AbstractValidator<TransferBedRequest>
{
    public TransferBedRequestValidator()
    {
        RuleFor(x => x.NewBedId).GreaterThan(0);
    }
}

public class TransferConsultantRequestValidator : AbstractValidator<TransferConsultantRequest>
{
    public TransferConsultantRequestValidator()
    {
        RuleFor(x => x.NewPrimaryConsultantId).GreaterThan(0);
    }
}

public class AddSecondaryConsultantRequestValidator : AbstractValidator<AddSecondaryConsultantRequest>
{
    public AddSecondaryConsultantRequestValidator()
    {
        RuleFor(x => x.DoctorId).GreaterThan(0);
    }
}

public class CancelAdmissionRequestValidator : AbstractValidator<CancelAdmissionRequest>
{
    public CancelAdmissionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class SetCreditLimitRequestValidator : AbstractValidator<SetCreditLimitRequest>
{
    public SetCreditLimitRequestValidator()
    {
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0).When(x => x.CreditLimit.HasValue);
    }
}

public class CreateDoctorOrderRequestValidator : AbstractValidator<CreateDoctorOrderRequest>
{
    public CreateDoctorOrderRequestValidator()
    {
        RuleFor(x => x.OrderedByDoctorId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Medication).NotNull().When(x => x.OrderType == Domain.Enums.DoctorOrderType.Medication)
            .WithMessage("A medication order needs the drug, dose, route and frequency.");
        RuleFor(x => x.Medication!).SetValidator(new MedicationOrderRequestValidator()).When(x => x.Medication is not null);
        RuleFor(x => x.Diet).NotNull().When(x => x.OrderType == Domain.Enums.DoctorOrderType.Diet)
            .WithMessage("A diet order needs a diet type.");
    }
}

public class AddVitalsRequestValidator : AbstractValidator<AddVitalsRequest>
{
    public AddVitalsRequestValidator()
    {
        RuleFor(x => x.TemperatureF).InclusiveBetween(80, 115).When(x => x.TemperatureF is not null);
        RuleFor(x => x.PulseRate).InclusiveBetween(0, 300).When(x => x.PulseRate is not null);
        RuleFor(x => x.RespiratoryRate).InclusiveBetween(0, 100).When(x => x.RespiratoryRate is not null);
        RuleFor(x => x.BloodPressureSystolic).InclusiveBetween(0, 350).When(x => x.BloodPressureSystolic is not null);
        RuleFor(x => x.BloodPressureDiastolic).InclusiveBetween(0, 250).When(x => x.BloodPressureDiastolic is not null);
        RuleFor(x => x).Must(x => x.BloodPressureSystolic is null || x.BloodPressureDiastolic is null || x.BloodPressureSystolic > x.BloodPressureDiastolic)
            .WithName("BloodPressure").WithMessage("Systolic pressure must be higher than diastolic.");
        RuleFor(x => x.SpO2).InclusiveBetween(0, 100).When(x => x.SpO2 is not null);
        RuleFor(x => x.RandomBloodSugar).InclusiveBetween(0, 1000).When(x => x.RandomBloodSugar is not null);
        RuleFor(x => x.GlasgowComaScale).InclusiveBetween(3, 15).When(x => x.GlasgowComaScale is not null);
        RuleFor(x => x.MeanArterialPressure).InclusiveBetween(0, 250).When(x => x.MeanArterialPressure is not null);
        RuleFor(x => x.CentralVenousPressure).InclusiveBetween(-10, 40).When(x => x.CentralVenousPressure is not null);
        RuleFor(x => x.UrineOutputMl).InclusiveBetween(0, 10000).When(x => x.UrineOutputMl is not null);
    }
}

public class MedicationOrderRequestValidator : AbstractValidator<MedicationOrderRequest>
{
    public MedicationOrderRequestValidator()
    {
        RuleFor(x => x.DrugId).GreaterThan(0);
        RuleFor(x => x.Dose).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Route).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate is not null)
            .WithMessage("End date cannot be before the start date.");
    }
}

public class CancelDoctorOrderRequestValidator : AbstractValidator<CancelDoctorOrderRequest>
{
    public CancelDoctorOrderRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class AddProgressNoteRequestValidator : AbstractValidator<AddProgressNoteRequest>
{
    public AddProgressNoteRequestValidator()
    {
        RuleFor(x => x.DoctorId).GreaterThan(0);
        RuleFor(x => x.Notes).NotEmpty();
    }
}

public class AddIntakeOutputRequestValidator : AbstractValidator<AddIntakeOutputRequest>
{
    public AddIntakeOutputRequestValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.QuantityMl).GreaterThan(0);
    }
}

public class AddProcedureNoteRequestValidator : AbstractValidator<AddProcedureNoteRequest>
{
    public AddProcedureNoteRequestValidator()
    {
        RuleFor(x => x.ProcedureName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PerformedByDoctorId).GreaterThan(0);
    }
}

public class RequestDischargeRequestValidator : AbstractValidator<RequestDischargeRequest>
{
    public RequestDischargeRequestValidator()
    {
        RuleFor(x => x.RequestedByDoctorId).GreaterThan(0);
    }
}

public class UpsertDischargeSummaryRequestValidator : AbstractValidator<UpsertDischargeSummaryRequest>
{
    public UpsertDischargeSummaryRequestValidator()
    {
        RuleFor(x => x.FinalDiagnosis).NotEmpty().MaximumLength(1000);
    }
}

public class ApproveDischargeSummaryRequestValidator : AbstractValidator<ApproveDischargeSummaryRequest>
{
    public ApproveDischargeSummaryRequestValidator()
    {
        RuleFor(x => x.ApprovedByDoctorId).GreaterThan(0);
    }
}

public class SetBedStatusRequestValidator : AbstractValidator<SetBedStatusRequest>
{
    public SetBedStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class CreateAdmissionRequestRequestValidator : AbstractValidator<CreateAdmissionRequestRequest>
{
    public CreateAdmissionRequestRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class RejectAdmissionRequestRequestValidator : AbstractValidator<RejectAdmissionRequestRequest>
{
    public RejectAdmissionRequestRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
