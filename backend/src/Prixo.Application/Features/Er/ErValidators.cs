using FluentValidation;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Er;

public class RegisterEmergencyVisitRequestValidator : AbstractValidator<RegisterEmergencyVisitRequest>
{
    public RegisterEmergencyVisitRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
    }
}

public class CancelEmergencyVisitRequestValidator : AbstractValidator<CancelEmergencyVisitRequest>
{
    public CancelEmergencyVisitRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RecordTriageRequestValidator : AbstractValidator<RecordTriageRequest>
{
    public RecordTriageRequestValidator()
    {
        RuleFor(x => x.ChiefComplaint).NotEmpty().MaximumLength(500);
    }
}

public class SaveAssessmentRequestValidator : AbstractValidator<SaveAssessmentRequest>
{
    public SaveAssessmentRequestValidator()
    {
        RuleFor(x => x.AssessedByDoctorId).GreaterThan(0);
        RuleFor(x => x.TransferredToFacility).NotEmpty()
            .When(x => x.Disposition == DispositionType.TransferredOut)
            .WithMessage("Specify the receiving facility for a transfer-out disposition.");
    }
}

public class AllocateObservationRequestValidator : AbstractValidator<AllocateObservationRequest>
{
    public AllocateObservationRequestValidator()
    {
        RuleFor(x => x.BedId).GreaterThan(0);
    }
}

public class SaveMlcRequestValidator : AbstractValidator<SaveMlcRequest>
{
    public SaveMlcRequestValidator()
    {
        RuleFor(x => x.PoliceStationName).MaximumLength(150);
        RuleFor(x => x.FirNumber).MaximumLength(50);
    }
}

public class SaveAccidentRequestValidator : AbstractValidator<SaveAccidentRequest>
{
    public SaveAccidentRequestValidator()
    {
        RuleFor(x => x.AccidentType).NotEmpty().MaximumLength(100);
    }
}
