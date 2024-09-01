namespace Avalonia.Core.ViewModels;

public interface IProgressBarViewModel
{
    /// <summary>
    /// Is operation in progress
    /// </summary>
    bool IsInProgress { get; }

    /// <summary>
    /// Progress numeric value
    /// </summary>
    float ProgressBarValue { get; set; }

    /// <summary>
    /// Operation descriptions
    /// </summary>
    string ProgressBarText { get; set; }
}

