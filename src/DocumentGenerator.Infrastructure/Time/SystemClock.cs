using DocumentGenerator.Application.Abstractions;

namespace DocumentGenerator.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
