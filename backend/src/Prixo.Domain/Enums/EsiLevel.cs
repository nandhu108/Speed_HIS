namespace Prixo.Domain.Enums;

/// <summary>Emergency Severity Index — 1 is most urgent (resuscitation), 5 is least urgent. Drives the ER queue's auto-priority ordering.</summary>
public enum EsiLevel
{
    Resuscitation = 1,
    Emergent = 2,
    Urgent = 3,
    LessUrgent = 4,
    NonUrgent = 5,
}
