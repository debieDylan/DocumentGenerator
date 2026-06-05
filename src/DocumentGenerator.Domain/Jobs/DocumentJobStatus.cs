namespace DocumentGenerator.Domain.Jobs;

public enum DocumentJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Canceled = 4,
}
