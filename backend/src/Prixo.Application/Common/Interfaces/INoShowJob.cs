namespace Prixo.Application.Common.Interfaces;

/// <summary>Recurring Hangfire job: auto-marks past-due Scheduled appointments as NoShow.</summary>
public interface INoShowJob
{
    Task RunAsync(CancellationToken ct = default);
}
