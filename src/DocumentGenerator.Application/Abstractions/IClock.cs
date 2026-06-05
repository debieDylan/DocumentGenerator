namespace DocumentGenerator.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
