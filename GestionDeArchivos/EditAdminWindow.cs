using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GestionDeArchivos
{
    public partial class EditAdminInfoWindow : Window
    {
        private const string UsersFilePath = "users.txt";
        private string username;
        private TextBox passwordBox;
        private TextBox phoneBox;
        private DatePicker birthDatePicker;
        private TextBlock messageBlock;
        private string[] userInfo;

        public EditAdminInfoWindow(string username)
        {
            this.username = username;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            LoadUserInfo();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            passwordBox = this.FindControl<TextBox>("PasswordBox");
            phoneBox = this.FindControl<TextBox>("PhoneBox");
            birthDatePicker = this.FindControl<DatePicker>("BirthDatePicker");
            messageBlock = this.FindControl<TextBlock>("MessageBlock");

            var saveButton = this.FindControl<Button>("SaveButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            if (saveButton != null) saveButton.Click += SaveButton_Click;
            if (cancelButton != null) cancelButton.Click += CancelButton_Click;
        }

        private void LoadUserInfo()
        {
            string[] lines = File.ReadAllLines(UsersFilePath);
            userInfo = lines.FirstOrDefault(line => line.StartsWith(username))?.Split(';');

            if (userInfo != null)
            {
                phoneBox.Text = userInfo[6].Trim();
                birthDatePicker.SelectedDate = DateTime.ParseExact(userInfo[5], "dd/MM/yyyy", null);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (userInfo == null)
            {
                messageBlock.Text = "Error: No se pudo cargar la información del usuario.";
                return;
            }

            // Actualizar la información del administrador
            if (!string.IsNullOrEmpty(passwordBox.Text))
            {
                userInfo[3] = GetMD5Hash(passwordBox.Text).PadRight(32);
            }
            userInfo[6] = phoneBox.Text.PadRight(4);
            userInfo[5] = birthDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? userInfo[5];

            // Reconstruir la línea del usuario
            string updatedUserLine = string.Join(";", userInfo);

            // Actualizar el archivo
            string[] lines = File.ReadAllLines(UsersFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(username))
                {
                    lines[i] = updatedUserLine;
                    break;
                }
            }

            File.WriteAllLines(UsersFilePath, lines);

            messageBlock.Text = "Cambios guardados exitosamente.";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GetMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }
    }
}