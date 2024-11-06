using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GestionDeArchivos
{
    public partial class AdminMenuWindow : Window
    {
        private string username;
        private const string UsersFilePath = "users.txt";
        private const string ContactsFilePath = "contacts.txt";
        private TextBlock userInfoBlock;
        private Button addContactButton;
        private string selectedUser;

        public AdminMenuWindow(string username)
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
            addContactButton = this.FindControl<Button>("AddContactButton");
            
            var modifyInfoButton = this.FindControl<Button>("ModifyInfoButton");
            var searchUserButton = this.FindControl<Button>("SearchUserButton");
            var addUserButton = this.FindControl<Button>("AddUserButton");
            var backupButton = this.FindControl<Button>("BackupButton");
            var exitButton = this.FindControl<Button>("ExitButton");

            if (modifyInfoButton != null) modifyInfoButton.Click += ModifyInfoButton_Click;
            if (searchUserButton != null) searchUserButton.Click += SearchUserButton_Click;
            if (addUserButton != null) addUserButton.Click += AddUserButton_Click;
            if (backupButton != null) backupButton.Click += BackupButton_Click;
            if (exitButton != null) exitButton.Click += ExitButton_Click;
            if (addContactButton != null)
            {
                addContactButton.Click += AddContactButton_Click;
                // Removemos esta línea que deshabilitaba el botón
                // addContactButton.IsEnabled = false;
            }
        }

        // El resto del código permanece igual
        private void LoadUserInfo()
        {
            string[] lines = File.ReadAllLines(UsersFilePath);
            var userInfo = lines.FirstOrDefault(line => line.StartsWith(username))?.Split(';');
            if (userInfo != null)
            {
                string role = userInfo[4].Trim() == "1" ? "Administrador" : "Usuario";
                userInfoBlock.Text = $"Usuario: {userInfo[0].Trim()}; " +
                                   $"Nombre: {userInfo[1].Trim()}; " +
                                   $"Apellido: {userInfo[2].Trim()}; " +
                                   $"Rol: {role}; " +
                                   $"Teléfono: {userInfo[6].Trim()}";
            }
        }

        public void SearchUser(string searchTerm)
        {
            string[] lines = File.ReadAllLines(UsersFilePath);
            var matchingUser = lines.FirstOrDefault(line =>
            {
                var fields = line.Split(';');
                return fields[0].Trim().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       fields[1].Trim().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       fields[2].Trim().Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            });

            if (matchingUser != null)
            {
                var userInfo = matchingUser.Split(';');
                selectedUser = userInfo[0].Trim();
                // También removemos esta línea ya que no necesitamos habilitar el botón aquí
                // addContactButton.IsEnabled = true;
            }
            else
            {
                selectedUser = null;
                // Y removemos esta línea que lo deshabilitaba
                // addContactButton.IsEnabled = false;
            }
        }

        private void AddContactButton_Click(object sender, RoutedEventArgs e)
        {
            var addContactWindow = new AddContactWindow(username);
            addContactWindow.ShowDialog(this);
        }

        private async Task ShowMessage(string title, string message)
        {
            var msgBox = new Window()
            {
                Title = title,
                Width = 250,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var panel = new StackPanel()
            {
                Margin = new Thickness(10)
            };

            var messageBlock = new TextBlock()
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var okButton = new Button()
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            okButton.Click += (s, e) => msgBox.Close();
            
            panel.Children.Add(messageBlock);
            panel.Children.Add(okButton);
            msgBox.Content = panel;

            await msgBox.ShowDialog(this);
        }

        private void ModifyInfoButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditAdminInfoWindow(username);
            editWindow.ShowDialog(this);
            LoadUserInfo();
        }

        private void SearchUserButton_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new SearchUserWindow();
            searchWindow.ShowDialog(this);
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterUserWindow();
            registerWindow.ShowDialog(this);
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            var backupWindow = new BackupWindow(username);
            backupWindow.ShowDialog(this);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}