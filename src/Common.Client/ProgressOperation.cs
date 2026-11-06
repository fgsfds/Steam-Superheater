namespace Common.Client;

/// <summary>Owns one cancellable UI operation's progress subscriptions and cleanup.</summary>
public sealed class ProgressOperation : IDisposable
{
    private readonly ProgressReport _report;
    private readonly EventHandler<float> _progress;
    private readonly ProgressReport.OperationMessageChanged _message;
    private readonly Action _cleanup;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _disposed;
    /// <summary>Cancellation for this operation.</summary>
    public CancellationToken Token => _cancellation.Token;
    /// <summary>Attaches handlers for an operation with a guaranteed disposal callback.</summary>
    public ProgressOperation(ProgressReport report, EventHandler<float> progress, ProgressReport.OperationMessageChanged message, Action cleanup)
    {
        _report = report; _progress = progress; _message = message; _cleanup = cleanup;
        report.Progress.ProgressChanged += _progress;
        report.NotifyOperationMessageChanged += _message;
    }
    /// <summary>Requests cancellation if the operation is still active.</summary>
    public Task CancelAsync() => _disposed ? Task.CompletedTask : _cancellation.CancelAsync();
    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        _report.Progress.ProgressChanged -= _progress;
        _report.NotifyOperationMessageChanged -= _message;
        _cancellation.Dispose();
        _cleanup();
    }
}
