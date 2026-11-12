using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Biomed;

/// <summary>Calibration tracking (plan §19: "due tracking, certificates"). Certificate upload reuses the same on-disk storage idiom as MRD's ScannedDocument.</summary>
public class CalibrationService(IAppDbContext db, IFileStorageService files, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(SaveCalibrationRequest request, Stream? certificate, string? certificateFileName, CancellationToken ct = default)
    {
        var equipment = await db.BiomedEquipment.FirstOrDefaultAsync(e => e.Id == request.EquipmentId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Biomed.BiomedEquipment), request.EquipmentId);
        if (request.NextDueDate < request.CalibrationDate)
        {
            throw new DomainException("Next due date must be on or after the calibration date.");
        }

        string? certPath = null;
        if (certificate is not null && certificateFileName is not null)
        {
            var stored = await files.SaveAsync(certificate, $"biomed-calibrations/{equipment.EquipmentCode}", certificateFileName, ct);
            certPath = stored.RelativePath;
        }

        var calibration = new Calibration
        {
            BranchId = equipment.BranchId, EquipmentId = request.EquipmentId, CalibrationDate = request.CalibrationDate,
            NextDueDate = request.NextDueDate, PerformedBy = request.PerformedBy, CertificateNumber = request.CertificateNumber,
            CertificateFilePath = certPath, Notes = request.Notes,
        };
        db.Calibrations.Add(calibration);
        await db.SaveChangesAsync(ct);
        return calibration.Id;
    }

    public async Task<IReadOnlyList<CalibrationDto>> GetAllAsync(CalibrationQuery query, CancellationToken ct = default)
    {
        var calibrations = db.Calibrations.AsNoTracking().Include(c => c.Equipment).AsQueryable();
        if (query.EquipmentId is { } equipmentId) calibrations = calibrations.Where(c => c.EquipmentId == equipmentId);

        var rows = await calibrations.OrderByDescending(c => c.CalibrationDate).ToListAsync(ct);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dtos = rows.Select(c => new CalibrationDto(c.Id, c.EquipmentId, c.Equipment!.Name, c.CalibrationDate, c.NextDueDate,
            c.PerformedBy, c.CertificateNumber, c.CertificateFilePath != null, BiomedMath.IsPmDue(c.NextDueDate, today), c.Notes));
        if (query.DueOnly) dtos = dtos.Where(d => d.IsDue);
        return dtos.ToList();
    }

    public async Task<(Stream Content, string FileName)> OpenCertificateAsync(long id, CancellationToken ct = default)
    {
        var calibration = await db.Calibrations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException(nameof(Calibration), id);
        if (calibration.CertificateFilePath is null)
        {
            throw new DomainException("This calibration has no certificate on file.");
        }
        var stream = await files.OpenReadAsync(calibration.CertificateFilePath, ct);
        return (stream, calibration.CertificateNumber ?? "certificate");
    }
}
