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
    public partial class EditUserInfoWindow : Window
    {
        private const string UsersFilePath = "users.txt";
        private string username;
        private TextBox phoneBox;
        private TextBox passwordBox;
        private DatePicker birthDatePicker;
        private ComboBox statusComboBox;
        private TextBlock messageBlock;
        private string[] userInfo;

        public EditUserInfoWindow(string username)
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

            phoneBox = this.FindControl<TextBox>("PhoneBox");
            passwordBox = this.FindControl<TextBox>("PasswordBox");
            birthDatePicker = this.FindControl<DatePicker>("BirthDatePicker");
            statusComboBox = this.FindControl<ComboBox>("StatusComboBox");
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
                statusComboBox.SelectedIndex = int.Parse(userInfo[7]) == 1 ? 0 : 1; // 1 es activo (índice 0), 0 es inactivo (índice 1)
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (userInfo == null)
            {
                messageBlock.Text = "Error: No se pudo cargar la información del usuario.";
                return;
            }

            // Actualizar la información del usuario
            userInfo[6] = phoneBox.Text.PadRight(4);
            if (!string.IsNullOrEmpty(passwordBox.Text))
            {
                userInfo[3] = GetMD5Hash(passwordBox.Text).PadRight(32);
            }
            userInfo[5] = birthDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? userInfo[5];
            userInfo[7] = (statusComboBox.SelectedIndex == 0 ? 1 : 0).ToString(); // Si el índice es 0, guardamos 1 (activo), si no, guardamos 0 (inactivo)

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