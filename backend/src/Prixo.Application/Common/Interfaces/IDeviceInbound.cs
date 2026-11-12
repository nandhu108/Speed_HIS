namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// Placeholder for bedside device integration (monitors, ventilators, infusion pumps
/// pushing readings automatically), per plan §15 Phase 15: "Device Integration (placeholder
/// only — device_reading table + IDeviceInbound interface)". No implementation exists and
/// nothing depends on this yet — ventilator/infusion charting is manual (IcuChartService).
/// Same posture as Lab's IAnalyzerInbound and EMR's IVirusScanner: the shape here is a best
/// guess at what a listener would need to hand off, so a future device-interface phase has
/// an integration point to implement against instead of touching IcuChartService's surface.
/// </summary>
public interface IDeviceInbound
{
    Task HandleReadingAsync(string rawMessage, CancellationToken ct = default);
}
