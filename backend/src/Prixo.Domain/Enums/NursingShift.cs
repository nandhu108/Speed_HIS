namespace Prixo.Domain.Enums;

/// <summary>Ward shift discriminator — shared by NursingNote (shift-tagged) and ShiftHandover (per-shift SBAR handover).</summary>
public enum NursingShift
{
    Morning = 1,
    Evening = 2,
    Night = 3,
}
