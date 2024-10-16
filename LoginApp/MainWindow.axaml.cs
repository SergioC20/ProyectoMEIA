using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Interactivity;

namespace LoginApp
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
            var showRegisterButton = this.FindControl<Button>("ShowRegisterButton");
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

            if (AuthenticateUser(username, password))
            {
                if (loginMessageBlock != null) loginMessageBlock.Text = "Login successful!";
            }
            else
            {
                if (loginMessageBlock != null) loginMessageBlock.Text = "Username or password wrong";
            }
        }

        private void ShowRegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            if (loginPanel != null) loginPanel.IsVisible = false;
            if (registerPanel != null) registerPanel.IsVisible = true;
        }

        private void BackToLoginButton_Click(object? sender, RoutedEventArgs e)
        {
            if (loginPanel != null) loginPanel.IsVisible = true;
            if (registerPanel != null) registerPanel.IsVisible = false;
        }

        private void RegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            string username = registerUsernameBox?.Text ?? string.Empty;
            string password = registerPasswordBox?.Text ?? string.Empty;
            string name = nameBox?.Text ?? string.Empty;
            string surname = surnameBox?.Text ?? string.Empty;
            DateTime birthDate = birthDatePicker?.SelectedDate?.DateTime ?? DateTime.Now;
            string phone = phoneBox?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname) ||
                string.IsNullOrWhiteSpace(phone))
            {
                if (registerMessageBlock != null) registerMessageBlock.Text = "Please fill all fields.";
                return;
            }

            if (!IsValidName(name) || !IsValidName(surname))
            {
                if (registerMessageBlock != null) registerMessageBlock.Text = "Name and surname should only contain letters and spaces.";
                return;
            }

            if (!IsValidPhoneNumber(phone))
            {
                if (registerMessageBlock != null) registerMessageBlock.Text = "Invalid phone number.";
                return;
            }

            if (!IsStrongPassword(password))
            {
                return;
            }

            if (AddNewUser(username, name, surname, password, birthDate, int.Parse(phone)))
            {
                if (registerMessageBlock != null) registerMessageBlock.Text = "User registered successfully!";
            }
            else
            {
                if (registerMessageBlock != null) registerMessageBlock.Text = "Error registering user. Username might already exist.";
            }
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

        private bool AddNewUser(string user, string nombre, string apellido, string password, DateTime fecha_nacimiento, int telefono)
        {
            if (IsUserExists(user)) return false;

            string hashedPassword = GetMD5Hash(password);
            string fechaFormatted = fecha_nacimiento.ToString("dd/MM/yyyy");

            bool isFirstUser = !File.Exists(UsersFilePath) || new FileInfo(UsersFilePath).Length == 0;
            int rol = isFirstUser ? 1 : 0;
            int estatus = 1;

            string newRecord = $"{user.PadRight(20)};{nombre.PadRight(30)};{apellido.PadRight(30)};{hashedPassword.PadRight(32)};{rol};{fechaFormatted};{telefono.ToString().PadRight(4)};{estatus}\n";

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

        private bool IsStrongPassword(string password)
        {
            bool isStrong = true;
            StringBuilder errorMessage = new StringBuilder();

            if (password.Length < 8)
            {
                errorMessage.AppendLine("Password should be at least 8 characters long.");
                isStrong = false;
            }
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errorMessage.AppendLine("Password should contain at least one uppercase letter.");
                isStrong = false;
            }
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errorMessage.AppendLine("Password should contain at least one lowercase letter.");
                isStrong = false;
            }
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errorMessage.AppendLine("Password should contain at least one digit.");
                isStrong = false;
            }
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?:{}|<>]"))
            {
                errorMessage.AppendLine("Password should contain at least one special character.");
                isStrong = false;
            }

            if (registerMessageBlock != null) registerMessageBlock.Text = errorMessage.ToString();
            return isStrong;
        }
    }
}