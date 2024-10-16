using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Interactivity;

namespace GestionDeArchivos
{
    public partial class MainWindow : Window
    {
        private TextBox? loginUsernameBox;
        private TextBox? loginPasswordBox;
        private TextBox? registerUsernameBox;
        private TextBox? registerPasswordBox;
        private TextBox? nameBox;
        private TextBox? surnameBox;
        private DatePicker? birthDatePicker;
        private TextBox? phoneBox;
        private TextBlock? loginMessageBlock;
        private TextBlock? registerMessageBlock;
        private StackPanel? loginPanel;
        private StackPanel? registerPanel;
        private Button? showRegisterButton;

        private const string UsersFilePath = "users.txt";
        private const int RecordSize = 135;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            loginUsernameBox = this.FindControl<TextBox>("LoginUsernameBox");
            loginPasswordBox = this.FindControl<TextBox>("LoginPasswordBox");
            registerUsernameBox = this.FindControl<TextBox>("RegisterUsernameBox");
            registerPasswordBox = this.FindControl<TextBox>("RegisterPasswordBox");
            nameBox = this.FindControl<TextBox>("NameBox");
            surnameBox = this.FindControl<TextBox>("SurnameBox");
            birthDatePicker = this.FindControl<DatePicker>("BirthDatePicker");
            phoneBox = this.FindControl<TextBox>("PhoneBox");
            loginMessageBlock = this.FindControl<TextBlock>("LoginMessageBlock");
            registerMessageBlock = this.FindControl<TextBlock>("RegisterMessageBlock");
            loginPanel = this.FindControl<StackPanel>("LoginPanel");
            registerPanel = this.FindControl<StackPanel>("RegisterPanel");

            var loginButton = this.FindControl<Button>("LoginButton");
            var registerButton = this.FindControl<Button>("RegisterButton");
            showRegisterButton = this.FindControl<Button>("ShowRegisterButton");
            var backToLoginButton = this.FindControl<Button>("BackToLoginButton");

            if (loginButton != null) loginButton.Click += LoginButton_Click;
            if (registerButton != null) registerButton.Click += RegisterButton_Click;
            if (showRegisterButton != null) showRegisterButton.Click += ShowRegisterButton_Click;
            if (backToLoginButton != null) backToLoginButton.Click += BackToLoginButton_Click;
        }

        private void LoginButton_Click(object? sender, RoutedEventArgs e)
        {
            string username = loginUsernameBox?.Text ?? string.Empty;
            string password = loginPasswordBox?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                if (loginMessageBlock != null) loginMessageBlock.Text = "Please enter both username and password.";
                return;
            }

            if (!IsUserExists(username))
            {
                if (loginMessageBlock != null) 
                {
                    loginMessageBlock.Text = "Usuario no encontrado, si desea agregarlo presione el\nboton";
                    if (showRegisterButton != null) showRegisterButton.IsVisible = true;
                }
                return;
            }

            if (AuthenticateUser(username, password))
            {
                if (loginMessageBlock != null) loginMessageBlock.Text = "Login successful!";
            }
            else
            {
                if (loginMessageBlock != null) loginMessageBlock.Text = "Incorrect password.";
            }
        }

        private void ShowRegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            if (loginPanel != null) loginPanel.IsVisible = false;
            if (registerPanel != null) registerPanel.IsVisible = true;
            if (showRegisterButton != null) showRegisterButton.IsVisible = false;
        }

        private void BackToLoginButton_Click(object? sender, RoutedEventArgs e)
        {
            if (loginPanel != null) loginPanel.IsVisible = true;
            if (registerPanel != null) registerPanel.IsVisible = false;
            if (showRegisterButton != null) showRegisterButton.IsVisible = false;
            if (loginMessageBlock != null) loginMessageBlock.Text = string.Empty;
        }

        private void RegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            if (registerMessageBlock == null) return;

            try
            {
                string username = registerUsernameBox?.Text ?? string.Empty;
                string password = registerPasswordBox?.Text ?? string.Empty;
                string name = nameBox?.Text ?? string.Empty;
                string surname = surnameBox?.Text ?? string.Empty;
                DateTime birthDate = birthDatePicker?.SelectedDate?.DateTime ?? DateTime.Now;
                string phone = phoneBox?.Text ?? string.Empty;

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
                    registerMessageBlock.Text = errorMessage.ToString();
                    return;
                }

                if (AddNewUser(username, name, surname, password, birthDate, int.Parse(phone)))
                {
                    registerMessageBlock.Text = "Usuario registrado exitosamente!";
                    ClearRegistrationFields();
                }
                else
                {
                    registerMessageBlock.Text = "Error al registrar usuario. El nombre de usuario podría ya existir.";
                }
            }
            catch (Exception ex)
            {
                registerMessageBlock.Text = $"Error inesperado: {ex.Message}";
            }
        }

        private void ClearRegistrationFields()
        {
            if (registerUsernameBox != null) registerUsernameBox.Text = string.Empty;
            if (registerPasswordBox != null) registerPasswordBox.Text = string.Empty;
            if (nameBox != null) nameBox.Text = string.Empty;
            if (surnameBox != null) surnameBox.Text = string.Empty;
            if (birthDatePicker != null) birthDatePicker.SelectedDate = null;
            if (phoneBox != null) phoneBox.Text = string.Empty;
        }

        private bool AuthenticateUser(string username, string password)
        {
            if (!File.Exists(UsersFilePath)) return false;

            string[] lines = File.ReadAllLines(UsersFilePath);
            foreach (var line in lines)
            {
                string[] fields = line.Split(';');
                if (fields[0].Trim() == username && fields[3].Trim() == GetMD5Hash(password))
                {
                    return true;
                }
            }
            return false;
        }

        private bool AddNewUser(string user, string name, string surname, string password, DateTime birthDate, int phone)
        {
            if (IsUserExists(user)) return false;

            string hashedPassword = GetMD5Hash(password);
            string formattedDate = birthDate.ToString("dd/MM/yyyy");

            bool isFirstUser = !File.Exists(UsersFilePath) || new FileInfo(UsersFilePath).Length == 0;
            int role = isFirstUser ? 1 : 0;
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