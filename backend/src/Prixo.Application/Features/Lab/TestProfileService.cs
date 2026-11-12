using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Lab;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Lab;

/// <summary>Orderable bundle of tests (e.g. "Liver Function Test"). Global — same shape as Billing's Package/PackageItem.</summary>
public class TestProfileService(IAppDbContext db)
{
    public async Task<IReadOnlyList<TestProfileListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.TestProfiles.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new TestProfileListItemDto(p.Id, p.Code, p.Name, p.Price, p.Tests.Count, p.IsActive))
            .ToListAsync(ct);

    public async Task<TestProfileDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var profile = await db.TestProfiles.AsNoTracking()
            .Include(p => p.Tests).ThenInclude(t => t.LabTest)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(TestProfile), id);
        return ToDetailDto(profile);
    }

    public async Task<long> CreateAsync(SaveTestProfileRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var profile = new TestProfile
        {
            Code = request.Code,
            Name = request.Name,
            Price = request.Price,
            IsActive = request.IsActive,
            Tests = request.LabTestIds.Select(id => new ProfileTest { LabTestId = id }).ToList(),
        };
        db.TestProfiles.Add(profile);
        await db.SaveChangesAsync(ct);
        return profile.Id;
    }

    public async Task UpdateAsync(long id, SaveTestProfileRequest request, CancellationToken ct = default)
    {
        var profile = await db.TestProfiles.Include(p => p.Tests)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(TestProfile), id);
        await EnsureReferencesAsync(request, id, ct);

        profile.Code = request.Code;
        profile.Name = request.Name;
        profile.Price = request.Price;
        profile.IsActive = request.IsActive;

        var desired = request.LabTestIds.ToHashSet();
        foreach (var row in profile.Tests.Where(t => !desired.Contains(t.LabTestId)).ToList())
        {
            db.ProfileTests.Remove(row);
            profile.Tests.Remove(row);
        }
        foreach (var labTestId in desired.Where(id => profile.Tests.All(t => t.LabTestId != id)))
        {
            profile.Tests.Add(new ProfileTest { TestProfileId = id, LabTestId = labTestId });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var profile = await db.TestProfiles.Include(p => p.Tests).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(TestProfile), id);
        if (await db.LabOrderItems.AnyAsync(i => i.TestProfileId == id, ct))
        {
            throw new DomainException("This profile has already been ordered and cannot be deleted.");
        }

        foreach (var test in profile.Tests)
        {
            db.ProfileTests.Remove(test);
        }
        db.TestProfiles.Remove(profile); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveTestProfileRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.TestProfiles.AnyAsync(p => p.Code == request.Code && p.Id != selfId, ct))
        {
            throw new DomainException($"Test profile code '{request.Code}' already exists.");
        }
        var testIds = request.LabTestIds.Distinct().ToList();
        var validTests = await db.LabTests.CountAsync(t => testIds.Contains(t.Id), ct);
        if (validTests != testIds.Count)
        {
            throw new DomainException("One or more lab tests do not exist.");
        }
    }

    private static TestProfileDetailDto ToDetailDto(TestProfile p) => new(
        p.Id, p.Code, p.Name, p.Price, p.IsActive,
        p.Tests.OrderBy(t => t.LabTest!.Name).Select(t => new TestProfileMemberDto(t.LabTestId, t.LabTest!.Name)).ToList());
}
