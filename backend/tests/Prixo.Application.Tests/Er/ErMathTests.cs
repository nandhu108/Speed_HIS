using FluentAssertions;
using Prixo.Application.Features.Er;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Er;

public class ErMathQueuePriorityTests
{
    [Fact]
    public void Untriaged_SortsAheadOfResuscitation() =>
        ErMath.QueuePriority(null).Should().BeLessThan(ErMath.QueuePriority(EsiLevel.Resuscitation));

    [Fact]
    public void Resuscitation_SortsAheadOfEmergent() =>
        ErMath.QueuePriority(EsiLevel.Resuscitation).Should().BeLessThan(ErMath.QueuePriority(EsiLevel.Emergent));

    [Fact]
    public void NonUrgent_SortsLast() =>
        ErMath.QueuePriority(EsiLevel.NonUrgent).Should().BeGreaterThan(ErMath.QueuePriority(EsiLevel.Urgent));
}
