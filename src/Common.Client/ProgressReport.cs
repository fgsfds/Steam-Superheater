using Octodiff.Diagnostics;

namespace Common.Client;

/// <summary>
/// Operation progress reporter
/// </summary>
public sealed class ProgressReport
{
    private string _operationMessage = string.Empty;

    /// <summary>
    /// Progress value
    /// </summary>
    public Progress<float> Progress { get; set; }

    /// <summary>
    /// Operation message
    /// </summary>
    public string OperationMessage
    {
        get => _operationMessage;
        set
        {
            if (_operationMessage != value)
            {
                _operationMessage = value;
                NotifyOperationMessageChanged?.Invoke(value);
            }
        }
    }

    public delegate void OperationMessageChanged(string message);
    public event OperationMessageChanged? NotifyOperationMessageChanged;


    public ProgressReport()
    {
        Progress = new Progress<float>();
    }
}

/// <summary>
/// Progress reporter for Octodiff patcher
/// </summary>
public sealed class OctodiffProgressReporter : IProgressReporter
{
    public delegate void ProgressChanged(float progress);
    public event ProgressChanged? NotifyProgressChanged;

    public void ReportProgress(string operation, long currentPosition, long total)
    {
        var progress = currentPosition * 100 / total;
        NotifyProgressChanged?.Invoke(progress);
    }
}

