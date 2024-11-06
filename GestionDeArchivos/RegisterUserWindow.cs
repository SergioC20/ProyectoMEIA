using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;

namespace GestionDeArchivos
{
    public partial class RegisterUserWindow : Window
    {
        private const string UsersFilePath = "users.txt";
        private const string DescUserFilePath = "Desc_user.txt";
        private TextBox? usernameBox;
        private TextBox? passwordBox;
        private TextBox? nameBox;
        private TextBox? surnameBox;
        private DatePicker? birthDatePicker;
        private TextBox? phoneBox;
        private TextBlock? messageBlock;

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

        private void RegisterButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (messageBlock == null || usernameBox == null || passwordBox == null || 
                    nameBox == null || surnameBox == null || birthDatePicker == null || phoneBox == null)
                {
                    return;
                }

                string username = usernameBox.Text ?? string.Empty;
                string password = passwordBox.Text ?? string.Empty;
                string name = nameBox.Text ?? string.Empty;
                string surname = surnameBox.Text ?? string.Empty;
                DateTime birthDate = birthDatePicker.SelectedDate?.DateTime ?? DateTime.Now;
                string phone = phoneBox.Text ?? string.Empty;

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

                if (!int.TryParse(phone, out int phoneNumber))
                {
                    messageBlock.Text = "El número de teléfono debe ser un número válido.";
                    return;
                }

                if (AddNewUser(username, name, surname, password, birthDate, phoneNumber))
                {
                    UpdateDescUserFile(name, username);
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
                if (messageBlock != null)
                    messageBlock.Text = $"Error inesperado: {ex.Message}";
            }
        }

        private void UpdateDescUserFile(string symbolicName, string creationUser)
        {
            try
            {
                // Solo actualizar si users.txt existe y no está vacío
                if (!File.Exists(UsersFilePath))
                {
                    return;
                }

                var usersData = File.ReadAllLines(UsersFilePath);
                if (usersData.Length == 0)
                {
                    return;
                }

                var currentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                
                // Contar registros totales y activos/inactivos
                int totalRecords = usersData.Length;
                int activeRecords = usersData.Count(line => line.Split(';')[7].Trim() == "1");
                int inactiveRecords = totalRecords - activeRecords;

                string newDescRecord = $"{symbolicName};{currentDateTime};{creationUser};{currentDateTime};{creationUser};{totalRecords};{activeRecords};{inactiveRecords}";

                // Si es el primer registro, crear el archivo con encabezado
                if (totalRecords == 1)
                {
                    string header = "nombresimbolico;fechadecreacion;usuariocreacion;fechamodificacion;usuariomodificacion;no.registros;registrosactivos;registrosinactivos";
                    File.WriteAllText(DescUserFilePath, header + Environment.NewLine + newDescRecord);
                }
                else if (File.Exists(DescUserFilePath))
                {
                    // Si ya existe, actualizar con la nueva información
                    var descLines = File.ReadAllLines(DescUserFilePath).ToList();
                    
                    // Mantener el encabezado
                    if (descLines.Count > 0)
                    {
                        // Actualizar todos los registros existentes con los nuevos conteos
                        for (int i = 1; i < descLines.Count; i++)
                        {
                            var fields = descLines[i].Split(';');
                            fields[5] = totalRecords.ToString();
                            fields[6] = activeRecords.ToString();
                            fields[7] = inactiveRecords.ToString();
                            descLines[i] = string.Join(";", fields);
                        }
                        
                        // Agregar el nuevo registro
                        descLines.Add(newDescRecord);
                        
                        // Escribir todo de vuelta al archivo
                        File.WriteAllLines(DescUserFilePath, descLines);
                    }
                }
            }
            catch (Exception ex)
            {
                if (messageBlock != null)
                    messageBlock.Text = $"Error al actualizar el archivo de descripción: {ex.Message}";
            }
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearRegistrationFields()
        {
            if (usernameBox != null) usernameBox.Text = string.Empty;
            if (passwordBox != null) passwordBox.Text = string.Empty;
            if (nameBox != null) nameBox.Text = string.Empty;
            if (surnameBox != null) surnameBox.Text = string.Empty;
            if (birthDatePicker != null) birthDatePicker.SelectedDate = null;
            if (phoneBox != null) phoneBox.Text = string.Empty;
        }

        private bool AddNewUser(string user, string name, string surname, string password, DateTime birthDate, int phone)
        {
            try
            {
                if (IsUserExists(user)) return false;

                string hashedPassword = GetMD5Hash(password);
                string formattedDate = birthDate.ToString("dd/MM/yyyy");

                bool isFirstUser = !File.Exists(UsersFilePath) || new FileInfo(UsersFilePath).Length == 0;
                int role = 0; // Siempre 0 para nuevos usuarios registrados por el administrador
                int status = 1;

                string newRecord = $"{user.PadRight(20)};{name.PadRight(30)};{surname.PadRight(30)};{hashedPassword.PadRight(32)};{role};{formattedDate};{phone.ToString().PadRight(4)};{status}\n";

                if (!File.Exists(UsersFilePath))
                {
                    File.WriteAllText(UsersFilePath, string.Empty);
                }

                File.AppendAllText(UsersFilePath, newRecord);
                return true;
            }
            catch (Exception ex)
            {
                if (messageBlock != null)
                    messageBlock.Text = $"Error al agregar nuevo usuario: {ex.Message}";
                return false;
            }
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