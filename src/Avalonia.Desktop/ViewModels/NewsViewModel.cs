using Avalonia.Desktop.ViewModels.Popups;
using Common;
using Common.Client.Providers;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;

namespace Avalonia.Desktop.ViewModels;

internal sealed partial class NewsViewModel : ObservableObject
{
    private readonly NewsProvider _newsProvider;
    private readonly IConfigProvider _config;
    private readonly PopupMessageViewModel _popupMessage;
    private readonly PopupEditorViewModel _popupEditor;
    private readonly SemaphoreSlim _locker = new(1);

    private int _selectedPage = 1;


    #region Binding Properties

    public ImmutableList<NewsEntity> NewsList => _newsProvider.PagesCount == 0 ? [] : [.. _newsProvider.GetNewsPage(_selectedPage)];

    public List<int> PagesCount => Enumerable.Range(1, _newsProvider.PagesCount).ToList();

    public string NewsTabHeader =>
        "News" + (_newsProvider.HasUnreadNews
        ? $" ({_newsProvider.UnreadNewsCount} unread)"
        : string.Empty);

    #endregion Binding Properties


    public NewsViewModel(
        NewsProvider newsProvider,
        IConfigProvider configProvider,
        PopupMessageViewModel popupMessage,
        PopupEditorViewModel popupEditor
        )
    {
        _newsProvider = newsProvider;
        _config = configProvider;
        _popupMessage = popupMessage;
        _popupEditor = popupEditor;

        _config.ParameterChangedEvent += OnParameterChangedEvent;
    }


    #region Relay Commands

    /// <summary>
    /// VM initialization
    /// </summary>
    [RelayCommand]
    private Task InitializeAsync() => UpdateAsync();

    /// <summary>
    /// Mark all news as read
    /// </summary>
    [RelayCommand(CanExecute = (nameof(MarkAllAsReadCanExecute)))]
    private void MarkAllAsRead()
    {
        _newsProvider.MarkAllAsRead();

        OnPropertyChanged(nameof(NewsList));
        OnPropertyChanged(nameof(NewsTabHeader));
        MarkAllAsReadCommand.NotifyCanExecuteChanged();
    }
    private bool MarkAllAsReadCanExecute() => _newsProvider.HasUnreadNews;

    /// <summary>
    /// Add news
    /// </summary>
    [RelayCommand]
    private async Task AddNews()
    {
        var newContent = await _popupEditor.ShowAndGetResultAsync("Add news entry", string.Empty).ConfigureAwait(true);

        if (newContent is null)
        {
            return;
        }

        var result = await _newsProvider.AddNewsAsync(newContent).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            _popupMessage.Show(
                "Error",
                result.Message,
                PopupMessageType.OkOnly
                );

            return;
        }

        OnPropertyChanged(nameof(NewsList));
    }

    /// <summary>
    /// Edit current news content
    /// </summary>
    [RelayCommand]
    private async Task EditNews(DateTime date)
    {
        var news = NewsList.FirstOrDefault(x => x.Date == date);

        if (news is null)
        {
            return;
        }

        var newContent = await _popupEditor.ShowAndGetResultAsync("Edit news entry", news.Content).ConfigureAwait(true);

        if (newContent is null)
        {
            return;
        }

        var result = await _newsProvider.ChangeNewsContentAsync(date, newContent).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            _popupMessage.Show(
                "Error",
                result.Message,
                PopupMessageType.OkOnly
                );

            return;
        }

        OnPropertyChanged(nameof(NewsList));
    }

    /// <summary>
    /// Edit current news content
    /// </summary>
    [RelayCommand]
    private void ChangePage(int a)
    {
        _selectedPage = a;
        OnPropertyChanged(nameof(NewsList));
    }

    #endregion Relay Commands

    /// <summary>
    /// Update news list and tab header
    /// </summary>
    private async Task UpdateAsync()
    {
        await _locker.WaitAsync().ConfigureAwait(true);

        var result = await _newsProvider.UpdateNewsListAsync().ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            _popupMessage.Show(
                "Error",
                result.Message,
                PopupMessageType.OkOnly
                );
        }

        OnPropertyChanged(nameof(NewsList));
        OnPropertyChanged(nameof(NewsTabHeader));
        OnPropertyChanged(nameof(PagesCount));
        MarkAllAsReadCommand.NotifyCanExecuteChanged();

        _ = _locker.Release();
    }

    private async void OnParameterChangedEvent(string parameterName)
    {
        if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)))
        {
            await UpdateAsync().ConfigureAwait(true);
        }
    }
}

