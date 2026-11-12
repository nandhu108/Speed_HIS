namespace Prixo.Domain.Enums;

/// <summary>Referral here means transferred out to another facility, not the admission-side referral source.</summary>
public enum DischargeType
{
    Normal = 1,
    Lama = 2,
    Dama = 3,
    Death = 4,
    Referral = 5,
}
