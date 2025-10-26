using System.Collections.Immutable;
using Avalonia.Desktop.Helpers;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Platform.Storage;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Axiom.Helpers;
using Common.Client.Providers.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Desktop.ViewModels.Editor;

internal sealed partial class FileFixViewModel : ObservableObject
{
    private FileFixEntity SelectedFix { get; set; }

    private readonly IFixesProvider _fixesProvider;

    private readonly PopupEditorViewModel _popupEditor;

    public ImmutableList<FileFixEntity>? SharedFixesList { get; set; }

    public string SelectedFixUrl
    {
        get => SelectedFix.Url ?? string.Empty;
        set
        {
            SelectedFix.Url = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
        }
    }

    public string SelectedFixInstallFolder
    {
        get => SelectedFix.InstallFolder ?? string.Empty;
        set
        {
            SelectedFix.InstallFolder = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
        }
    }

    public string SelectedFixVariants
    {
        get => SelectedFix.Variants is not null ? string.Join(';', SelectedFix.Variants) : string.Empty;
        set
        {
            SelectedFix.Variants = value.SplitSemicolonSeparatedString();
            OnPropertyChanged();
        }
    }

    public string SelectedFixConfigFile
    {
        get => SelectedFix.ConfigFile ?? string.Empty;
        set
        {
            SelectedFix.ConfigFile = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
        }
    }

    public string SelectedFixFilesToDelete
    {
        get => SelectedFix.FilesToDelete is not null ? string.Join(';', SelectedFix.FilesToDelete) : string.Empty;
        set
        {
            SelectedFix.FilesToDelete = value.SplitSemicolonSeparatedString();
            OnPropertyChanged();
        }
    }

    public string SelectedFixFilesToBackup
    {
        get => SelectedFix.FilesToBackup is not null ? string.Join(';', SelectedFix.FilesToBackup) : string.Empty;
        set
        {
            SelectedFix.FilesToBackup = value.SplitSemicolonSeparatedString();
            OnPropertyChanged();
        }
    }

    public string SelectedFixFilesToPatch
    {
        get => SelectedFix.FilesToPatch is not null ? string.Join(';', SelectedFix.FilesToPatch) : string.Empty;
        set
        {
            SelectedFix.FilesToPatch = value.SplitSemicolonSeparatedString();
            OnPropertyChanged();
        }
    }

    public string SelectedFixRunAfterInstall
    {
        get => SelectedFix.RunAfterInstall ?? string.Empty;
        set
        {
            SelectedFix.RunAfterInstall = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
        }
    }

    public string SelectedFixWineDllsOverrides
    {
        get => SelectedFix.WineDllOverrides is not null ? string.Join(';', SelectedFix.WineDllOverrides) : string.Empty;
        set
        {
            SelectedFix.WineDllOverrides = value.SplitSemicolonSeparatedString();
            OnPropertyChanged();
        }
    }

    public BaseFixEntity? SelectedSharedFix
    {
        get
        {
            var sharedFixGuid = SelectedFix.SharedFixGuid is null ? null : SelectedFix.SharedFixGuid;

            if (sharedFixGuid is null)
            {
                return null;
            }

            var sharedFix = SharedFixesList?.FirstOrDefault(x => x.Guid == sharedFixGuid);

            return sharedFix;
        }

        set
        {
            SelectedFix.SharedFixGuid = value?.Guid;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSharedFixSelected));
            ResetSelectedSharedFixCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsSharedFixSelected => SelectedSharedFix is not null;

    public string? SelectedSharedFixInstallFolder
    {
        get => SelectedFix.SharedFixInstallFolder is null ? null : SelectedFix.SharedFixInstallFolder;
        set
        {
            SelectedFix.SharedFixInstallFolder = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
        }
    }

    public string? SelectedFixFileSize
    {
        get => SelectedFix.FileSize < 1 ? string.Empty : SelectedFix.FileSize.ToString();
        set
        {
            SelectedFix.FileSize = long.TryParse(value, out var size) ? size : 0;
            OnPropertyChanged();
        }
    }

    public string? SelectedFixMD5
    {
        get => SelectedFix.MD5;
        set
        {
            SelectedFix.MD5 = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
        }
    }


    public FileFixViewModel(
        FileFixEntity fix,
        IFixesProvider fixesProvider,
        PopupEditorViewModel popupEditor
        )
    {
        SelectedFix = fix;
        _fixesProvider = fixesProvider;
        _popupEditor = popupEditor;
        SharedFixesList = _fixesProvider.SharedFixes is null ? null : [.. _fixesProvider.SharedFixes];
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

        var result = await _popupEditor.ShowAndGetResultAsync("Files to delete", fileFix.FilesToDelete ?? []).ConfigureAwait(true);

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

        var result = await _popupEditor.ShowAndGetResultAsync("Files to backup", fileFix.FilesToBackup ?? []).ConfigureAwait(true);

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

        var result = await _popupEditor.ShowAndGetResultAsync("Files to patch", fileFix.FilesToPatch ?? []).ConfigureAwait(true);

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

        var result = await _popupEditor.ShowAndGetResultAsync("Wine DLL overrides", fileFix.WineDllOverrides ?? []).ConfigureAwait(true);

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

        var result = await _popupEditor.ShowAndGetResultAsync("Fix variants", fileFix.Variants ?? []).ConfigureAwait(true);

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
