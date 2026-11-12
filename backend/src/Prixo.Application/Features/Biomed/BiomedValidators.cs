using FluentValidation;

namespace Prixo.Application.Features.Biomed;

public class SaveEquipmentRequestValidator : AbstractValidator<SaveEquipmentRequest>
{
    public SaveEquipmentRequestValidator()
    {
        RuleFor(x => x.EquipmentCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CurrentLocation).NotEmpty().MaximumLength(200);
    }
}

public class RecordEquipmentMovementRequestValidator : AbstractValidator<RecordEquipmentMovementRequest>
{
    public RecordEquipmentMovementRequestValidator()
    {
        RuleFor(x => x.ToLocation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class SaveAmcContractRequestValidator : AbstractValidator<SaveAmcContractRequest>
{
    public SaveAmcContractRequestValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0);
        RuleFor(x => x.VendorId).GreaterThan(0);
        RuleFor(x => x.ContractNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RenewalReminderDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
    }
}

public class SavePmScheduleRequestValidator : AbstractValidator<SavePmScheduleRequest>
{
    public SavePmScheduleRequestValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0);
        RuleFor(x => x.FrequencyDays).GreaterThan(0);
    }
}

public class CreateWorkOrderFromComplaintRequestValidator : AbstractValidator<CreateWorkOrderFromComplaintRequest>
{
    public CreateWorkOrderFromComplaintRequestValidator()
    {
        RuleFor(x => x.BreakdownComplaintId).GreaterThan(0);
    }
}

public class RecordSpareUsageRequestValidator : AbstractValidator<RecordSpareUsageRequest>
{
    public RecordSpareUsageRequestValidator()
    {
        RuleFor(x => x.BatchId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class RaiseComplaintRequestValidator : AbstractValidator<RaiseComplaintRequest>
{
    public RaiseComplaintRequestValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}

public class ResolveComplaintRequestValidator : AbstractValidator<ResolveComplaintRequest>
{
    public ResolveComplaintRequestValidator()
    {
        RuleFor(x => x.Resolution).NotEmpty().MaximumLength(1000);
    }
}

public class SaveCalibrationRequestValidator : AbstractValidator<SaveCalibrationRequest>
{
    public SaveCalibrationRequestValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0);
        RuleFor(x => x.CertificateNumber).MaximumLength(100);
    }
}

public class LogServiceHistoryRequestValidator : AbstractValidator<LogServiceHistoryRequest>
{
    public LogServiceHistoryRequestValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0);
        RuleFor(x => x.ServiceType).NotEmpty().MaximumLength(100);
    }
}
