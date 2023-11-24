using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class PopupEditorViewModel : ObservableObject
    {
        private List<string>? _result;


        #region Binding Properties

        public bool IsPopupVisible { get; private set; }

        public bool IsYesNo { get; private set; }

        public string TitleText { get; private set; }

        public string Text { get; set; }

        #endregion Binding Properties


        #region Relay Commands

        [RelayCommand]
        private void Cancel()
        {
            _result = null;

            IsPopupVisible = false;
            OnPropertyChanged(nameof(IsPopupVisible));
        }

        [RelayCommand]
        private void Save()
        {
            List<string> result = new();
            var list = Text.Split(Environment.NewLine);

            foreach (var item in list)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    result.Add(item);
                }
            }

            _result = result;

            IsPopupVisible = false;
            OnPropertyChanged(nameof(IsPopupVisible));
        }

        #endregion Relay Commands


        /// <summary>
        /// Show popup window and return result
        /// </summary>
        /// <returns>true if Ok or Yes pressed, false if Cancel pressed</returns>
        public List<string>? ShowAndGetResult(string title, List<string>? text)
        {
            TitleText = title;
            OnPropertyChanged(nameof(TitleText));

            string textString = string.Empty;

            if (text is not null)
            {
                textString = string.Join(Environment.NewLine, text);
            }

            Text = textString;
            OnPropertyChanged(nameof(Text));

            IsPopupVisible = true;
            OnPropertyChanged(nameof(IsPopupVisible));

            return _result;
        }
    }
}
