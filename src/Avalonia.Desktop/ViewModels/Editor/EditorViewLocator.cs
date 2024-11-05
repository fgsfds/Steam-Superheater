using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Desktop.UserControls.Editor;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.Desktop.ViewModels.Editor;

public class EditorViewLocator : IDataTemplate
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

        return ThrowHelper.ThrowNotSupportedException<Control>();
    }

    public bool Match(object? data)
    {
        return data is ObservableObject;
    }
}