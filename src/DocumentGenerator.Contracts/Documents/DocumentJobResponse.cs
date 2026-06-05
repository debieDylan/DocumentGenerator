namespace DocumentGenerator.Contracts.Documents;

public sealed record DocumentJobResponse(
    Guid JobId,
    string Status,
    bool ReusedExistingJob);
