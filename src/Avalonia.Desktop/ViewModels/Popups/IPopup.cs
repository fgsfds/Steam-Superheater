namespace Avalonia.Desktop.ViewModels.Popups;

public interface IPopup
{
    /// <summary>
    /// Is popup visible
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Popup title text
    /// </summary>
    string TitleText { get; }

    /// <summary>
    /// Invoked when popup is shown
    /// </summary>
    event Action<bool>? PopupShownEvent;
}

