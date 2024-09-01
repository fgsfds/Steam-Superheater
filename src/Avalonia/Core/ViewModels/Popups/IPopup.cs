namespace Avalonia.Core.ViewModels.Popups;

public interface IPopup
{
    /// <summary>
    /// Is popup visible
    /// </summary>
    public bool IsVisible { get; }

    /// <summary>
    /// Popup title text
    /// </summary>
    public string TitleText { get; }

    /// <summary>
    /// Invoked when popup is shown
    /// </summary>
    event Action<bool>? PopupShownEvent;
}

