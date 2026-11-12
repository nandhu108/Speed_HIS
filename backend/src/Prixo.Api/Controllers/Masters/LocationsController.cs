using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Locations;

namespace Prixo.Api.Controllers.Masters;

/// <summary>
/// Building → floor → ward → room → bed hierarchy (plus room types), managed on
/// one screen so it lives on one controller: /api/v1/locations/{level}.
/// </summary>
[Route("api/v1/locations")]
public class LocationsController(LocationService locations) : ApiControllerBase
{
    // ── Buildings ──────────────────────────────────────────────────────────

    [HttpGet("buildings")]
    [HasPermission(PermissionRegistry.LocationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BuildingDto>>>> GetBuildings(CancellationToken ct) =>
        OkResponse(await locations.GetBuildingsAsync(ct));

    [HttpPost("buildings")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateBuilding(SaveBuildingRequest request, CancellationToken ct)
    {
        var id = await locations.CreateBuildingAsync(request, ct);
        return CreatedResponse($"/api/v1/locations/buildings/{id}", id, "Building created.");
    }

    [HttpPut("buildings/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> UpdateBuilding(long id, SaveBuildingRequest request, CancellationToken ct)
    {
        await locations.UpdateBuildingAsync(id, request, ct);
        return OkResponse<object?>(null, "Building updated.");
    }

    [HttpDelete("buildings/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> DeleteBuilding(long id, CancellationToken ct)
    {
        await locations.DeleteBuildingAsync(id, ct);
        return OkResponse<object?>(null, "Building deleted.");
    }

    // ── Floors ─────────────────────────────────────────────────────────────

    [HttpGet("floors")]
    [HasPermission(PermissionRegistry.LocationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FloorDto>>>> GetFloors(CancellationToken ct) =>
        OkResponse(await locations.GetFloorsAsync(ct));

    [HttpPost("floors")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateFloor(SaveFloorRequest request, CancellationToken ct)
    {
        var id = await locations.CreateFloorAsync(request, ct);
        return CreatedResponse($"/api/v1/locations/floors/{id}", id, "Floor created.");
    }

    [HttpPut("floors/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> UpdateFloor(long id, SaveFloorRequest request, CancellationToken ct)
    {
        await locations.UpdateFloorAsync(id, request, ct);
        return OkResponse<object?>(null, "Floor updated.");
    }

    [HttpDelete("floors/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> DeleteFloor(long id, CancellationToken ct)
    {
        await locations.DeleteFloorAsync(id, ct);
        return OkResponse<object?>(null, "Floor deleted.");
    }

    // ── Room types ─────────────────────────────────────────────────────────

    [HttpGet("room-types")]
    [HasPermission(PermissionRegistry.LocationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomTypeDto>>>> GetRoomTypes(CancellationToken ct) =>
        OkResponse(await locations.GetRoomTypesAsync(ct));

    [HttpPost("room-types")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateRoomType(SaveRoomTypeRequest request, CancellationToken ct)
    {
        var id = await locations.CreateRoomTypeAsync(request, ct);
        return CreatedResponse($"/api/v1/locations/room-types/{id}", id, "Room type created.");
    }

    [HttpPut("room-types/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> UpdateRoomType(long id, SaveRoomTypeRequest request, CancellationToken ct)
    {
        await locations.UpdateRoomTypeAsync(id, request, ct);
        return OkResponse<object?>(null, "Room type updated.");
    }

    [HttpDelete("room-types/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> DeleteRoomType(long id, CancellationToken ct)
    {
        await locations.DeleteRoomTypeAsync(id, ct);
        return OkResponse<object?>(null, "Room type deleted.");
    }

    // ── Room type charges (date-effective bed-charge revisions) ──────────────

    [HttpGet("room-types/{roomTypeId:long}/charges")]
    [HasPermission(PermissionRegistry.LocationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomTypeChargeDto>>>> GetRoomTypeCharges(long roomTypeId, CancellationToken ct) =>
        OkResponse(await locations.GetRoomTypeChargesAsync(roomTypeId, ct));

    [HttpPost("room-types/{roomTypeId:long}/charges")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddRoomTypeCharge(long roomTypeId, SaveRoomTypeChargeRequest request, CancellationToken ct)
    {
        var id = await locations.AddRoomTypeChargeAsync(roomTypeId, request, ct);
        return CreatedResponse($"/api/v1/locations/room-types/{roomTypeId}/charges/{id}", id, "Room type charge added.");
    }

    [HttpDelete("room-types/{roomTypeId:long}/charges/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> DeleteRoomTypeCharge(long roomTypeId, long id, CancellationToken ct)
    {
        await locations.DeleteRoomTypeChargeAsync(roomTypeId, id, ct);
        return OkResponse<object?>(null, "Room type charge deleted.");
    }

    // ── Wards ──────────────────────────────────────────────────────────────

    [HttpGet("wards")]
    [HasPermission(PermissionRegistry.LocationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WardDto>>>> GetWards(CancellationToken ct) =>
        OkResponse(await locations.GetWardsAsync(ct));

    [HttpPost("wards")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateWard(SaveWardRequest request, CancellationToken ct)
    {
        var id = await locations.CreateWardAsync(request, ct);
        return CreatedResponse($"/api/v1/locations/wards/{id}", id, "Ward created.");
    }

    [HttpPut("wards/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> UpdateWard(long id, SaveWardRequest request, CancellationToken ct)
    {
        await locations.UpdateWardAsync(id, request, ct);
        return OkResponse<object?>(null, "Ward updated.");
    }

    [HttpDelete("wards/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> DeleteWard(long id, CancellationToken ct)
    {
        await locations.DeleteWardAsync(id, ct);
        return OkResponse<object?>(null, "Ward deleted.");
    }

    // ── Rooms ──────────────────────────────────────────────────────────────

    [HttpGet("rooms")]
    [HasPermission(PermissionRegistry.LocationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoomDto>>>> GetRooms(CancellationToken ct) =>
        OkResponse(await locations.GetRoomsAsync(ct));

    [HttpPost("rooms")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateRoom(SaveRoomRequest request, CancellationToken ct)
    {
        var id = await locations.CreateRoomAsync(request, ct);
        return CreatedResponse($"/api/v1/locations/rooms/{id}", id, "Room created.");
    }

    [HttpPut("rooms/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> UpdateRoom(long id, SaveRoomRequest request, CancellationToken ct)
    {
        await locations.UpdateRoomAsync(id, request, ct);
        return OkResponse<object?>(null, "Room updated.");
    }

    [HttpDelete("rooms/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> DeleteRoom(long id, CancellationToken ct)
    {
        await locations.DeleteRoomAsync(id, ct);
        return OkResponse<object?>(null, "Room deleted.");
    }

    // ── Beds ───────────────────────────────────────────────────────────────

    [HttpGet("beds")]
    [HasPermission(PermissionRegistry.LocationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BedDto>>>> GetBeds(
        [FromQuery] long? roomId, CancellationToken ct) =>
        OkResponse(await locations.GetBedsAsync(roomId, ct));

    [HttpPost("beds")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateBed(SaveBedRequest request, CancellationToken ct)
    {
        var id = await locations.CreateBedAsync(request, ct);
        return CreatedResponse($"/api/v1/locations/beds/{id}", id, "Bed created.");
    }

    [HttpPut("beds/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> UpdateBed(long id, SaveBedRequest request, CancellationToken ct)
    {
        await locations.UpdateBedAsync(id, request, ct);
        return OkResponse<object?>(null, "Bed updated.");
    }

    [HttpDelete("beds/{id:long}")]
    [HasPermission(PermissionRegistry.LocationManage)]
    public async Task<ActionResult<ApiResponse>> DeleteBed(long id, CancellationToken ct)
    {
        await locations.DeleteBedAsync(id, ct);
        return OkResponse<object?>(null, "Bed deleted.");
    }
}
