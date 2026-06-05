namespace DocumentGenerator.Domain.Jobs;

public readonly record struct DocumentJobId(Guid Value)
{
    public static DocumentJobId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
