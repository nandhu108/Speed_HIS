using Prixo.Domain.Common;
using Prixo.Domain.Entities.Ipd;

namespace Prixo.Domain.Entities.Icu;

/// <summary>
/// Placeholder table for bedside device integration (plan §15: "Device Integration
/// (placeholder only — device_reading table + IDeviceInbound interface)"). Nothing writes
/// to this yet — no listener implementation exists, same posture as Lab's
/// IAnalyzerInbound/Phase 7 and EMR's IVirusScanner/Phase 11. Exists so a future device-
/// integration phase has schema and an interface to implement against instead of having to
/// design the shape from scratch.
/// </summary>
public class DeviceReading : BranchEntity
{
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public required string DeviceType { get; set; }
    public required string ParameterName { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public string? RawPayload { get; set; }
}
