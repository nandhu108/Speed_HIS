using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Patients.Abdm;

/// <summary>
/// Administration and governance service for Ayushman Bharat Digital Mission (ABDM).
/// Allows hospital administrators to enable/disable the overall ABDM integration or individual
/// milestones (M1: ABHA, M2: HIP, M3: HIU), configure credentials, and inspect live network metrics.
/// </summary>
public class AbdmAdminService(
    IAppDbContext db,
    SettingService settings,
    IAbdmGateway gateway,
    IActivityLogger activity,
    TimeProvider clock)
{
    public async Task<AbdmAdminStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var master = await settings.GetBoolAsync(SettingKeys.AbdmEnabled, true, ct);
        var m1 = await settings.GetBoolAsync(SettingKeys.AbdmM1Enabled, true, ct);
        var m2 = await settings.GetBoolAsync(SettingKeys.AbdmM2Enabled, true, ct);
        var m3 = await settings.GetBoolAsync(SettingKeys.AbdmM3Enabled, true, ct);

        var baseUrl = await settings.GetRawAsync(SettingKeys.AbdmBaseUrl, ct) ?? "https://dev.abdm.gov.in/gateway";
        var clientId = await settings.GetRawAsync(SettingKeys.AbdmClientId, ct) ?? string.Empty;
        var hipId = await settings.GetRawAsync(SettingKeys.AbdmHipId, ct) ?? string.Empty;
        var hiuId = await settings.GetRawAsync(SettingKeys.AbdmHiuId, ct) ?? string.Empty;

        var totalAbha = await db.AbhaProfiles.CountAsync(ct);
        var totalCareContexts = await db.AbdmCareContexts.CountAsync(c => c.IsLinked, ct);
        var totalDataTransfers = await db.AbdmDataTransferLogs.CountAsync(ct);
        var totalConsentRequests = await db.AbdmConsentRequests.CountAsync(ct);
        var totalExternalRecords = await db.AbdmExternalHealthRecords.CountAsync(ct);

        return new AbdmAdminStatusDto(
            master,
            m1,
            m2,
            m3,
            baseUrl,
            clientId,
            hipId,
            hiuId,
            gateway.IsConfigured || !string.IsNullOrWhiteSpace(clientId),
            totalAbha,
            totalCareContexts,
            totalDataTransfers,
            totalConsentRequests,
            totalExternalRecords,
            clock.GetUtcNow());
    }

    public async Task<AbdmAdminStatusDto> UpdateConfigAsync(
        UpdateAbdmAdminConfigRequest request, CancellationToken ct = default)
    {
        await settings.UpdateAsync(SettingKeys.AbdmEnabled, new UpdateSettingRequest(request.MasterEnabled ? "true" : "false"), ct);
        await settings.UpdateAsync(SettingKeys.AbdmM1Enabled, new UpdateSettingRequest(request.M1Enabled ? "true" : "false"), ct);
        await settings.UpdateAsync(SettingKeys.AbdmM2Enabled, new UpdateSettingRequest(request.M2Enabled ? "true" : "false"), ct);
        await settings.UpdateAsync(SettingKeys.AbdmM3Enabled, new UpdateSettingRequest(request.M3Enabled ? "true" : "false"), ct);

        if (request.GatewayUrl != null)
        {
            await settings.UpdateAsync(SettingKeys.AbdmBaseUrl, new UpdateSettingRequest(request.GatewayUrl.Trim()), ct);
        }
        if (request.ClientId != null)
        {
            await settings.UpdateAsync(SettingKeys.AbdmClientId, new UpdateSettingRequest(request.ClientId.Trim()), ct);
        }
        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            await settings.UpdateAsync(SettingKeys.AbdmClientSecret, new UpdateSettingRequest(request.ClientSecret.Trim()), ct);
        }
        if (request.HipId != null)
        {
            await settings.UpdateAsync(SettingKeys.AbdmHipId, new UpdateSettingRequest(request.HipId.Trim()), ct);
        }
        if (request.HiuId != null)
        {
            await settings.UpdateAsync(SettingKeys.AbdmHiuId, new UpdateSettingRequest(request.HiuId.Trim()), ct);
        }

        await activity.LogAsync(
            "ABDM_SETTINGS_UPDATED",
            $"Updated ABDM configuration: Master={request.MasterEnabled}, M1={request.M1Enabled}, M2={request.M2Enabled}, M3={request.M3Enabled}", ct: ct);

        return await GetStatusAsync(ct);
    }

    public async Task<AbdmAdminStatusDto> ToggleMilestoneAsync(
        ToggleMilestoneRequest request, CancellationToken ct = default)
    {
        var (key, label) = request.Milestone.ToLowerInvariant() switch
        {
            "master" => (SettingKeys.AbdmEnabled, "ABDM Master Switch"),
            "m1" => (SettingKeys.AbdmM1Enabled, "ABDM Milestone M1 (ABHA)"),
            "m2" => (SettingKeys.AbdmM2Enabled, "ABDM Milestone M2 (HIP)"),
            "m3" => (SettingKeys.AbdmM3Enabled, "ABDM Milestone M3 (HIU)"),
            _ => throw new DomainException($"Unknown milestone identifier '{request.Milestone}'. Expected 'master', 'm1', 'm2', or 'm3'."),
        };

        var valueStr = request.Enabled ? "true" : "false";
        await settings.UpdateAsync(key, new UpdateSettingRequest(valueStr), ct);

        await activity.LogAsync(
            "ABDM_MILESTONE_TOGGLED",
            $"{label} was {(request.Enabled ? "ENABLED" : "DISABLED")} by administrator", ct: ct);

        return await GetStatusAsync(ct);
    }

    public async Task<AbdmGatewayStatus> TestConnectionAsync(CancellationToken ct = default)
    {
        return await gateway.CheckGatewayHealthAsync(ct);
    }
}
