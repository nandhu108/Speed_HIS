using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Patients;

namespace Prixo.Application.Features.Insurance;

/// <summary>Read-only lookup used by the Pre-Auth/Claim raise screens to pick a patient's policy — the policies themselves are captured inline on Patient registration/update.</summary>
public class PatientInsurancePolicyService(IAppDbContext db)
{
    public async Task<IReadOnlyList<PatientInsurancePolicyDto>> GetByPatientAsync(
        long patientId, CancellationToken ct = default) =>
        await db.PatientInsurancePolicies.AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .Include(p => p.InsuranceCompany)
            .OrderByDescending(p => p.IsPrimary).ThenBy(p => p.Id)
            .Select(p => new PatientInsurancePolicyDto(
                p.Id, p.InsuranceCompanyId, p.InsuranceCompany!.Name, p.PolicyNumber,
                p.PolicyValidFrom, p.PolicyValidTo, p.IsPrimary))
            .ToListAsync(ct);
}
