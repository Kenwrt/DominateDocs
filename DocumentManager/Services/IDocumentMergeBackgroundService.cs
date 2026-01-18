using DominateDocsData.Models;

namespace DocumentManager.Services;

public interface IDocumentMergeBackgroundService
{
    event EventHandler<DocumentMerge> OnDocumentMergeCompletedEvent;

    event EventHandler<DocumentMerge> OnDocumentMergeErrorEvent;

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}