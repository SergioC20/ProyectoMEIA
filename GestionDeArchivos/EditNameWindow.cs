using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace GestionDeArchivos
{
    public partial class EditNameWindow : Window
    {
        private TextBox? newNameBox;
        public string? NewName { get; private set; }

        public EditNameWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            newNameBox = this.FindControl<TextBox>("NewNameBox");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var saveButton = this.FindControl<Button>("SaveButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            if (saveButton != null) saveButton.Click += SaveButton_Click;
            if (cancelButton != null) cancelButton.Click += CancelButton_Click;
        }

        private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NewName = newNameBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(NewName))
            {
                ShowMessage("Error", "El nombre no puede estar vacío.");
                return;
            }
            Close(true);
        }

        private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close(false);
        }

        private void ShowMessage(string title, string message)
        {
            var messageBox = new Window
            {
                Title = title,
                Width = 250,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var messageBlock = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            okButton.Click += (s, e) => messageBox.Close();

            panel.Children.Add(messageBlock);
            panel.Children.Add(okButton);
            messageBox.Content = panel;

            messageBox.ShowDialog(this);
        }
    }
}