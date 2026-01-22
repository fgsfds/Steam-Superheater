using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Desktop.UserControls.Editor;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.Desktop.ViewModels.Editor;

public sealed class EditorViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is FileFixViewModel)
        {
            return new EditorFileFixControl();
        }

        if (data is RegFixViewModel)
        {
            return new EditorRegFixControl();
        }

        throw new NotSupportedException();
    }

    public bool Match(object? data)
    {
        return data is ObservableObject;
    }
}