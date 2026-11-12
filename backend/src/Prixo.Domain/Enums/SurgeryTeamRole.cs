namespace Prixo.Domain.Enums;

/// <summary>Roles beyond the booking's primary Surgeon/Anaesthetist (which are direct FKs on SurgeryBooking, not team rows).</summary>
public enum SurgeryTeamRole
{
    AssistantSurgeon = 1,
    AssistantAnaesthetist = 2,
    ScrubNurse = 3,
    CirculatingNurse = 4,
    Other = 5,
}
