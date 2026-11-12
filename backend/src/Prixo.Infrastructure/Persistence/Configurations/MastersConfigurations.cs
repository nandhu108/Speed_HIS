using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 2 master tables. Schema: master. Unique indexes are partial
// (WHERE is_deleted = false); branch-scoped masters key uniqueness per branch.
// Money numeric(18,2), quantities numeric(18,3), percentages numeric(5,2).

internal static class MasterConfigurationExtensions
{
    /// <summary>Common shape for global code+name masters: lengths + unique code.</summary>
    public static void ConfigureCodeName<T>(this EntityTypeBuilder<T> builder, string table)
        where T : class
    {
        builder.ToTable(table, "master");
        builder.Property("Code").HasMaxLength(30);
        builder.Property("Name").HasMaxLength(200);
        builder.HasIndex("Code").IsUnique().HasFilter("is_deleted = false");
    }

    /// <summary>Branch-scoped masters: code unique per branch.</summary>
    public static void ConfigureBranchCodeName<T>(this EntityTypeBuilder<T> builder, string table)
        where T : class
    {
        builder.ToTable(table, "master");
        builder.Property("Code").HasMaxLength(30);
        builder.Property("Name").HasMaxLength(200);
        builder.HasIndex("BranchId", "Code").IsUnique().HasFilter("is_deleted = false");
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ConfigureCodeName("departments");
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.Type).HasDefaultValue(DepartmentType.Clinical);
    }
}

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ConfigureCodeName("specialties");
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}

public class PatientCategoryConfiguration : IEntityTypeConfiguration<PatientCategory>
{
    public void Configure(EntityTypeBuilder<PatientCategory> builder)
    {
        builder.ConfigureCodeName("patient_categories");
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.ColorHex).HasMaxLength(7);
    }
}

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("doctors", "master");
        builder.Property(d => d.RegistrationNo).HasMaxLength(50);
        builder.Property(d => d.Qualification).HasMaxLength(300);
        builder.Property(d => d.ConsultationFee).HasPrecision(18, 2);
        builder.Property(d => d.FollowUpFee).HasPrecision(18, 2);
        builder.HasIndex(d => d.UserId).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(d => d.RegistrationNo).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(d => d.User).WithMany()
            .HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Specialty).WithMany()
            .HasForeignKey(d => d.SpecialtyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorDepartmentConfiguration : IEntityTypeConfiguration<DoctorDepartment>
{
    public void Configure(EntityTypeBuilder<DoctorDepartment> builder)
    {
        builder.ToTable("doctor_departments", "master");
        builder.HasIndex(dd => new { dd.DoctorId, dd.DepartmentId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(dd => dd.Doctor).WithMany(d => d.DoctorDepartments)
            .HasForeignKey(dd => dd.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(dd => dd.Department).WithMany()
            .HasForeignKey(dd => dd.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", "master");
        builder.Property(e => e.Code).HasMaxLength(30);
        builder.Property(e => e.FullName).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.HasIndex(e => new { e.BranchId, e.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(e => e.Designation).WithMany()
            .HasForeignKey(e => e.DesignationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Department).WithMany()
            .HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ConfigureCodeName("service_categories");
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ConfigureCodeName("services");
        builder.Property(s => s.DefaultRate).HasPrecision(18, 2);
        builder.HasOne(s => s.Category).WithMany()
            .HasForeignKey(s => s.ServiceCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Department).WithMany()
            .HasForeignKey(s => s.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Tax).WithMany()
            .HasForeignKey(s => s.TaxId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.ConfigureBranchCodeName("tariffs");
        builder.HasOne(t => t.PatientCategory).WithMany()
            .HasForeignKey(t => t.PatientCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_tariffs_effective_range",
            "effective_to IS NULL OR effective_to >= effective_from"));
    }
}

public class TariffItemConfiguration : IEntityTypeConfiguration<TariffItem>
{
    public void Configure(EntityTypeBuilder<TariffItem> builder)
    {
        builder.ToTable("tariff_items", "master");
        builder.Property(i => i.Rate).HasPrecision(18, 2);
        // Postgres treats each NULL as distinct in a unique index, so this still allows one
        // room-agnostic row (RoomTypeId null) plus any number of room-type-specific overrides
        // for the same service — the "no two identical room-agnostic rows" case is caught by
        // SaveTariffRequestValidator's (ServiceId, RoomTypeId) distinct check instead.
        builder.HasIndex(i => new { i.TariffId, i.ServiceId, i.RoomTypeId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(i => i.Tariff).WithMany(t => t.Items)
            .HasForeignKey(i => i.TariffId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Service).WithMany()
            .HasForeignKey(i => i.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.RoomType).WithMany()
            .HasForeignKey(i => i.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_tariff_items_rate", "rate >= 0"));
    }
}

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ConfigureBranchCodeName("packages");
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_packages_price", "price >= 0"));
    }
}

public class PackageItemConfiguration : IEntityTypeConfiguration<PackageItem>
{
    public void Configure(EntityTypeBuilder<PackageItem> builder)
    {
        builder.ToTable("package_items", "master");
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.HasIndex(i => new { i.PackageId, i.ServiceId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(i => i.Package).WithMany(p => p.Items)
            .HasForeignKey(i => i.PackageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Service).WithMany()
            .HasForeignKey(i => i.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_package_items_quantity", "quantity > 0"));
    }
}

public class BillEstimateTemplateConfiguration : IEntityTypeConfiguration<BillEstimateTemplate>
{
    public void Configure(EntityTypeBuilder<BillEstimateTemplate> builder)
    {
        builder.ConfigureBranchCodeName("bill_estimate_templates");
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillEstimateTemplateItemConfiguration : IEntityTypeConfiguration<BillEstimateTemplateItem>
{
    public void Configure(EntityTypeBuilder<BillEstimateTemplateItem> builder)
    {
        builder.ToTable("bill_estimate_template_items", "master");
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.HasIndex(i => i.BillEstimateTemplateId);
        builder.HasOne(i => i.BillEstimateTemplate).WithMany(t => t.Items)
            .HasForeignKey(i => i.BillEstimateTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Service).WithMany()
            .HasForeignKey(i => i.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Package).WithMany()
            .HasForeignKey(i => i.PackageId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_bill_estimate_template_items_quantity", "quantity > 0"));
    }
}

public class DoctorPayoutRuleConfiguration : IEntityTypeConfiguration<DoctorPayoutRule>
{
    public void Configure(EntityTypeBuilder<DoctorPayoutRule> builder)
    {
        builder.ToTable("doctor_payout_rules", "master");
        builder.Property(r => r.Value).HasPrecision(18, 2);
        builder.HasIndex(r => new { r.BranchId, r.DoctorId, r.ServiceCategoryId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(r => r.Doctor).WithMany()
            .HasForeignKey(r => r.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ServiceCategory).WithMany()
            .HasForeignKey(r => r.ServiceCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_doctor_payout_rules_value", "value >= 0"));
    }
}

// Loyalty card program masters (front-office extension — see docs/modules/loyalty.md).

public class LoyaltyCardCategoryConfiguration : IEntityTypeConfiguration<LoyaltyCardCategory>
{
    public void Configure(EntityTypeBuilder<LoyaltyCardCategory> builder)
    {
        builder.ConfigureCodeName("loyalty_card_categories");
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}

public class MemberRelationTagConfiguration : IEntityTypeConfiguration<MemberRelationTag>
{
    public void Configure(EntityTypeBuilder<MemberRelationTag> builder)
    {
        builder.ConfigureCodeName("member_relation_tags");
        builder.Property(t => t.Description).HasMaxLength(500);
    }
}

public class LoyaltyCardTypeConfiguration : IEntityTypeConfiguration<LoyaltyCardType>
{
    public void Configure(EntityTypeBuilder<LoyaltyCardType> builder)
    {
        builder.ConfigureCodeName("loyalty_card_types");
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.HasOne(t => t.LoyaltyCardCategory).WithMany()
            .HasForeignKey(t => t.LoyaltyCardCategoryId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_loyalty_card_types_member_limit", "member_limit >= 0"));
    }
}

public class LoyaltyCardPriceConfiguration : IEntityTypeConfiguration<LoyaltyCardPrice>
{
    public void Configure(EntityTypeBuilder<LoyaltyCardPrice> builder)
    {
        builder.ToTable("loyalty_card_prices", "master");
        builder.HasIndex(p => new { p.BranchId, p.LoyaltyCardTypeId, p.EffectiveFrom }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(p => p.LoyaltyCardType).WithMany()
            .HasForeignKey(p => p.LoyaltyCardTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_loyalty_card_prices_effective_range",
            "effective_to IS NULL OR effective_to >= effective_from"));
    }
}

public class LoyaltyCardPriceItemConfiguration : IEntityTypeConfiguration<LoyaltyCardPriceItem>
{
    public void Configure(EntityTypeBuilder<LoyaltyCardPriceItem> builder)
    {
        builder.ToTable("loyalty_card_price_items", "master");
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.HasIndex(i => new { i.LoyaltyCardPriceId, i.DurationMonths }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(i => i.LoyaltyCardPrice).WithMany(p => p.Items)
            .HasForeignKey(i => i.LoyaltyCardPriceId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_loyalty_card_price_items_amount", "amount >= 0");
            t.HasCheckConstraint("ck_loyalty_card_price_items_duration", "duration_months > 0");
        });
    }
}

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ConfigureBranchCodeName("buildings");
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(b => b.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        builder.ConfigureBranchCodeName("floors");
        builder.HasOne(f => f.Building).WithMany()
            .HasForeignKey(f => f.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(f => f.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.ConfigureBranchCodeName("room_types");
        builder.Property(r => r.BedChargePerDay).HasPrecision(18, 2);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_room_types_bed_charge", "bed_charge_per_day >= 0"));
    }
}

public class RoomTypeChargeConfiguration : IEntityTypeConfiguration<RoomTypeCharge>
{
    public void Configure(EntityTypeBuilder<RoomTypeCharge> builder)
    {
        builder.ToTable("room_type_charges", "master");
        builder.Property(c => c.Rate).HasPrecision(18, 2);
        builder.HasIndex(c => c.RoomTypeId);
        builder.HasOne(c => c.RoomType).WithMany(r => r.Charges)
            .HasForeignKey(c => c.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_room_type_charges_rate", "rate >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_room_type_charges_effective_range",
            "effective_to IS NULL OR effective_to >= effective_from"));
    }
}

public class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ConfigureBranchCodeName("wards");
        builder.HasOne(w => w.Floor).WithMany()
            .HasForeignKey(w => w.FloorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(w => w.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ConfigureBranchCodeName("rooms");
        builder.HasOne(r => r.Ward).WithMany()
            .HasForeignKey(r => r.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.RoomType).WithMany()
            .HasForeignKey(r => r.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.ToTable("beds", "master");
        builder.Property(b => b.Code).HasMaxLength(30);
        builder.HasIndex(b => new { b.BranchId, b.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(b => b.Room).WithMany()
            .HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(b => b.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ConfigureBranchCodeName("vendors");
        builder.Property(v => v.ContactPerson).HasMaxLength(200);
        builder.Property(v => v.Phone).HasMaxLength(20);
        builder.Property(v => v.Email).HasMaxLength(200);
        builder.Property(v => v.Address).HasMaxLength(500);
        builder.Property(v => v.GstNumber).HasMaxLength(20);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(v => v.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        builder.ConfigureCodeName("item_categories");
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ConfigureCodeName("items");
        builder.Property(i => i.HsnCode).HasMaxLength(20);
        builder.Property(i => i.ReorderLevel).HasPrecision(18, 3);
        builder.HasOne(i => i.Category).WithMany()
            .HasForeignKey(i => i.ItemCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Uom).WithMany()
            .HasForeignKey(i => i.UomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Tax).WithMany()
            .HasForeignKey(i => i.TaxId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GenericConfiguration : IEntityTypeConfiguration<Generic>
{
    public void Configure(EntityTypeBuilder<Generic> builder)
    {
        builder.ConfigureCodeName("generics");
        builder.Property(g => g.Description).HasMaxLength(500);
    }
}

public class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ConfigureCodeName("manufacturers");
        builder.Property(m => m.Description).HasMaxLength(500);
    }
}

public class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        builder.ConfigureCodeName("drugs");
        builder.Property(d => d.Form).HasMaxLength(50);
        builder.Property(d => d.Strength).HasMaxLength(50);
        builder.Property(d => d.HsnCode).HasMaxLength(20);
        builder.Property(d => d.Schedule).HasMaxLength(10);
        builder.HasOne(d => d.Generic).WithMany()
            .HasForeignKey(d => d.GenericId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Manufacturer).WithMany()
            .HasForeignKey(d => d.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Uom).WithMany()
            .HasForeignKey(d => d.UomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Tax).WithMany()
            .HasForeignKey(d => d.TaxId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TaxConfiguration : IEntityTypeConfiguration<Tax>
{
    public void Configure(EntityTypeBuilder<Tax> builder)
    {
        builder.ConfigureCodeName("taxes");
        builder.Property(t => t.RatePercent).HasPrecision(5, 2);
        builder.ToTable(t => t.HasCheckConstraint("ck_taxes_rate", "rate_percent >= 0 AND rate_percent <= 100"));
    }
}

public class DiscountTypeConfiguration : IEntityTypeConfiguration<DiscountType>
{
    public void Configure(EntityTypeBuilder<DiscountType> builder)
    {
        builder.ConfigureCodeName("discount_types");
        builder.Property(d => d.MaxPercent).HasPrecision(5, 2);
        builder.Property(d => d.ApprovalThresholdPercent).HasPrecision(5, 2);
        builder.ToTable(t => t.HasCheckConstraint("ck_discount_types_percents",
            "max_percent >= 0 AND max_percent <= 100 AND approval_threshold_percent >= 0 AND approval_threshold_percent <= 100"));
    }
}

public class ReferrerConfiguration : IEntityTypeConfiguration<Referrer>
{
    public void Configure(EntityTypeBuilder<Referrer> builder)
    {
        builder.ConfigureBranchCodeName("referrers");
        builder.Property(r => r.Phone).HasMaxLength(20);
        builder.Property(r => r.Organization).HasMaxLength(200);
        builder.HasOne(r => r.Doctor).WithMany()
            .HasForeignKey(r => r.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IcdCodeConfiguration : IEntityTypeConfiguration<IcdCode>
{
    public void Configure(EntityTypeBuilder<IcdCode> builder)
    {
        builder.ToTable("icd_codes", "master");
        builder.Property(c => c.Code).HasMaxLength(20);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Category).HasMaxLength(200);
        builder.HasIndex(c => c.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class CorporateConfiguration : IEntityTypeConfiguration<Corporate>
{
    public void Configure(EntityTypeBuilder<Corporate> builder)
    {
        builder.ConfigureCodeName("corporates");
        builder.Property(c => c.ContactPerson).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.GstNumber).HasMaxLength(20);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);
    }
}

public class InsuranceCompanyConfiguration : IEntityTypeConfiguration<InsuranceCompany>
{
    public void Configure(EntityTypeBuilder<InsuranceCompany> builder)
    {
        builder.ConfigureCodeName("insurance_companies");
        builder.Property(i => i.ContactPerson).HasMaxLength(200);
        builder.Property(i => i.Phone).HasMaxLength(20);
        builder.Property(i => i.Email).HasMaxLength(200);
        builder.Property(i => i.Address).HasMaxLength(500);
    }
}

public class UomConfiguration : IEntityTypeConfiguration<Uom>
{
    public void Configure(EntityTypeBuilder<Uom> builder)
    {
        builder.ConfigureCodeName("uoms");
        builder.Property(u => u.Description).HasMaxLength(500);
    }
}

public class SampleTypeConfiguration : IEntityTypeConfiguration<SampleType>
{
    public void Configure(EntityTypeBuilder<SampleType> builder)
    {
        builder.ConfigureCodeName("sample_types");
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}

public class ModalityConfiguration : IEntityTypeConfiguration<Modality>
{
    public void Configure(EntityTypeBuilder<Modality> builder)
    {
        builder.ConfigureCodeName("modalities");
        builder.Property(m => m.Description).HasMaxLength(500);
    }
}

public class RecordDocumentTypeConfiguration : IEntityTypeConfiguration<RecordDocumentType>
{
    public void Configure(EntityTypeBuilder<RecordDocumentType> builder)
    {
        builder.ConfigureCodeName("record_document_types");
        builder.Property(r => r.Description).HasMaxLength(500);
    }
}

// Front Office reference-data masters — all plain code+name, same shape as Uom/SampleType above.

public class CampConfiguration : IEntityTypeConfiguration<Camp>
{
    public void Configure(EntityTypeBuilder<Camp> builder)
    {
        builder.ConfigureCodeName("camps");
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}

public class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ConfigureCodeName("states");
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ConfigureCodeName("cities");
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}

public class NationalityConfiguration : IEntityTypeConfiguration<Nationality>
{
    public void Configure(EntityTypeBuilder<Nationality> builder)
    {
        builder.ConfigureCodeName("nationalities");
        builder.Property(n => n.Description).HasMaxLength(500);
    }
}

public class ReligionConfiguration : IEntityTypeConfiguration<Religion>
{
    public void Configure(EntityTypeBuilder<Religion> builder)
    {
        builder.ConfigureCodeName("religions");
        builder.Property(r => r.Description).HasMaxLength(500);
    }
}

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ConfigureCodeName("languages");
        builder.Property(l => l.Description).HasMaxLength(500);
    }
}

public class SpeciallyAbledCategoryConfiguration : IEntityTypeConfiguration<SpeciallyAbledCategory>
{
    public void Configure(EntityTypeBuilder<SpeciallyAbledCategory> builder)
    {
        builder.ConfigureCodeName("specially_abled_categories");
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}

public class PatientHcTagConfiguration : IEntityTypeConfiguration<PatientHcTag>
{
    public void Configure(EntityTypeBuilder<PatientHcTag> builder)
    {
        builder.ConfigureCodeName("patient_hc_tags");
        builder.Property(p => p.Description).HasMaxLength(500);
    }
}

public class PatientCaseTypeConfiguration : IEntityTypeConfiguration<PatientCaseType>
{
    public void Configure(EntityTypeBuilder<PatientCaseType> builder)
    {
        builder.ConfigureCodeName("patient_case_types");
        builder.Property(p => p.Description).HasMaxLength(500);
    }
}

public class MlcTypeConfiguration : IEntityTypeConfiguration<MlcType>
{
    public void Configure(EntityTypeBuilder<MlcType> builder)
    {
        builder.ConfigureCodeName("mlc_types");
        builder.Property(m => m.Description).HasMaxLength(500);
    }
}

public class MhcDoctorConfiguration : IEntityTypeConfiguration<MhcDoctor>
{
    public void Configure(EntityTypeBuilder<MhcDoctor> builder)
    {
        builder.ConfigureCodeName("mhc_doctors");
        builder.Property(m => m.Description).HasMaxLength(500);
    }
}

public class MhcPackageConfiguration : IEntityTypeConfiguration<MhcPackage>
{
    public void Configure(EntityTypeBuilder<MhcPackage> builder)
    {
        builder.ConfigureCodeName("mhc_packages");
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(18, 2);
    }
}

public class LoyaltyProgramConfiguration : IEntityTypeConfiguration<LoyaltyProgram>
{
    public void Configure(EntityTypeBuilder<LoyaltyProgram> builder)
    {
        builder.ConfigureCodeName("loyalty_programs");
        builder.Property(l => l.Description).HasMaxLength(500);
    }
}

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ConfigureCodeName("units");
        builder.Property(u => u.Description).HasMaxLength(500);
    }
}

public class UnitDoctorConfiguration : IEntityTypeConfiguration<UnitDoctor>
{
    public void Configure(EntityTypeBuilder<UnitDoctor> builder)
    {
        builder.ToTable("unit_doctors", "master");
        builder.HasIndex(ud => new { ud.UnitId, ud.DoctorId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(ud => ud.Unit).WithMany()
            .HasForeignKey(ud => ud.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(ud => ud.Doctor).WithMany()
            .HasForeignKey(ud => ud.DoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppointmentTypeConfiguration : IEntityTypeConfiguration<AppointmentType>
{
    public void Configure(EntityTypeBuilder<AppointmentType> builder)
    {
        builder.ConfigureCodeName("appointment_types");
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.ToTable(t => t.HasCheckConstraint("ck_appointment_types_slot_minutes", "default_slot_minutes > 0"));
    }
}

public class SalutationConfiguration : IEntityTypeConfiguration<Salutation>
{
    public void Configure(EntityTypeBuilder<Salutation> builder)
    {
        builder.ConfigureCodeName("salutations");
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}

public class MaritalStatusConfiguration : IEntityTypeConfiguration<MaritalStatus>
{
    public void Configure(EntityTypeBuilder<MaritalStatus> builder)
    {
        builder.ConfigureCodeName("marital_statuses");
        builder.Property(m => m.Description).HasMaxLength(500);
    }
}

public class IdentityDocumentTypeConfiguration : IEntityTypeConfiguration<IdentityDocumentType>
{
    public void Configure(EntityTypeBuilder<IdentityDocumentType> builder)
    {
        builder.ConfigureCodeName("identity_document_types");
        builder.Property(i => i.Description).HasMaxLength(500);
    }
}

public class VisitClassConfiguration : IEntityTypeConfiguration<VisitClass>
{
    public void Configure(EntityTypeBuilder<VisitClass> builder)
    {
        builder.ConfigureCodeName("visit_classes");
        builder.Property(v => v.Description).HasMaxLength(500);
    }
}

public class VisitSessionConfiguration : IEntityTypeConfiguration<VisitSession>
{
    public void Configure(EntityTypeBuilder<VisitSession> builder)
    {
        builder.ConfigureCodeName("visit_sessions");
        builder.Property(v => v.Description).HasMaxLength(500);
    }
}

public class ClaimDocumentTypeConfiguration : IEntityTypeConfiguration<ClaimDocumentType>
{
    public void Configure(EntityTypeBuilder<ClaimDocumentType> builder)
    {
        builder.ConfigureCodeName("claim_document_types");
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}

public class ClaimDenialReasonConfiguration : IEntityTypeConfiguration<ClaimDenialReason>
{
    public void Configure(EntityTypeBuilder<ClaimDenialReason> builder)
    {
        builder.ConfigureCodeName("claim_denial_reasons");
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("areas", "master");
        builder.Property(a => a.Code).HasMaxLength(30);
        builder.Property(a => a.Name).HasMaxLength(200);
        builder.HasIndex(a => new { a.CityId, a.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(a => a.City).WithMany()
            .HasForeignKey(a => a.CityId).OnDelete(DeleteBehavior.Restrict);
    }
}
