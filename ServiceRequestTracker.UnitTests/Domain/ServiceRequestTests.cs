using FluentAssertions;
using ServiceRequestTracker.Domain;
using ServiceRequestTracker.Domain.Enums;
using static ServiceRequestTracker.Domain.Enums.ServiceRequestStatus;

namespace ServiceRequestTracker.UnitTests.Domain;

public class ServiceRequestTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FirstChangeAt = new(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc);
    private static readonly DateTime SecondChangeAt = new(2026, 1, 1, 12, 10, 0, DateTimeKind.Utc);

    public static TheoryData<ServiceRequestStatus, ServiceRequestStatus> ForbiddenTransitions => new()
    {
        { Done, InProgress },
        { Cancelled, InProgress },
        { New, Done },
        { New, New },
        { InProgress, InProgress },
        { InProgress, New },
        { Done, New },
        { Done, Done },
        { Cancelled, Cancelled }
    };

    [Fact]
    public void Create_ValidArguments_SetsNewStatusAndTimestamps()
    {
        var request = ServiceRequest.Create("Title", "description", Priority.High, CreatedAt);

        request.Id.Should().NotBeEmpty();
        request.Status.Should().Be(New);
        request.CreatedAt.Should().Be(CreatedAt);
        request.UpdatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void ChangeStatus_NormalFlow_MovesNewToInProgressToDone()
    {
        var request = ServiceRequest.Create("Title", null, Priority.Normal, CreatedAt);

        request.ChangeStatus(InProgress, FirstChangeAt);

        request.Status.Should().Be(InProgress);
        request.UpdatedAt.Should().Be(FirstChangeAt);
        request.CreatedAt.Should().Be(CreatedAt);

        request.ChangeStatus(Done, SecondChangeAt);

        request.Status.Should().Be(Done);
        request.UpdatedAt.Should().Be(SecondChangeAt);
        request.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void ChangeStatus_CancelledToNew_IsAllowed()
    {
        var request = CreateInStatus(Cancelled);

        request.ChangeStatus(New, SecondChangeAt);

        request.Status.Should().Be(New);
        request.UpdatedAt.Should().Be(SecondChangeAt);
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public void ChangeStatus_ForbiddenTransition_ThrowsAndKeepsState(ServiceRequestStatus from, ServiceRequestStatus to)
    {
        var request = CreateInStatus(from);
        var updatedAtBefore = request.UpdatedAt;

        var act = () => request.ChangeStatus(to, SecondChangeAt);

        act.Should().Throw<InvalidStatusTransitionException>()
            .Where(e => e.From == from && e.To == to)
            .WithMessage($"Cannot change status from {from} to {to}");
        request.Status.Should().Be(from);
        request.UpdatedAt.Should().Be(updatedAtBefore);
    }

    private static ServiceRequest CreateInStatus(ServiceRequestStatus status)
    {
        var request = ServiceRequest.Create("Title", null, Priority.Normal, CreatedAt);

        switch (status)
        {
            case InProgress:
                request.ChangeStatus(InProgress, FirstChangeAt);
                break;
            case Done:
                request.ChangeStatus(InProgress, FirstChangeAt);
                request.ChangeStatus(Done, FirstChangeAt);
                break;
            case Cancelled:
                request.ChangeStatus(Cancelled, FirstChangeAt);
                break;
        }

        return request;
    }
}