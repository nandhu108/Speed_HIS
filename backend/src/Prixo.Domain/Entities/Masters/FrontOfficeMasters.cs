using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

// Front Office reference-data masters (patient demographics + camp/MLC/loyalty tagging).
// All global, all simple code+name masters on the generic SimpleMasterService framework.

public class Camp : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class State : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class City : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class Nationality : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class Religion : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class Language : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class SpeciallyAbledCategory : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class PatientHcTag : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class PatientCaseType : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Medico-legal case type tag (RTA, Assault, Poisoning...). Not yet linked to Er.MlcRecord.</summary>
public class MlcType : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Doctor tagging list for Master Health Checkup / camp panels.</summary>
public class MhcDoctor : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    /// <summary>Days after an MHC visit within which a repeat registration counts as Review — mirrors Doctor.FollowUpWindowDays.</summary>
    public int FollowUpWindowDays { get; set; } = 14;
}

/// <summary>MHC checkup package (name + fixed price). Not ISimpleMaster — carries Price beyond Code/Name/Description, so it uses a bespoke service/controller like Area.</summary>
public class MhcPackage : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
}

/// <summary>Loyalty program/tier name — schema only; the earn/redeem ledger is a separate, later ask.</summary>
public class LoyaltyProgram : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Hospital sub-unit/wing, distinct from Department.</summary>
public class Unit : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Doctor linked to a Unit ("Link Unit &amp; Doctors") — bare join, mirrors DoctorDepartment.</summary>
public class UnitDoctor : BaseEntity
{
    public long UnitId { get; set; }
    public Unit? Unit { get; set; }
    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
}

/// <summary>Configurable appointment-type tag ("Appointment Masters") — Booked/WalkIn stays a
/// fixed AppointmentBookingType enum; this is an optional, unwired categorization on top,
/// same additive-tag posture as OpVisit.CampId. Not ISimpleMaster since it carries DefaultSlotMinutes.</summary>
public class AppointmentType : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DefaultSlotMinutes { get; set; } = 10;
}

/// <summary>Area within a City — not a flat ISimpleMaster since it carries the CityId FK.</summary>
public class Area : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long CityId { get; set; }
    public City? City { get; set; }
}

/// <summary>Name prefix (Mr/Mrs/Ms/Dr/Master/Baby...) — registration-screen gap-fill (Phase 24).</summary>
public class Salutation : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Marital status tag (Single/Married/Divorced/Widowed...) — registration-screen gap-fill (Phase 24).</summary>
public class MaritalStatus : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Generalized identity-document type (PAN/Passport/Voter ID/Driving License/Ration Card/Other),
/// used when PatientIdentifier.Type == Other — Aadhaar/Abha keep their own dedicated enum values since
/// those two carry verify-placeholder behavior. Registration-screen gap-fill (Phase 24).</summary>
public class IdentityDocumentType : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>OP registration "Class" tag (Regular/Express/Premium...) — purely descriptive, not
/// consumed by the slot/queue engine, same additive posture as AppointmentType. Phase 24.</summary>
public class VisitClass : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>OP registration "Session" tag (Morning/Afternoon/Evening) — purely descriptive,
/// same additive posture as VisitClass/AppointmentType. Phase 24.</summary>
public class VisitSession : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Claim supporting-document type (Pre-Auth Form/Discharge Summary/Bill Copy/Policy Copy...).
/// Insurance/TPA module (Phase 24).</summary>
public class ClaimDocumentType : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Claim denial reason code (Pre-existing exclusion/Non-payable item/Documentation
/// incomplete/Policy lapsed...). Insurance/TPA module (Phase 24).</summary>
public class ClaimDenialReason : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
