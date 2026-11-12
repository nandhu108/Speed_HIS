namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// Placeholder for bidirectional lab analyzer integration (ASTM/HL7 listener → worklist →
/// result auto-population), per plan §11 Phase 7: "Analyzer Integration (placeholder —
/// interface IAnalyzerInbound defined, held)". No implementation exists and nothing
/// depends on this yet — result entry is manual (LabOrderService.EnterResultAsync). The
/// shape here is a best guess at what a listener would need to hand off; it exists so a
/// future analyzer-interface phase has an integration point to implement against instead
/// of having to touch LabOrderService's public surface.
/// </summary>
public interface IAnalyzerInbound
{
    Task HandleResultMessageAsync(string rawMessage, CancellationToken ct = default);
}
