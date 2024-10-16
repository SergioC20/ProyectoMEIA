using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace GestionDeArchivos
{
    public partial class RegisterUserWindow : Window
    {
        private const string UsersFilePath = "users.txt";
        private TextBox usernameBox;
        private TextBox passwordBox;
        private TextBox nameBox;
        private TextBox surnameBox;
        private DatePicker birthDatePicker;
        private TextBox phoneBox;
        private TextBlock messageBlock;

        public RegisterUserWindow()
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
            passwordBox = this.FindControl<TextBox>("PasswordBox");
            nameBox = this.FindControl<TextBox>("NameBox");
            surnameBox = this.FindControl<TextBox>("SurnameBox");
            birthDatePicker = this.FindControl<DatePicker>("BirthDatePicker");
            phoneBox = this.FindControl<TextBox>("PhoneBox");
            messageBlock = this.FindControl<TextBlock>("MessageBlock");

            var registerButton = this.FindControl<Button>("RegisterButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            if (registerButton != null) registerButton.Click += RegisterButton_Click;
            if (cancelButton != null) cancelButton.Click += CancelButton_Click;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = usernameBox.Text;
                string password = passwordBox.Text;
                string name = nameBox.Text;
                string surname = surnameBox.Text;
                DateTime birthDate = birthDatePicker.SelectedDate?.DateTime ?? DateTime.Now;
                string phone = phoneBox.Text;

                StringBuilder errorMessage = new StringBuilder();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname) ||
                    string.IsNullOrWhiteSpace(phone))
                {
                    errorMessage.AppendLine("Por favor, complete todos los campos.");
                }

                if (!IsValidName(name) || !IsValidName(surname))
                {
                    errorMessage.AppendLine("El nombre y apellido solo deben contener letras y espacios.");
                }

                if (!IsValidPhoneNumber(phone))
                {
                    errorMessage.AppendLine("Número de teléfono inválido.");
                }

                string passwordErrorMessage = IsStrongPassword(password);
                if (!string.IsNullOrEmpty(passwordErrorMessage))
                {
                    errorMessage.Append(passwordErrorMessage);
                }

                if (errorMessage.Length > 0)
                {
                    messageBlock.Text = errorMessage.ToString();
                    return;
                }

                if (AddNewUser(username, name, surname, password, birthDate, int.Parse(phone)))
                {
                    messageBlock.Text = "Usuario registrado exitosamente!";
                    ClearRegistrationFields();
                }
                else
                {
                    messageBlock.Text = "Error al registrar usuario. El nombre de usuario podría ya existir.";
                }
            }
            catch (Exception ex)
            {
                messageBlock.Text = $"Error inesperado: {ex.Message}";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearRegistrationFields()
        {
            usernameBox.Text = string.Empty;
            passwordBox.Text = string.Empty;
            nameBox.Text = string.Empty;
            surnameBox.Text = string.Empty;
            birthDatePicker.SelectedDate = null;
            phoneBox.Text = string.Empty;
        }

        private bool AddNewUser(string user, string name, string surname, string password, DateTime birthDate, int phone)
        {
            if (IsUserExists(user)) return false;

            string hashedPassword = GetMD5Hash(password);
            string formattedDate = birthDate.ToString("dd/MM/yyyy");

            bool isFirstUser = !File.Exists(UsersFilePath) || new FileInfo(UsersFilePath).Length == 0;
            int role = 0; // Siempre 0 para nuevos usuarios registrados por el administrador
            int status = 1;

            string newRecord = $"{user.PadRight(20)};{name.PadRight(30)};{surname.PadRight(30)};{hashedPassword.PadRight(32)};{role};{formattedDate};{phone.ToString().PadRight(4)};{status}\n";

            File.AppendAllText(UsersFilePath, newRecord);
            return true;
        }

        private bool IsUserExists(string username)
        {
            if (!File.Exists(UsersFilePath)) return false;

            string[] lines = File.ReadAllLines(UsersFilePath);
            foreach (var line in lines)
            {
                string[] fields = line.Split(';');
                if (fields[0].Trim().Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
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

        private bool IsValidName(string input)
        {
            return Regex.IsMatch(input, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$");
        }

        private bool IsValidPhoneNumber(string input)
        {
            return Regex.IsMatch(input, @"^\d+$");
        }

        private string IsStrongPassword(string password)
        {
            StringBuilder errorMessage = new StringBuilder();

            if (password.Length < 8)
            {
                errorMessage.AppendLine("La contraseña debe tener al menos 8 caracteres.");
            }
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errorMessage.AppendLine("La contraseña debe contener al menos una letra mayúscula.");
            }
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errorMessage.AppendLine("La contraseña debe contener al menos una letra minúscula.");
            }
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errorMessage.AppendLine("La contraseña debe contener al menos un número.");
            }
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?:{}|<>]"))
            {
                errorMessage.AppendLine("La contraseña debe contener al menos un carácter especial.");
            }

            return errorMessage.ToString();
        }
    }
}