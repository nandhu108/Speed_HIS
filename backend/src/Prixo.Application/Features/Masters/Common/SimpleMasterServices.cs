using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Application.Features.Masters.Common;

// Concrete simple-master services. Each adds only its factory plus an in-use
// delete guard where other tables reference the master; everything else comes
// from the generic framework.

public class SpecialtyService(IAppDbContext db) : SimpleMasterService<Specialty>(db)
{
    protected override string DisplayName => "Specialty";
    protected override Specialty NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Doctors.AnyAsync(d => d.SpecialtyId == id, ct)) ThrowInUse("doctors");
    }
}

public class ServiceCategoryService(IAppDbContext db) : SimpleMasterService<ServiceCategory>(db)
{
    protected override string DisplayName => "Service category";
    protected override ServiceCategory NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Services.AnyAsync(s => s.ServiceCategoryId == id, ct)) ThrowInUse("services");
    }
}

public class ItemCategoryService(IAppDbContext db) : SimpleMasterService<ItemCategory>(db)
{
    protected override string DisplayName => "Item category";
    protected override ItemCategory NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Items.AnyAsync(i => i.ItemCategoryId == id, ct)) ThrowInUse("items");
    }
}

public class GenericService(IAppDbContext db) : SimpleMasterService<Generic>(db)
{
    protected override string DisplayName => "Generic";
    protected override Generic NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Drugs.AnyAsync(d => d.GenericId == id, ct)) ThrowInUse("drugs");
    }
}

public class ManufacturerService(IAppDbContext db) : SimpleMasterService<Manufacturer>(db)
{
    protected override string DisplayName => "Manufacturer";
    protected override Manufacturer NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Drugs.AnyAsync(d => d.ManufacturerId == id, ct)) ThrowInUse("drugs");
    }
}

public class UomService(IAppDbContext db) : SimpleMasterService<Uom>(db)
{
    protected override string DisplayName => "Unit of measure";
    protected override Uom NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Items.AnyAsync(i => i.UomId == id, ct)) ThrowInUse("items");
        if (await Db.Drugs.AnyAsync(d => d.UomId == id, ct)) ThrowInUse("drugs");
    }
}

public class SampleTypeService(IAppDbContext db) : SimpleMasterService<SampleType>(db)
{
    protected override string DisplayName => "Sample type";
    protected override SampleType NewEntity(string code, string name) => new() { Code = code, Name = name };
}

public class ModalityService(IAppDbContext db) : SimpleMasterService<Modality>(db)
{
    protected override string DisplayName => "Modality";
    protected override Modality NewEntity(string code, string name) => new() { Code = code, Name = name };
}

public class RecordDocumentTypeService(IAppDbContext db) : SimpleMasterService<RecordDocumentType>(db)
{
    protected override string DisplayName => "Document type";
    protected override RecordDocumentType NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.ScannedDocuments.AnyAsync(d => d.DocumentTypeId == id, ct)) ThrowInUse("scanned documents");
    }
}

// Front Office reference-data masters — no other table references these yet, so no delete guard.

public class CampService(IAppDbContext db) : SimpleMasterService<Camp>(db)
{
    protected override string DisplayName => "Camp";
    protected override Camp NewEntity(string code, string name) => new() { Code = code, Name = name };
}

public class StateService(IAppDbContext db) : SimpleMasterService<State>(db)
{
    protected override string DisplayName => "State";
    protected override State NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Patients.AnyAsync(p => p.StateId == id, ct)) ThrowInUse("patients");
    }
}

public class CityService(IAppDbContext db) : SimpleMasterService<City>(db)
{
    protected override string DisplayName => "City";
    protected override City NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Areas.AnyAsync(a => a.CityId == id, ct)) ThrowInUse("areas");
        if (await Db.Patients.AnyAsync(p => p.CityId == id, ct)) ThrowInUse("patients");
    }
}

public class NationalityService(IAppDbContext db) : SimpleMasterService<Nationality>(db)
{
    protected override string DisplayName => "Nationality";
    protected override Nationality NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Patients.AnyAsync(p => p.NationalityId == id || p.CountryId == id, ct)) ThrowInUse("patients");
    }
}

public class ReligionService(IAppDbContext db) : SimpleMasterService<Religion>(db)
{
    protected override string DisplayName => "Religion";
    protected override Religion NewEntity(string code, string name) => new() { Code = code, Name = name };
}

public class LanguageService(IAppDbContext db) : SimpleMasterService<Language>(db)
{
    protected override string DisplayName => "Language";
    protected override Language NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Patients.AnyAsync(p => p.LanguageId == id, ct)) ThrowInUse("patients");
    }
}

public class SpeciallyAbledCategoryService(IAppDbContext db) : SimpleMasterService<SpeciallyAbledCategory>(db)
{
    protected override string DisplayName => "Specially abled category";
    protected override SpeciallyAbledCategory NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Patients.AnyAsync(p => p.SpeciallyAbledCategoryId == id, ct)) ThrowInUse("patients");
    }
}

public class PatientHcTagService(IAppDbContext db) : SimpleMasterService<PatientHcTag>(db)
{
    protected override string DisplayName => "Patient HC tag";
    protected override PatientHcTag NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Patients.AnyAsync(p => p.PatientHcTagId == id, ct)) ThrowInUse("patients");
    }
}

public class PatientCaseTypeService(IAppDbContext db) : SimpleMasterService<PatientCaseType>(db)
{
    protected override string DisplayName => "Patient case type";
    protected override PatientCaseType NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Patients.AnyAsync(p => p.PatientCaseTypeId == id, ct)) ThrowInUse("patients");
    }
}

public class MlcTypeService(IAppDbContext db) : SimpleMasterService<MlcType>(db)
{
    protected override string DisplayName => "MLC type";
    protected override MlcType NewEntity(string code, string name) => new() { Code = code, Name = name };
}

public class MhcDoctorService(IAppDbContext db) : SimpleMasterService<MhcDoctor>(db)
{
    protected override string DisplayName => "MHC doctor";
    protected override MhcDoctor NewEntity(string code, string name) => new() { Code = code, Name = name };
}

public class LoyaltyProgramService(IAppDbContext db) : SimpleMasterService<LoyaltyProgram>(db)
{
    protected override string DisplayName => "Loyalty program";
    protected override LoyaltyProgram NewEntity(string code, string name) => new() { Code = code, Name = name };
}

public class UnitService(IAppDbContext db) : SimpleMasterService<Unit>(db)
{
    protected override string DisplayName => "Unit";
    protected override Unit NewEntity(string code, string name) => new() { Code = code, Name = name };
}

public class LoyaltyCardCategoryService(IAppDbContext db) : SimpleMasterService<LoyaltyCardCategory>(db)
{
    protected override string DisplayName => "Loyalty card category";
    protected override LoyaltyCardCategory NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.LoyaltyCardTypes.AnyAsync(t => t.LoyaltyCardCategoryId == id, ct)) ThrowInUse("loyalty card types");
    }
}

public class MemberRelationTagService(IAppDbContext db) : SimpleMasterService<MemberRelationTag>(db)
{
    protected override string DisplayName => "Member relation tag";
    protected override MemberRelationTag NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.LoyaltyCardMembers.AnyAsync(m => m.MemberRelationTagId == id, ct)) ThrowInUse("loyalty card members");
    }
}

public class SalutationService(IAppDbContext db) : SimpleMasterService<Salutation>(db)
{
    protected override string DisplayName => "Salutation";
    protected override Salutation NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Patients.AnyAsync(p => p.SalutationId == id, ct)) ThrowInUse("patients");
    }
}

public class MaritalStatusService(IAppDbContext db) : SimpleMasterService<MaritalStatus>(db)
{
    protected override string DisplayName => "Marital status";
    protected override MaritalStatus NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Patients.AnyAsync(p => p.MaritalStatusId == id, ct)) ThrowInUse("patients");
    }
}

public class IdentityDocumentTypeService(IAppDbContext db) : SimpleMasterService<IdentityDocumentType>(db)
{
    protected override string DisplayName => "Identity document type";
    protected override IdentityDocumentType NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.PatientIdentifiers.AnyAsync(i => i.IdentityDocumentTypeId == id, ct)) ThrowInUse("patient identifiers");
    }
}

public class VisitClassService(IAppDbContext db) : SimpleMasterService<VisitClass>(db)
{
    protected override string DisplayName => "Visit class";
    protected override VisitClass NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.OpVisits.AnyAsync(v => v.VisitClassId == id, ct)) ThrowInUse("OP visits");
    }
}

public class VisitSessionService(IAppDbContext db) : SimpleMasterService<VisitSession>(db)
{
    protected override string DisplayName => "Visit session";
    protected override VisitSession NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.OpVisits.AnyAsync(v => v.SessionId == id, ct)) ThrowInUse("OP visits");
    }
}

public class ClaimDocumentTypeService(IAppDbContext db) : SimpleMasterService<ClaimDocumentType>(db)
{
    protected override string DisplayName => "Claim document type";
    protected override ClaimDocumentType NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.ClaimDocuments.AnyAsync(d => d.DocumentTypeId == id, ct)) ThrowInUse("claim documents");
    }
}

public class ClaimDenialReasonService(IAppDbContext db) : SimpleMasterService<ClaimDenialReason>(db)
{
    protected override string DisplayName => "Claim denial reason";
    protected override ClaimDenialReason NewEntity(string code, string name) => new() { Code = code, Name = name };

    protected override async Task EnsureDeletableAsync(long id, CancellationToken ct)
    {
        if (await Db.Claims.AnyAsync(c => c.DenialReasonId == id, ct)) ThrowInUse("claims");
    }
}
