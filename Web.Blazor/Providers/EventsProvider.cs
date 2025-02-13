using Common.Enums;
using Database.Server;
using Database.Server.DbEntities;

namespace Web.Blazor.Providers;

public sealed class EventsProvider
{
    private readonly DatabaseContextFactory _databaseContextFactory;

    public EventsProvider(DatabaseContextFactory databaseContextFactory)
    {
        _databaseContextFactory = databaseContextFactory;
    }

    public async Task LogEventAsync(EventTypeEnum eventTypeEnum, Version version, Guid? fixGuid)
    {
        await using var dbContext = _databaseContextFactory.Get();

        EventsDbEntity entity = new()
        {
            EventType = eventTypeEnum,
            Version = version.ToString(),
            Time = DateTime.UtcNow,
            FixGuid = fixGuid
        };

        _ = await dbContext.Events.AddAsync(entity);
        _ = await dbContext.SaveChangesAsync();
    }
}
