using Avalonia.Desktop.ViewModels.Popups;
using AvaloniaEdit.Utils;
using Common;
using Common.Client.Providers;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Avalonia.Desktop.ViewModels;

internal sealed partial class NewsViewModel : ObservableObject
{
    private readonly NewsProvider _newsProvider;
    private readonly IConfigProvider _config;
    private readonly PopupMessageViewModel _popupMessage;
    private readonly PopupEditorViewModel _popupEditor;
    private readonly SemaphoreSlim _locker = new(1);
    private int _loadedPage = 1;


    #region Binding Properties

    public ObservableCollection<NewsEntity> NewsList { get; private set; }

    public string NewsTabHeader =>
        "News" + (_newsProvider.HasUnreadNews
        ? $" ({_newsProvider.UnreadNewsCount} unread)"
        : string.Empty);

    [ObservableProperty]
    private Vector _scrollOffset;

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

        NewsList = [];
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
        ResetNewsList();
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
    }

    /// <summary>
    /// Edit current news content
    /// </summary>
    [RelayCommand]
    private void LoadNextPage()
    {
        if (_loadedPage == _newsProvider.PagesCount)
        {
            return;
        }

        _loadedPage++;

        var news = _newsProvider.GetNewsPage(_loadedPage);

        NewsList.AddRange(news);
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

        ResetNewsList();

        _ = _locker.Release();
    }

    /// <summary>
    /// Load first page and scroll to the top
    /// </summary>
    private void ResetNewsList()
    {
        _loadedPage = 1;
        NewsList.Clear();

        ScrollOffset = new();

        var news = _newsProvider.GetNewsPage(1);
        NewsList.AddRange(news);

        OnPropertyChanged(nameof(NewsTabHeader));
        MarkAllAsReadCommand.NotifyCanExecuteChanged();
    }

    private async void OnParameterChangedEvent(string parameterName)
    {
        if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)))
        {
            await UpdateAsync().ConfigureAwait(true);
        }
    }
}

