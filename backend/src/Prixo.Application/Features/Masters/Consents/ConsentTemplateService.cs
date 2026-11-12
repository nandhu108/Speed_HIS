using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Consents;

public record ConsentTemplateDto(long Id, string Code, string Title, string BodyText, bool IsActive);
public record SaveConsentTemplateRequest(string Code, string Title, string BodyText, bool IsActive = true);

public class SaveConsentTemplateRequestValidator : AbstractValidator<SaveConsentTemplateRequest>
{
    public SaveConsentTemplateRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyText).NotEmpty().MaximumLength(8000);
    }
}

/// <summary>Reusable consent text signed at registration or before a procedure (global).</summary>
public class ConsentTemplateService(IAppDbContext db)
{
    public async Task<IReadOnlyList<ConsentTemplateDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.ConsentTemplates.AsNoTracking()
            .OrderBy(c => c.Title)
            .Select(c => new ConsentTemplateDto(c.Id, c.Code, c.Title, c.BodyText, c.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveConsentTemplateRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var template = new ConsentTemplate
        {
            Code = request.Code,
            Title = request.Title,
            BodyText = request.BodyText,
            IsActive = request.IsActive,
        };
        db.ConsentTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }

    public async Task UpdateAsync(long id, SaveConsentTemplateRequest request, CancellationToken ct = default)
    {
        var template = await db.ConsentTemplates.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(ConsentTemplate), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        template.Code = request.Code;
        template.Title = request.Title;
        template.BodyText = request.BodyText;
        template.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var template = await db.ConsentTemplates.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(ConsentTemplate), id);
        if (await db.PatientConsents.AnyAsync(pc => pc.ConsentTemplateId == id, ct))
        {
            throw new DomainException("Consent template has signed consents recorded against it and cannot be deleted.");
        }

        db.ConsentTemplates.Remove(template); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.ConsentTemplates.AnyAsync(c => c.Code == code && c.Id != selfId, ct))
        {
            throw new DomainException($"Consent template code '{code}' already exists.");
        }
    }
}
