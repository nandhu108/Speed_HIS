using FluentAssertions;
using Prixo.Application.Common.Security;

namespace Prixo.Application.Tests.Security;

public class PermissionRegistryTests
{
    [Fact]
    public void AllCodes_AreUnique()
    {
        var codes = PermissionRegistry.All.Select(p => p.Code).ToList();
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllCodes_FollowModuleResourceActionFormat()
    {
        foreach (var definition in PermissionRegistry.All)
        {
            var parts = definition.Code.Split('.');
            parts.Should().HaveCount(3, $"'{definition.Code}' must be MODULE.RESOURCE.ACTION");
            parts.Should().OnlyContain(part => part == part.ToUpperInvariant() && part.Length > 0,
                $"'{definition.Code}' must be uppercase");
        }
    }

    [Fact]
    public void Definitions_DeriveModuleResourceAction()
    {
        var definition = new PermissionDefinition("ADMIN.USER.VIEW", "View users");
        definition.Module.Should().Be("ADMIN");
        definition.Resource.Should().Be("USER");
        definition.Action.Should().Be("VIEW");
    }

    [Fact]
    public void EveryDefinition_HasDescription()
    {
        PermissionRegistry.All.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Description));
    }

    [Fact]
    public void OpBillingRole_CoversLegacyOpBillingWithoutIpAccess()
    {
        var role = DefaultRoleDefinitions.Find("OP_BILLING");

        role.Should().NotBeNull();
        role!.Permissions.Should().Contain(PermissionRegistry.BillView);
        role.Permissions.Should().Contain(PermissionRegistry.BillViewOp);
        role.Permissions.Should().Contain(PermissionRegistry.BillCreateOp);
        role.Permissions.Should().Contain(PermissionRegistry.TariffManage);
        role.Permissions.Should().Contain(PermissionRegistry.ServiceManage);
        role.Permissions.Should().Contain(PermissionRegistry.MhcPackageManage);
        role.Permissions.Should().Contain(PermissionRegistry.CorporateManage);
        role.Permissions.Should().Contain(PermissionRegistry.DiscountApprove);
        role.Permissions.Should().Contain(PermissionRegistry.RefundApprove);
        role.Permissions.Should().Contain(PermissionRegistry.CashierShiftReceive);
        role.Permissions.Should().Contain(PermissionRegistry.BillingReportView);
        role.Permissions.Should().NotContain(PermissionRegistry.BillViewIp);
        role.Permissions.Should().NotContain(PermissionRegistry.BillViewEmergency);
        role.Permissions.Should().NotContain(PermissionRegistry.BillCreateIp);
        role.Permissions.Should().NotContain(PermissionRegistry.BillCreateEmergency);
    }

    [Fact]
    public void OpBillingSupervisorRole_IsNotSeededSeparately()
    {
        DefaultRoleDefinitions.Find("OP_BILLING_SUPERVISOR").Should().BeNull();
    }
}
