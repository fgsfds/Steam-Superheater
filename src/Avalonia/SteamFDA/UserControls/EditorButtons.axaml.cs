using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;

namespace SteamFDA.UserControls
{
    public partial class EditorButtons : UserControl
    {
        public EditorButtons()
        {
            InitializeComponent();

            //fix for inconsistent combobox width
            GamesComboBox.ItemsPanel = new FuncTemplate<Panel>(new(() => new StackPanel()));
        }
    }
}
