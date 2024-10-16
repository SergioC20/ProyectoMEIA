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
        private TextBlock userInfoBlock;

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

            if (modifyInfoButton != null) modifyInfoButton.Click += ModifyInfoButton_Click;
            if (exitButton != null) exitButton.Click += ExitButton_Click;
        }

        private void LoadUserInfo()
        {
            string[] lines = File.ReadAllLines(UsersFilePath);
            var userInfo = lines.FirstOrDefault(line => line.StartsWith(username))?.Split(';');

            if (userInfo != null)
            {
                string role = userInfo[4].Trim() == "1" ? "Administrador" : "Usuario";
                userInfoBlock.Text = $"Usuario: {userInfo[0].Trim()}\n" +
                                     $"Nombre: {userInfo[1].Trim()}\n" +
                                     $"Apellido: {userInfo[2].Trim()}\n" +
                                     $"Rol: {role}\n" +
                                     $"Teléfono: {userInfo[6].Trim()}";
            }
        }

        private void ModifyInfoButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditUserInfoWindow(username);
            editWindow.ShowDialog(this);
            // Recargar la información del usuario después de la edición
            LoadUserInfo();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}