using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Tests;

public sealed class ArchitectureSmokeTests
{
    public static bool ApplicationPortsStayAsync() =>
        typeof(IDocumentJobRepository).GetMethod(nameof(IDocumentJobRepository.GetByIdAsync))?.ReturnType
            == typeof(Task<DocumentJob?>);
}
