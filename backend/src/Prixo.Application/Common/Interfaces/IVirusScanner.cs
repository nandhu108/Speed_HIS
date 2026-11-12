namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// Placeholder for an antivirus scan hook on EMR Attachment uploads, per plan §11
/// ("virus-scan hook placeholder"). No implementation exists and nothing depends on this yet
/// — AttachmentService stores every upload as VirusScanStatus.NotScanned. The shape here is a
/// best guess at what a scanner integration would need, same posture as Lab's
/// IAnalyzerInbound and ICU's IDeviceInbound.
/// </summary>
public interface IVirusScanner
{
    Task<bool> IsCleanAsync(Stream content, string fileName, CancellationToken ct = default);
}
