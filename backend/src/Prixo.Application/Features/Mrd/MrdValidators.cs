using FluentValidation;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Mrd;

public class CreateRecordFileRequestValidator : AbstractValidator<CreateRecordFileRequest>
{
    public CreateRecordFileRequestValidator()
    {
        RuleFor(x => x.AdmissionId).GreaterThan(0);
    }
}

public class RecordMovementRequestValidator : AbstractValidator<RecordMovementRequest>
{
    public RecordMovementRequestValidator()
    {
        RuleFor(x => x.ToLocation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class CreateFileRequestRequestValidator : AbstractValidator<CreateFileRequestRequest>
{
    public CreateFileRequestRequestValidator()
    {
        RuleFor(x => x.RecordFileId).GreaterThan(0);
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RequesterName).NotEmpty().MaximumLength(200)
            .When(x => x.RequestType is FileRequestType.External or FileRequestType.Legal)
            .WithMessage("Specify the requester's name for an external/legal record request.");
    }
}

public class RejectFileRequestRequestValidator : AbstractValidator<RejectFileRequestRequest>
{
    public RejectFileRequestRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class SaveFileAuditRequestValidator : AbstractValidator<SaveFileAuditRequest>
{
    public SaveFileAuditRequestValidator()
    {
        RuleFor(x => x.MissingDocumentsNotes).MaximumLength(1000);
    }
}

public class SaveRetentionPolicyRequestValidator : AbstractValidator<SaveRetentionPolicyRequest>
{
    public SaveRetentionPolicyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30).Matches("^[A-Z0-9_-]+$")
            .WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RetentionYears).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateDestructionEntryRequestValidator : AbstractValidator<CreateDestructionEntryRequest>
{
    public CreateDestructionEntryRequestValidator()
    {
        RuleFor(x => x.RecordFileId).GreaterThan(0);
        RuleFor(x => x.RetentionPolicyId).GreaterThan(0);
    }
}

public class DestroyRequestValidator : AbstractValidator<DestroyRequest>
{
    public DestroyRequestValidator()
    {
        RuleFor(x => x.DestructionMethod).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class MarkCodedRequestValidator : AbstractValidator<MarkCodedRequest>
{
    public MarkCodedRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
