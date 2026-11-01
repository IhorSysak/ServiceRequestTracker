using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ServiceRequestTracker.Features.ServiceRequests;
using ServiceRequestTracker.Infrastructure.Messaging;
using ServiceRequestTracker.Infrastructure.Outbox;
using ServiceRequestTracker.Infrastructure.Persistence;

namespace ServiceRequestTracker.IntegrationTests;

[Collection(IntegrationCollection.Name)]
public class MessagingTests(AppFixture fixture)
{
    private static readonly JsonSerializerOptions ResponseJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task CreateRequest_Posted_EventReachesConsumer()
    {
        var created = await CreateRequestAsync("High");

        var events = await WaitForEventsAsync(created.Id, expectedCount: 1);

        var processed = events.Should().ContainSingle().Subject;
        processed.OldStatus.Should().BeNull();
        processed.NewStatus.Should().Be("New");
        processed.Priority.Should().Be("High");

        var outbox = await Eventually.WaitAsync(
            () => fixture.QueryDbAsync(db => db.OutboxMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == processed.MessageId)),
            message => message?.ProcessedAt is not null);

        outbox.Should().NotBeNull("the processed event must come from an outbox record with the same message id");
        outbox!.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeStatus_Patched_EventReachesConsumer()
    {
        var created = await CreateRequestAsync("Normal");

        using var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/service-requests/{created.Id}/status")
        {
            Content = JsonContent.Create(new { status = "InProgress" })
        };
        var response = await fixture.Client.SendAsync(patch);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var events = await WaitForEventsAsync(created.Id, expectedCount: 2);

        events.Should().HaveCount(2);
        var statusChange = events[1];
        statusChange.OldStatus.Should().Be("New");
        statusChange.NewStatus.Should().Be("InProgress");
    }

    [Fact]
    public async Task Consumer_SameMessagePublishedTwice_StoresItOnce()
    {
        var duplicate = BuildPayload(Guid.NewGuid(), Guid.NewGuid());
        var marker = BuildPayload(Guid.NewGuid(), Guid.NewGuid());
        var duplicateId = MessageIdOf(duplicate);
        var markerId = MessageIdOf(marker);

        await fixture.PublishAsync(duplicate, duplicate, marker);

        var markerRows = await Eventually.WaitAsync(
            () => fixture.QueryDbAsync(db => db.ProcessedEvents.AsNoTracking().CountAsync(x => x.MessageId == markerId)),
            count => count >= 1);
        markerRows.Should().Be(1);

        var duplicateRows = await fixture.QueryDbAsync(db => db.ProcessedEvents.AsNoTracking().CountAsync(x => x.MessageId == duplicateId));
        duplicateRows.Should().Be(1);
    }

    private async Task<ServiceRequestResponse> CreateRequestAsync(string priority)
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/service-requests", new
        {
            title = $"Pump {Guid.NewGuid()}",
            priority
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ServiceRequestResponse>(ResponseJson);
        return created!;
    }

    private async Task<List<ProcessedEvent>> WaitForEventsAsync(Guid requestId, int expectedCount) =>
        await Eventually.WaitAsync(
            () => fixture.QueryDbAsync(db => db.ProcessedEvents
                .AsNoTracking()
                .Where(x => x.RequestId == requestId)
                .OrderBy(x => x.OccurredAt)
                .ToListAsync()),
            events => events.Count >= expectedCount);

    private static string BuildPayload(Guid messageId, Guid requestId) =>
        JsonSerializer.Serialize(
            new ServiceRequestChangedEvent(messageId, requestId, OldStatus: null, NewStatus: "New", Priority: "Normal", DateTime.UtcNow),
            JsonSerializerOptions.Web);

    private static Guid MessageIdOf(string payload) =>
        JsonDocument.Parse(payload).RootElement.GetProperty("messageId").GetGuid();
}