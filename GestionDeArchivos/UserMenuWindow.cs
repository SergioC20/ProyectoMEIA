using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Linq;

namespace GestionDeArchivos
{
    public partial class UserMenuWindow : Window
    {
        private string username;
        private const string UsersFilePath = "users.txt";
        private TextBlock? userInfoBlock;
        private Button? addContactButton;

        public UserMenuWindow(string username)
        {
            this.username = username;
            InitializeComponent();
            LoadUserInfo();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            userInfoBlock = this.FindControl<TextBlock>("UserInfoBlock");
            var modifyInfoButton = this.FindControl<Button>("ModifyInfoButton");
            var exitButton = this.FindControl<Button>("ExitButton");
            addContactButton = this.FindControl<Button>("AddContactButton");

            if (modifyInfoButton != null) modifyInfoButton.Click += ModifyInfoButton_Click!;
            if (exitButton != null) exitButton.Click += ExitButton_Click!;
            if (addContactButton != null) addContactButton.Click += AddContactButton_Click!;
        }

        private void LoadUserInfo()
        {
            try
            {
                string[] lines = File.ReadAllLines(UsersFilePath);
                var userInfo = lines.FirstOrDefault(line => line.StartsWith(username))?.Split(';');

                if (userInfo != null && userInfoBlock != null)
                {
                    string role = userInfo[4].Trim() == "1" ? "Administrador" : "Usuario";
                    userInfoBlock.Text = $"Usuario: {userInfo[0].Trim()}\n" +
                                       $"Nombre: {userInfo[1].Trim()}\n" +
                                       $"Apellido: {userInfo[2].Trim()}\n" +
                                       $"Rol: {role}\n" +
                                       $"Teléfono: {userInfo[6].Trim()}";
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al cargar información del usuario: {ex.Message}");
            }
        }

        private async void ModifyInfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new EditUserInfoWindow(username);
                await editWindow.ShowDialog(this);
                LoadUserInfo(); // Recargar la información después de la edición
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al abrir ventana de modificación: {ex.Message}");
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void AddContactButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addContactWindow = new AddContactWindow(username);
                await addContactWindow.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al abrir ventana de contactos: {ex.Message}");
            }
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