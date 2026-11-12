using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Er;

/// <summary>Pure ER queue ordering — no I/O, so EmergencyVisitService delegates here and the rule stays unit-testable without a database.</summary>
public static class ErMath
{
    /// <summary>Lower sorts first. Untriaged (no ESI yet) sorts ahead of every triaged level so the queue surfaces them for triage first — sort ties then break on arrival time.</summary>
    public static int QueuePriority(EsiLevel? esiLevel) => esiLevel is { } level ? (int)level : 0;
}
