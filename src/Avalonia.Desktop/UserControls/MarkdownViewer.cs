using Markdown.Avalonia;
using Markdown.Avalonia.Html;
using Markdown.Avalonia.Plugins;

namespace Avalonia.Desktop.UserControls;

/// <summary>
/// Markdown viewer that also renders raw HTML tags such as <c>&lt;img&gt;</c>.
/// </summary>
public sealed class MarkdownViewer : MarkdownScrollViewer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownViewer" /> class.
    /// </summary>
    public MarkdownViewer()
    {
        var plugins = new MdAvPlugins();
        plugins.Plugins.Add(new HtmlPlugin());

        Plugins = plugins;
    }
}
