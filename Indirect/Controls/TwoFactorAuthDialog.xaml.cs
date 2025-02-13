﻿using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Indirect.Controls
{
    public sealed partial class TwoFactorAuthDialog : ContentDialog, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _errorMessage = string.Empty;

        private string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            }
        }

        public TwoFactorAuthDialog()
        {
            this.InitializeComponent();
        }

        private async void ConfirmSecurityCode(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (CodeBox.Text.Length < 6)
            {
                args.Cancel = true;
                ErrorMessage = "Please enter a valid security code";
                return;
            }
            var deferral = args.GetDeferral();
            this.IsPrimaryButtonEnabled = false;
            var result = await ((App)App.Current).ViewModel.InstaApi.LoginWithTwoFactorAsync(CodeBox.Text);
            if (!result.IsSucceeded)
            {
                args.Cancel = true;
                ErrorMessage = result.Message;
            }
            this.IsPrimaryButtonEnabled = true;
            deferral.Complete();
        }

        private void TwoFactorAuthDialog_OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            ErrorMessage = string.Empty;
        }

        private void CodeBox_OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if (string.IsNullOrEmpty(sender.Text) || !args.IsContentChanging) return;
            sender.Text = new string(sender.Text.Where(c => '0' <= c && c <= '9').ToArray());
            sender.SelectionStart = sender.Text.Length;
        }
    }
}
