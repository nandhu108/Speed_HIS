namespace Prixo.Domain.Enums;

/// <summary>Terminal Closed state. Phase 20 (Quality/NABH) widened Incident in place with RCA/sentinel fields rather than forking this state machine — see Incident's own doc comment.</summary>
public enum IncidentStatus
{
    Reported = 1,
    UnderReview = 2,
    Closed = 3,
}
