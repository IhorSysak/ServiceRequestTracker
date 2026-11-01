using FluentAssertions;
using ServiceRequestTracker.Domain;
using ServiceRequestTracker.Domain.Enums;
using static ServiceRequestTracker.Domain.Enums.ServiceRequestStatus;

namespace ServiceRequestTracker.UnitTests.Domain;

public class StatusTransitionsTests
{
    public static TheoryData<ServiceRequestStatus, ServiceRequestStatus, bool> Matrix => new()
    {
        { New, New, false },
        { New, InProgress, true },
        { New, Done, false },
        { New, Cancelled, true },

        { InProgress, New, false },
        { InProgress, InProgress, false },
        { InProgress, Done, true },
        { InProgress, Cancelled, true },

        { Done, New, false },
        { Done, InProgress, false },
        { Done, Done, false },
        { Done, Cancelled, false },

        { Cancelled, New, true },
        { Cancelled, InProgress, false },
        { Cancelled, Done, false },
        { Cancelled, Cancelled, false }
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void CanTransition_StatusPair_ReturnsExpected(ServiceRequestStatus from, ServiceRequestStatus to, bool expected)
    {
        StatusTransitions.CanTransition(from, to).Should().Be(expected);
    }
}