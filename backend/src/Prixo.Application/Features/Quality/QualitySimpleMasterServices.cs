using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Masters.Common;
using Prixo.Domain.Entities.Quality;

namespace Prixo.Application.Features.Quality;

public class QualityCommitteeService(IAppDbContext db) : SimpleMasterService<QualityCommittee>(db)
{
    protected override string DisplayName => "Committee";
    protected override QualityCommittee NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.QualityCommitteeMeetings.AnyAsync(m => m.CommitteeId == id, ct)) ThrowInUse("committee meetings");
    }
}

public class QualityNabhStandardService(IAppDbContext db) : SimpleMasterService<QualityNabhStandard>(db)
{
    protected override string DisplayName => "NABH standard";
    protected override QualityNabhStandard NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.QualityNonConformances.AnyAsync(n => n.StandardId == id, ct)) ThrowInUse("non-conformances");
        if (await Db.QualityEvidence.AnyAsync(e => e.StandardId == id, ct)) ThrowInUse("evidence uploads");
    }
}
