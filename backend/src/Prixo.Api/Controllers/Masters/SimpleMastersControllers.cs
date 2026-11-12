using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Common;
using Prixo.Domain.Common;

namespace Prixo.Api.Controllers.Masters;

/// <summary>
/// Uniform CRUD surface of the generic master framework. Derived controllers only
/// pick the route (via class name) and the permission codes on each action.
/// </summary>
public abstract class SimpleMasterControllerBase<TEntity>(SimpleMasterService<TEntity> service)
    : ApiControllerBase
    where TEntity : BaseEntity, ISimpleMaster
{
    protected async Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAllCoreAsync(
        string? search, CancellationToken ct) =>
        OkResponse(await service.GetAllAsync(search, ct));

    protected async Task<ActionResult<ApiResponse<long>>> CreateCoreAsync(
        SaveSimpleMasterRequest request, string route, string message, CancellationToken ct)
    {
        var id = await service.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/{route}/{id}", id, message);
    }

    protected async Task<ActionResult<ApiResponse>> UpdateCoreAsync(
        long id, SaveSimpleMasterRequest request, string message, CancellationToken ct)
    {
        await service.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, message);
    }

    protected async Task<ActionResult<ApiResponse>> DeleteCoreAsync(
        long id, string message, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return OkResponse<object?>(null, message);
    }
}

public class SpecialtiesController(SpecialtyService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Specialty>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.SpecialtyView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.SpecialtyManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "specialties", "Specialty created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.SpecialtyManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Specialty updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.SpecialtyManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Specialty deleted.", ct);
}


public class ServiceCategoriesController(ServiceCategoryService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.ServiceCategory>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ServiceView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.ServiceManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "servicecategories", "Service category created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ServiceManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Service category updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ServiceManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Service category deleted.", ct);
}

public class ItemCategoriesController(ItemCategoryService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.ItemCategory>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ItemView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.ItemManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "itemcategories", "Item category created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ItemManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Item category updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ItemManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Item category deleted.", ct);
}

public class GenericsController(GenericService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Generic>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DrugView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.DrugManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "generics", "Generic created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.DrugManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Generic updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.DrugManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Generic deleted.", ct);
}

public class ManufacturersController(ManufacturerService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Manufacturer>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DrugView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.DrugManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "manufacturers", "Manufacturer created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.DrugManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Manufacturer updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.DrugManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Manufacturer deleted.", ct);
}

public class UomsController(UomService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Uom>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.UomView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.UomManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "uoms", "Unit of measure created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.UomManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Unit of measure updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.UomManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Unit of measure deleted.", ct);
}

public class SampleTypesController(SampleTypeService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.SampleType>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.SampleTypeView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.SampleTypeManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "sampletypes", "Sample type created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.SampleTypeManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Sample type updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.SampleTypeManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Sample type deleted.", ct);
}

public class ModalitiesController(ModalityService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Modality>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ModalityView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.ModalityManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "modalities", "Modality created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ModalityManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Modality updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ModalityManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Modality deleted.", ct);
}

[Route("api/v1/document-types")]
public class RecordDocumentTypesController(RecordDocumentTypeService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.RecordDocumentType>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.RecordDocumentTypeView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.RecordDocumentTypeManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "document-types", "Document type created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.RecordDocumentTypeManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Document type updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.RecordDocumentTypeManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Document type deleted.", ct);
}

public class CampsController(CampService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Camp>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.CampView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.CampManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "camps", "Camp created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.CampManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Camp updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.CampManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Camp deleted.", ct);
}

public class StatesController(StateService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.State>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.StateView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.StateManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "states", "State created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.StateManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "State updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.StateManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "State deleted.", ct);
}

public class CitiesController(CityService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.City>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.CityView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.CityManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "cities", "City created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.CityManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "City updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.CityManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "City deleted.", ct);
}

public class NationalitiesController(NationalityService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Nationality>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.NationalityView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.NationalityManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "nationalities", "Nationality created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.NationalityManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Nationality updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.NationalityManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Nationality deleted.", ct);
}

public class ReligionsController(ReligionService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Religion>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ReligionView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.ReligionManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "religions", "Religion created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ReligionManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Religion updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ReligionManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Religion deleted.", ct);
}

public class LanguagesController(LanguageService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Language>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LanguageView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.LanguageManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "languages", "Language created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.LanguageManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Language updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.LanguageManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Language deleted.", ct);
}

[Route("api/v1/specially-abled-categories")]
public class SpeciallyAbledCategoriesController(SpeciallyAbledCategoryService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.SpeciallyAbledCategory>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.SpeciallyAbledCategoryView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.SpeciallyAbledCategoryManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "specially-abled-categories", "Specially-abled category created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.SpeciallyAbledCategoryManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Specially-abled category updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.SpeciallyAbledCategoryManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Specially-abled category deleted.", ct);
}

[Route("api/v1/patient-hc-tags")]
public class PatientHcTagsController(PatientHcTagService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.PatientHcTag>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PatientHcTagView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.PatientHcTagManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "patient-hc-tags", "Patient HC tag created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.PatientHcTagManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Patient HC tag updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.PatientHcTagManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Patient HC tag deleted.", ct);
}

[Route("api/v1/patient-case-types")]
public class PatientCaseTypesController(PatientCaseTypeService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.PatientCaseType>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PatientCaseTypeView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.PatientCaseTypeManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "patient-case-types", "Patient case type created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.PatientCaseTypeManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Patient case type updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.PatientCaseTypeManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Patient case type deleted.", ct);
}

[Route("api/v1/mlc-types")]
public class MlcTypesController(MlcTypeService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.MlcType>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MlcTypeView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.MlcTypeManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "mlc-types", "MLC type created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.MlcTypeManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "MLC type updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.MlcTypeManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "MLC type deleted.", ct);
}

[Route("api/v1/mhc-doctors")]
public class MhcDoctorsController(MhcDoctorService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.MhcDoctor>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MhcDoctorView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.MhcDoctorManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "mhc-doctors", "MHC doctor created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.MhcDoctorManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "MHC doctor updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.MhcDoctorManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "MHC doctor deleted.", ct);
}

[Route("api/v1/loyalty-programs")]
public class LoyaltyProgramsController(LoyaltyProgramService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.LoyaltyProgram>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LoyaltyProgramView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.LoyaltyProgramManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "loyalty-programs", "Loyalty program created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyProgramManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Loyalty program updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyProgramManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Loyalty program deleted.", ct);
}

public class UnitsController(UnitService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Unit>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.UnitView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.UnitManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "units", "Unit created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.UnitManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Unit updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.UnitManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Unit deleted.", ct);
}

[Route("api/v1/loyalty-card-categories")]
public class LoyaltyCardCategoriesController(LoyaltyCardCategoryService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.LoyaltyCardCategory>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LoyaltyCardCategoryView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.LoyaltyCardCategoryManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "loyalty-card-categories", "Loyalty card category created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardCategoryManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Loyalty card category updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardCategoryManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Loyalty card category deleted.", ct);
}

[Route("api/v1/member-relation-tags")]
public class MemberRelationTagsController(MemberRelationTagService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.MemberRelationTag>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MemberRelationTagView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.MemberRelationTagManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "member-relation-tags", "Member relation tag created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.MemberRelationTagManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Member relation tag updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.MemberRelationTagManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Member relation tag deleted.", ct);
}

public class SalutationsController(SalutationService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.Salutation>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.SalutationView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.SalutationManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "salutations", "Salutation created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.SalutationManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Salutation updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.SalutationManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Salutation deleted.", ct);
}

[Route("api/v1/marital-statuses")]
public class MaritalStatusesController(MaritalStatusService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.MaritalStatus>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MaritalStatusView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.MaritalStatusManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "marital-statuses", "Marital status created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.MaritalStatusManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Marital status updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.MaritalStatusManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Marital status deleted.", ct);
}

[Route("api/v1/identity-document-types")]
public class IdentityDocumentTypesController(IdentityDocumentTypeService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.IdentityDocumentType>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.IdentityDocumentTypeView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.IdentityDocumentTypeManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "identity-document-types", "Identity document type created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.IdentityDocumentTypeManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Identity document type updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.IdentityDocumentTypeManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Identity document type deleted.", ct);
}

[Route("api/v1/visit-classes")]
public class VisitClassesController(VisitClassService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.VisitClass>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.VisitClassView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.VisitClassManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "visit-classes", "Visit class created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.VisitClassManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Visit class updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.VisitClassManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Visit class deleted.", ct);
}

[Route("api/v1/visit-sessions")]
public class VisitSessionsController(VisitSessionService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.VisitSession>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.VisitSessionView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.VisitSessionManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "visit-sessions", "Visit session created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.VisitSessionManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Visit session updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.VisitSessionManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Visit session deleted.", ct);
}

[Route("api/v1/claim-document-types")]
public class ClaimDocumentTypesController(ClaimDocumentTypeService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.ClaimDocumentType>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ClaimDocumentTypeView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.ClaimDocumentTypeManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "claim-document-types", "Claim document type created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ClaimDocumentTypeManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Claim document type updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ClaimDocumentTypeManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Claim document type deleted.", ct);
}

[Route("api/v1/claim-denial-reasons")]
public class ClaimDenialReasonsController(ClaimDenialReasonService service)
    : SimpleMasterControllerBase<Domain.Entities.Masters.ClaimDenialReason>(service)
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ClaimDenialReasonView)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<SimpleMasterDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) => GetAllCoreAsync(search, ct);

    [HttpPost]
    [HasPermission(PermissionRegistry.ClaimDenialReasonManage)]
    public Task<ActionResult<ApiResponse<long>>> Create(SaveSimpleMasterRequest request, CancellationToken ct) =>
        CreateCoreAsync(request, "claim-denial-reasons", "Claim denial reason created.", ct);

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ClaimDenialReasonManage)]
    public Task<ActionResult<ApiResponse>> Update(long id, SaveSimpleMasterRequest request, CancellationToken ct) =>
        UpdateCoreAsync(id, request, "Claim denial reason updated.", ct);

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ClaimDenialReasonManage)]
    public Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct) =>
        DeleteCoreAsync(id, "Claim denial reason deleted.", ct);
}
