using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Desktop.ViewModels.Popups;

public sealed partial class PopupMessageViewModel : ObservableObject, IPopup
{
    private Action? _okAction;
    private SemaphoreSlim? _semaphore;
    private bool _result;

    public event Action<bool>? PopupShownEvent;


    #region Binding Properties

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _isYesNo;

    [ObservableProperty]
    private bool _isOkOnly;

    [ObservableProperty]
    private string _titleText = string.Empty;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private HorizontalAlignment _textAlignment;

    #endregion Binding Properties


    #region Relay Commands

    [RelayCommand]
    private void Cancel()
    {
        _result = false;

        Reset();
    }

    [RelayCommand]
    private void Ok()
    {
        _result = true;

        Reset();

        _okAction?.Invoke();
    }

    #endregion Relay Commands


    /// <summary>
    /// Show popup window and return result
    /// </summary>
    /// <param name="title">Popup title</param>
    /// <param name="message">Popup message</param>
    /// <param name="type">Popup type</param>
    /// <param name="okAction">Action that performs on OK button press</param>
    /// <param name="alignment">Message text alignment</param>
    /// <returns>true if Ok or Yes pressed, false if Cancel pressed</returns>
    public async Task<bool> ShowAndGetResultAsync(
        string title,
        string message,
        PopupMessageType type,
        Action? okAction = null,
        HorizontalAlignment alignment = HorizontalAlignment.Center
        )
    {
        ChangePopupType(type);

        _okAction = okAction;
        TitleText = title;
        MessageText = message;
        TextAlignment = alignment;

        IsVisible = true;
        PopupShownEvent?.Invoke(true);

        _semaphore = new(0);
        await _semaphore.WaitAsync().ConfigureAwait(true);

        return _result;
    }


    /// <summary>
    /// Show popup window and return result
    /// </summary>
    /// <param name="title">Popup title</param>
    /// <param name="message">Popup message</param>
    /// <param name="type">Popup type</param>
    /// <param name="okAction">Action that performs on OK button press</param>
    /// <param name="alignment">Message text alignment</param>
    public void Show(
        string title,
        string message,
        PopupMessageType type,
        Action? okAction = null,
        HorizontalAlignment alignment = HorizontalAlignment.Center
        )
    {
        ChangePopupType(type);

        _okAction = okAction;
        TitleText = title;
        MessageText = message;
        TextAlignment = alignment;

        IsVisible = true;
        PopupShownEvent?.Invoke(true);
    }

    /// <summary>
    /// Change type of the popup message
    /// </summary>
    /// <param name="type">Popup message type</param>
    private void ChangePopupType(PopupMessageType type)
    {
        switch (type)
        {
            case PopupMessageType.OkOnly:
                IsOkOnly = true;
                IsYesNo = false;
                break;
            case PopupMessageType.YesNo:
                IsYesNo = true;
                IsOkOnly = false;
                break;
        }
    }

    /// <summary>
    /// Reset popup to its initial state
    /// </summary>
    private void Reset()
    {
        IsVisible = false;
        PopupShownEvent?.Invoke(false);

        _ = _semaphore?.Release();
        _semaphore = null;
    }
}

public enum PopupMessageType
{
    ///<summary>
    ///OK button that executes action
    ///</summary>
    OkOnly,
    ///<summary>
    ///Yes and No buttons, Yes button executes action
    ///</summary>
    YesNo
}

