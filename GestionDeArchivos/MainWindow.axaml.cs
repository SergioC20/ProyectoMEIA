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
    public partial class VentanaPrincipal : Window
    {
        private TextBox? cajaUsuarioInicio;
        private TextBox? cajaContrasenaInicio;
        private TextBox? cajaUsuarioRegistro;
        private TextBox? cajaContrasenaRegistro;
        private TextBox? cajaNombre;
        private TextBox? cajaApellido;
        private DatePicker? selectorFechaNacimiento;
        private TextBox? cajaTelefono;
        private TextBlock? bloqueErrorInicio;
        private TextBlock? bloqueErrorRegistro;
        private StackPanel? panelInicio;
        private StackPanel? panelRegistro;

        private const string RutaArchivoUsuarios = "usuarios.txt";
        private const int TamanoRegistro = 135;

        public VentanaPrincipal()
        {
            inicializarComponentes();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void inicializarComponentes()
        {
            AvaloniaXamlLoader.Load(this);

            cajaUsuarioInicio = this.FindControl<TextBox>("CajaUsuarioInicio");
            cajaContrasenaInicio = this.FindControl<TextBox>("CajaContrasenaInicio");
            cajaUsuarioRegistro = this.FindControl<TextBox>("CajaUsuarioRegistro");
            cajaContrasenaRegistro = this.FindControl<TextBox>("CajaContrasenaRegistro");
            cajaNombre = this.FindControl<TextBox>("CajaNombre");
            cajaApellido = this.FindControl<TextBox>("CajaApellido");
            selectorFechaNacimiento = this.FindControl<DatePicker>("SelectorFechaNacimiento");
            cajaTelefono = this.FindControl<TextBox>("CajaTelefono");
            bloqueErrorInicio = this.FindControl<TextBlock>("BloqueErrorInicio");
            bloqueErrorRegistro = this.FindControl<TextBlock>("BloqueErrorRegistro");
            panelInicio = this.FindControl<StackPanel>("PanelInicio");
            panelRegistro = this.FindControl<StackPanel>("PanelRegistro");

            var botonInicio = this.FindControl<Button>("BotonInicio");
            var botonRegistro = this.FindControl<Button>("BotonRegistro");
            var botonMostrarRegistro = this.FindControl<Button>("BotonMostrarRegistro");
            var botonVolverInicio = this.FindControl<Button>("BotonVolverInicio");

            if (botonInicio != null) botonInicio.Click += clickBotonInicio;
            if (botonRegistro != null) botonRegistro.Click += clickBotonRegistro;
            if (botonMostrarRegistro != null) botonMostrarRegistro.Click += clickBotonMostrarRegistro;
            if (botonVolverInicio != null) botonVolverInicio.Click += clickBotonVolverInicio;
        }

        private void clickBotonInicio(object? sender, RoutedEventArgs e)
        {
            string usuario = cajaUsuarioInicio?.Text ?? string.Empty;
            string contrasena = cajaContrasenaInicio?.Text ?? string.Empty;

            if (autenticarUsuario(usuario, contrasena))
            {
                if (bloqueErrorInicio != null) bloqueErrorInicio.Text = "¡Inicio de sesión exitoso!";
            }
            else
            {
                if (bloqueErrorInicio != null) bloqueErrorInicio.Text = "Usuario o contraseña incorrectos";
            }
        }

        private void clickBotonMostrarRegistro(object? sender, RoutedEventArgs e)
        {
            if (panelInicio != null) panelInicio.IsVisible = false;
            if (panelRegistro != null) panelRegistro.IsVisible = true;
        }

        private void clickBotonVolverInicio(object? sender, RoutedEventArgs e)
        {
            if (panelInicio != null) panelInicio.IsVisible = true;
            if (panelRegistro != null) panelRegistro.IsVisible = false;
        }

        private void clickBotonRegistro(object? sender, RoutedEventArgs e)
        {
            string usuario = cajaUsuarioRegistro?.Text ?? string.Empty;
            string contrasena = cajaContrasenaRegistro?.Text ?? string.Empty;
            string nombre = cajaNombre?.Text ?? string.Empty;
            string apellido = cajaApellido?.Text ?? string.Empty;
            DateTime fechaNacimiento = selectorFechaNacimiento?.SelectedDate?.DateTime ?? DateTime.Now;
            string telefono = cajaTelefono?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena) ||
                string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(telefono))
            {
                if (bloqueErrorRegistro != null) bloqueErrorRegistro.Text = "Por favor, complete todos los campos.";
                return;
            }

            if (!esNombreValido(nombre) || !esNombreValido(apellido))
            {
                if (bloqueErrorRegistro != null) bloqueErrorRegistro.Text = "El nombre y apellido solo deben contener letras y espacios.";
                return;
            }

            if (!esNumeroTelefonoValido(telefono))
            {
                if (bloqueErrorRegistro != null) bloqueErrorRegistro.Text = "Número de teléfono inválido.";
                return;
            }

            if (!esContrasenaFuerte(contrasena))
            {
                return;
            }

            if (agregarNuevoUsuario(usuario, nombre, apellido, contrasena, fechaNacimiento, int.Parse(telefono)))
            {
                if (bloqueErrorRegistro != null) bloqueErrorRegistro.Text = "Usuario registrado exitosamente.";
            }
            else
            {
                if (bloqueErrorRegistro != null) bloqueErrorRegistro.Text = "Error al registrar usuario. El nombre de usuario podría ya existir.";
            }
        }

        private bool autenticarUsuario(string usuario, string contrasena)
        {
            if (!File.Exists(RutaArchivoUsuarios)) return false;

            string[] lineas = File.ReadAllLines(RutaArchivoUsuarios);
            foreach (var linea in lineas)
            {
                string[] campos = linea.Split(';');
                if (campos[0].Trim() == usuario && campos[3].Trim() == obtenerHashMD5(contrasena))
                {
                    return true;
                }
            }
            return false;
        }

        private bool agregarNuevoUsuario(string usuario, string nombre, string apellido, string contrasena, DateTime fechaNacimiento, int telefono)
        {
            if (existeUsuario(usuario)) return false;

            string contrasenaEncriptada = obtenerHashMD5(contrasena);
            string fechaFormateada = fechaNacimiento.ToString("dd/MM/yyyy");

            bool esPrimerUsuario = !File.Exists(RutaArchivoUsuarios) || new FileInfo(RutaArchivoUsuarios).Length == 0;
            int rol = esPrimerUsuario ? 1 : 0;
            int estatus = 1;

            string nuevoRegistro = $"{usuario.PadRight(20)};{nombre.PadRight(30)};{apellido.PadRight(30)};{contrasenaEncriptada.PadRight(32)};{rol};{fechaFormateada};{telefono.ToString().PadRight(4)};{estatus}\n";

            File.AppendAllText(RutaArchivoUsuarios, nuevoRegistro);
            return true;
        }

        private bool existeUsuario(string usuario)
        {
            if (!File.Exists(RutaArchivoUsuarios)) return false;

            string[] lineas = File.ReadAllLines(RutaArchivoUsuarios);
            foreach (var linea in lineas)
            {
                string[] campos = linea.Split(';');
                if (campos[0].Trim().Equals(usuario, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private string obtenerHashMD5(string entrada)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] datosEntrada = Encoding.ASCII.GetBytes(entrada);
                byte[] datosHash = md5.ComputeHash(datosEntrada);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < datosHash.Length; i++)
                {
                    sb.Append(datosHash[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }

        private bool esNombreValido(string entrada)
        {
            return Regex.IsMatch(entrada, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$");
        }

        private bool esNumeroTelefonoValido(string entrada)
        {
            return Regex.IsMatch(entrada, @"^\d+$");
        }

        private bool esContrasenaFuerte(string contrasena)
        {
            bool esFuerte = true;
            StringBuilder mensajeError = new StringBuilder();

            if (contrasena.Length < 8)
            {
                mensajeError.AppendLine("La contraseña debe tener al menos 8 caracteres.");
                esFuerte = false;
            }
            if (!Regex.IsMatch(contrasena, @"[A-Z]"))
            {
                mensajeError.AppendLine("La contraseña debe contener al menos una letra mayúscula.");
                esFuerte = false;
            }
            if (!Regex.IsMatch(contrasena, @"[a-z]"))
            {
                mensajeError.AppendLine("La contraseña debe contener al menos una letra minúscula.");
                esFuerte = false;
            }
            if (!Regex.IsMatch(contrasena, @"[0-9]"))
            {
                mensajeError.AppendLine("La contraseña debe contener al menos un dígito.");
                esFuerte = false;
            }
            if (!Regex.IsMatch(contrasena, @"[!@#$%^&*(),.?:{}|<>]"))
            {
                mensajeError.AppendLine("La contraseña debe contener al menos un carácter especial.");
                esFuerte = false;
            }

            if (bloqueErrorRegistro != null) bloqueErrorRegistro.Text = mensajeError.ToString();
            return esFuerte;
        }
    }
}