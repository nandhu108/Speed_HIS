using FluentAssertions;
using Prixo.Application.Features.Rad;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Rad;

public class RadMathRecomputeOrderStatusTests
{
    [Fact]
    public void AllOrdered_StaysOrdered() =>
        RadMath.RecomputeOrderStatus([RadOrderItemStatus.Ordered, RadOrderItemStatus.Ordered]).Should().Be(RadOrderStatus.Ordered);

    [Fact]
    public void SomeProgressed_InProgress() =>
        RadMath.RecomputeOrderStatus([RadOrderItemStatus.Ordered, RadOrderItemStatus.Scheduled]).Should().Be(RadOrderStatus.InProgress);

    [Fact]
    public void AllApproved_Completed() =>
        RadMath.RecomputeOrderStatus([RadOrderItemStatus.Approved, RadOrderItemStatus.Approved]).Should().Be(RadOrderStatus.Completed);

    [Fact]
    public void ApprovedPlusCancelled_StillCompleted() =>
        RadMath.RecomputeOrderStatus([RadOrderItemStatus.Approved, RadOrderItemStatus.Cancelled]).Should().Be(RadOrderStatus.Completed);

    [Fact]
    public void AllCancelled_Cancelled() =>
        RadMath.RecomputeOrderStatus([RadOrderItemStatus.Cancelled, RadOrderItemStatus.Cancelled]).Should().Be(RadOrderStatus.Cancelled);

    [Fact]
    public void EmptyList_Cancelled() =>
        RadMath.RecomputeOrderStatus([]).Should().Be(RadOrderStatus.Cancelled);

    [Fact]
    public void CancelledPlusInProgress_InProgress() =>
        RadMath.RecomputeOrderStatus([RadOrderItemStatus.Cancelled, RadOrderItemStatus.ImageCaptured]).Should().Be(RadOrderStatus.InProgress);
}
