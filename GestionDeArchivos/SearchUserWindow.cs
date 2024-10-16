using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Linq;

namespace GestionDeArchivos
{
    public partial class SearchUserWindow : Window
    {
        private const string UsersFilePath = "users.txt";
        private TextBox usernameBox;
        private TextBlock resultBlock;
        private Button editButton;

        public SearchUserWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            usernameBox = this.FindControl<TextBox>("UsernameBox");
            resultBlock = this.FindControl<TextBlock>("ResultBlock");
            editButton = this.FindControl<Button>("EditButton");

            var searchButton = this.FindControl<Button>("SearchButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            if (searchButton != null) searchButton.Click += SearchButton_Click;
            if (editButton != null) editButton.Click += EditButton_Click;
            if (cancelButton != null) cancelButton.Click += CancelButton_Click;

            editButton.IsVisible = false;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameBox.Text;
            if (string.IsNullOrWhiteSpace(username))
            {
                resultBlock.Text = "Por favor, ingrese un nombre de usuario.";
                return;
            }

            string[] lines = File.ReadAllLines(UsersFilePath);
            var userInfo = lines.FirstOrDefault(line => line.StartsWith(username));

            if (userInfo != null)
            {
                resultBlock.Text = "Usuario encontrado.";
                editButton.IsVisible = true;
            }
            else
            {
                resultBlock.Text = "El usuario no existe.";
                editButton.IsVisible = false;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditUserInfoWindow(usernameBox.Text);
            editWindow.ShowDialog(this);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}