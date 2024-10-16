using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text;

namespace GestionDeArchivos
{
    public partial class BackupWindow : Window
    {
        private const string UsersFilePath = "users.txt";
        private const string BitacoraFilePath = "bitacora_backup.txt";
        private TextBox pathBox;
        private TextBlock messageBlock;
        private string adminUsername;

        public BackupWindow(string adminUsername)
        {
            this.adminUsername = adminUsername;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            pathBox = this.FindControl<TextBox>("PathBox");
            messageBlock = this.FindControl<TextBlock>("MessageBlock");

            var selectPathButton = this.FindControl<Button>("SelectPathButton");
            var backupButton = this.FindControl<Button>("BackupButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            if (selectPathButton != null) selectPathButton.Click += SelectPathButton_Click;
            if (backupButton != null) backupButton.Click += BackupButton_Click;
            if (cancelButton != null) cancelButton.Click += CancelButton_Click;
        }

        private async void SelectPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                pathBox.Text = result;
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(pathBox.Text))
            {
                messageBlock.Text = "Por favor, seleccione una ruta para el backup.";
                return;
            }

            try
            {
                string backupPath = Path.Combine(pathBox.Text, "backup.txt");
                File.Copy(UsersFilePath, backupPath, true);

                string bitacoraEntry = $"{backupPath};{adminUsername};{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                File.AppendAllText(BitacoraFilePath, bitacoraEntry);

                messageBlock.Text = $"Backup realizado exitosamente en: {backupPath}";
            }
            catch (Exception ex)
            {
                messageBlock.Text = $"Error al realizar el backup: {ex.Message}";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}