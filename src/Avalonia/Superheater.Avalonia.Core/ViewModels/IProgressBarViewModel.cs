namespace Superheater.Avalonia.Core.ViewModels
{
    public interface IProgressBarViewModel
    {
        bool IsInProgress { get; }

        float ProgressBarValue { get; set; }

        string ProgressBarText { get; set; }
    }
}
