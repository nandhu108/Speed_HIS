using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Domain.Entities.Masters;

/// <summary>
/// Doctor profile layered on a login user. Global — one profile works at any mapped
/// branch. Fees are defaults; branch tariffs may override at billing time.
/// </summary>
public class Doctor : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }

    public required string RegistrationNo { get; set; }
    public long? SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
    public string? Qualification { get; set; }

    public decimal ConsultationFee { get; set; }
    public decimal FollowUpFee { get; set; }
    /// <summary>Days after a visit within which a revisit counts as Review (Phase 5 uses this).</summary>
    public int FollowUpWindowDays { get; set; } = 14;
    /// <summary>Schedule default; Phase 4 doctor schedules start from this.</summary>
    public int DefaultSlotMinutes { get; set; } = 10;

    public ICollection<DoctorDepartment> DoctorDepartments { get; set; } = [];
}

public class DoctorDepartment : BaseEntity
{
    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public long DepartmentId { get; set; }
    public Department? Department { get; set; }
}
