using Common.Client;

namespace Tests.Unit;

public sealed class ProgressOperationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnAndFailureAlwaysRemoveHandlersAndCleanup(bool fail)
    {
        var report = new ProgressReport(); var messages = 0; var cleanups = 0;
        try
        {
            using var operation = new ProgressOperation(report, (_, _) => { }, _ => messages++, () => cleanups++);
            report.OperationMessage = "active";
            if (fail) { throw new IOException("failure"); }
        }
        catch (IOException) { }
        report.OperationMessage = "after";
        Assert.Equal(1, messages); Assert.Equal(1, cleanups);
        using var next = new ProgressOperation(report, (_, _) => { }, _ => messages++, () => cleanups++);
        report.OperationMessage = "next";
        Assert.Equal(2, messages);
    }
    [Fact]
    public async Task CancellationBelongsToOneOperationAndDisposalIsIdempotent()
    {
        var cleanup = 0;
        var operation = new ProgressOperation(new ProgressReport(), (_, _) => { }, _ => { }, () => cleanup++);
        var token = operation.Token;
        await operation.CancelAsync();
        Assert.True(token.IsCancellationRequested);
        operation.Dispose(); operation.Dispose(); await operation.CancelAsync();
        Assert.Equal(1, cleanup);
        using var next = new ProgressOperation(new ProgressReport(), (_, _) => { }, _ => { }, () => { });
        Assert.False(next.Token.IsCancellationRequested);
    }
}
