namespace Superheater.Avalonia.Core.ViewModels.Popups
{
    public interface IPopup
    {
        public bool IsVisible { get; }

        public string TitleText { get; }

        event Action<bool>? PopupShownEvent;
    }
}
