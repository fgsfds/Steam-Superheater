using Avalonia.Desktop.Helpers;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Platform.Storage;
using Common.Client.Providers.Interfaces;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;

namespace Avalonia.Desktop.ViewModels.Editor;

internal sealed partial class FileFixViewModel : ObservableObject
{
    private FileFixEntity SelectedFix { get; set; }

    private readonly IFixesProvider _fixesProvider;

    private readonly PopupEditorViewModel _popupEditor;

    public ImmutableList<FileFixEntity>? SharedFixesList { get; set; }

    public string SelectedFixVariants
    {
        get => SelectedFix is FileFixEntity fileFix && fileFix.Variants is not null
            ? string.Join(';', fileFix.Variants)
            : string.Empty;
        set
        {
            Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

            fileFix.Variants = value.SplitSemicolonSeparatedString();
        }
    }

    public string SelectedFixFilesToDelete
    {
        get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToDelete is not null
            ? string.Join(';', fileFix.FilesToDelete)
            : string.Empty;
        set
        {
            Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

            fileFix.FilesToDelete = value.SplitSemicolonSeparatedString();
        }
    }

    public string SelectedFixFilesToBackup
    {
        get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToBackup is not null
            ? string.Join(';', fileFix.FilesToBackup)
            : string.Empty;
        set
        {
            Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

            fileFix.FilesToBackup = value.SplitSemicolonSeparatedString();
        }
    }

    public string SelectedFixFilesToPatch
    {
        get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToPatch is not null
            ? string.Join(';', fileFix.FilesToPatch)
            : string.Empty;
        set
        {
            Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

            fileFix.FilesToPatch = value.SplitSemicolonSeparatedString();
        }
    }

    public string SelectedFixWineDllsOverrides
    {
        get => SelectedFix is FileFixEntity fileFix && fileFix.WineDllOverrides is not null
            ? string.Join(';', fileFix.WineDllOverrides)
            : string.Empty;
        set
        {
            Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

            fileFix.WineDllOverrides = value.SplitSemicolonSeparatedString();
        }
    }

    public string SelectedFixUrl
    {
        get => SelectedFix is FileFixEntity fileFix && fileFix.Url is not null
            ? fileFix.Url
            : string.Empty;
        set
        {
            Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

            fileFix.Url = string.IsNullOrWhiteSpace(value)
                ? null
                : value;
        }
    }

    public BaseFixEntity? SelectedSharedFix
    {
        get => SelectedFix is not FileFixEntity fileFix || fileFix.SharedFixGuid is null ? null : SharedFixesList?.FirstOrDefault(x => x.Guid == fileFix.SharedFixGuid);
        set
        {
            Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

            fileFix.SharedFixGuid = value?.Guid;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSharedFixSelected));
            ResetSelectedSharedFixCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsSharedFixSelected => SelectedSharedFix is not null;


    public FileFixViewModel(
        FileFixEntity fix,
        IFixesProvider fixesProvider,
        PopupEditorViewModel popupEditor
        )
    {
        SelectedFix = fix;
        _fixesProvider = fixesProvider;
        _popupEditor = popupEditor;
    }


    /// <summary>
    /// Open fix file picker
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenFilePickerCanExecute))]
    private async Task OpenFilePickerAsync()
    {
        Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

        var topLevel = AvaloniaProperties.TopLevel;

        FilePickerFileType zipType = new("Zip")
        {
            Patterns = new[] { "*.zip" },
            MimeTypes = new[] { "application/zip" }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose fix file",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>() { zipType }
        }).ConfigureAwait(true);

        if (files.Count == 0)
        {
            return;
        }

        fileFix.Url = files[0].Path.LocalPath;
        OnPropertyChanged(nameof(SelectedFixUrl));
    }
    private bool OpenFilePickerCanExecute() => SelectedFix is FileFixEntity;


    /// <summary>
    /// Open files to delete editor
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenFilesToDeleteEditorCanExecute))]
    private async Task OpenFilesToDeleteEditorAsync()
    {
        Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

        var result = await _popupEditor.ShowAndGetResultAsync("Files to delete", fileFix.FilesToDelete).ConfigureAwait(true);

        if (result is not null)
        {
            fileFix.FilesToDelete = result.ToListOfString();
            OnPropertyChanged(nameof(SelectedFixFilesToDelete));
        }
    }
    private bool OpenFilesToDeleteEditorCanExecute() => SelectedFix is FileFixEntity;


    /// <summary>
    /// Open files to backup editor
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenFilesToBackupEditorCanExecute))]
    private async Task OpenFilesToBackupEditorAsync()
    {
        Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

        var result = await _popupEditor.ShowAndGetResultAsync("Files to backup", fileFix.FilesToBackup).ConfigureAwait(true);

        if (result is not null)
        {
            fileFix.FilesToBackup = result.ToListOfString();
            OnPropertyChanged(nameof(SelectedFixFilesToBackup));
        }
    }
    private bool OpenFilesToBackupEditorCanExecute() => SelectedFix is FileFixEntity;


    /// <summary>
    /// Open files to patch editor
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenFilesToPatchEditorCanExecute))]
    private async Task OpenFilesToPatchEditorAsync()
    {
        Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

        var result = await _popupEditor.ShowAndGetResultAsync("Files to patch", fileFix.FilesToPatch).ConfigureAwait(true);

        if (result is not null)
        {
            fileFix.FilesToPatch = result.ToListOfString();
            OnPropertyChanged(nameof(SelectedFixFilesToPatch));
        }
    }
    private bool OpenFilesToPatchEditorCanExecute() => SelectedFix is FileFixEntity;


    /// <summary>
    /// Open wine dll overrides editor
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenWineDllsOverridesEditorCanExecute))]
    private async Task OpenWineDllsOverridesEditorAsync()
    {
        Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

        var result = await _popupEditor.ShowAndGetResultAsync("Wine DLL overrides", fileFix.WineDllOverrides).ConfigureAwait(true);

        if (result is not null)
        {
            fileFix.WineDllOverrides = result.ToListOfString();
            OnPropertyChanged(nameof(SelectedFixWineDllsOverrides));
        }
    }
    private bool OpenWineDllsOverridesEditorCanExecute() => SelectedFix is FileFixEntity;


    /// <summary>
    /// Open variants editor
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenVariantsEditorCanExecute))]
    private async Task OpenVariantsEditorAsync()
    {
        Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

        var result = await _popupEditor.ShowAndGetResultAsync("Fix variants", fileFix.Variants).ConfigureAwait(true);

        if (result is not null)
        {
            fileFix.Variants = result.ToListOfString();
            OnPropertyChanged(nameof(SelectedFixVariants));
        }
    }
    private bool OpenVariantsEditorCanExecute() => SelectedFix is FileFixEntity;


    /// <summary>
    /// Reset shared fix
    /// </summary>
    [RelayCommand(CanExecute = nameof(ResetSelectedSharedFixCanExecute))]
    private void ResetSelectedSharedFix()
    {
        Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);

        SelectedSharedFix = null;
        fileFix.SharedFixInstallFolder = null;

        OnPropertyChanged(nameof(SelectedSharedFix));
        OnPropertyChanged(nameof(IsSharedFixSelected));
    }
    private bool ResetSelectedSharedFixCanExecute() => SelectedSharedFix is not null;
}
