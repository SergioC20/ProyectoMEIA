using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EjemploLogin
{
    internal class Program
    {
        static int RECORD_SIZE = 135; // Tamaño estimado de los campos

        // Obtener el registro del archivo en una posición específica
        static string GetRecord(string path, int posicion)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                fs.Seek(posicion * RECORD_SIZE, SeekOrigin.Begin);
                byte[] line = reader.ReadBytes(RECORD_SIZE);
                string record = Encoding.ASCII.GetString(line).Trim();
                return record;
            }
        }

        // Método para generar hash MD5 de la contraseña
        static string GetMD5Hash(string input)
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

        // Método para agregar un nuevo usuario con asignación automática de rol y estatus
        static void AddNewUser(string path_enhanced, string user, string nombre, string apellido, string password, DateTime fecha_nacimiento, int telefono)
        {
            string hashedPassword = GetMD5Hash(password);
            string fechaFormatted = fecha_nacimiento.ToString("dd/MM/yyyy");

            // Verificar si el archivo ya tiene usuarios, si no, asignar rol de administrador
            bool isFirstUser = !File.Exists(path_enhanced) || new FileInfo(path_enhanced).Length == 0;
            int rol = isFirstUser ? 1 : 0;  // 1 para Administrador si es el primer usuario, 0 para Usuario en cualquier otro caso
            int estatus = 1; // Estatus siempre activo (1)

            string newRecord = $"{user.PadRight(20)};{nombre.PadRight(30)};{apellido.PadRight(30)};{hashedPassword.PadRight(32)};{rol};{fechaFormatted};{telefono.ToString().PadRight(4)};{estatus}\n";

            using (FileStream fs = new FileStream(path_enhanced, FileMode.Append, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                byte[] recordBytes = Encoding.ASCII.GetBytes(newRecord);
                writer.Write(recordBytes);
            }

            Console.WriteLine("Usuario creado con éxito.");
            Console.WriteLine($"Rol asignado: {(rol == 1 ? "Administrador" : "Usuario")}");
        }

        // Método para validar que el nombre y apellido no contengan números ni caracteres especiales,
        // pero sí permitir acentos y espacios.
        static bool IsValidName(string input)
        {
            return Regex.IsMatch(input, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$");
        }

        // Método para validar que el teléfono solo contenga números
        static bool IsValidPhoneNumber(string input)
        {
            return Regex.IsMatch(input, @"^\d+$");
        }

        // Método para validar el formato de la fecha de nacimiento
        static bool IsValidDate(string input)
        {
            DateTime tempDate;
            return DateTime.TryParseExact(input, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out tempDate);
        }

        // Método para verificar que un campo no exceda el tamaño en bytes permitido
        static bool IsValidByteLength(string input, int maxBytes)
        {
            return Encoding.ASCII.GetByteCount(input) <= maxBytes;
        }

        // Método para validar la fortaleza de la contraseña
        static bool IsStrongPassword(string password)
        {
            int minimumNumericCharacters = 1;
            int minimumSymbolCharacters = 1;
            int preferredPasswordLength = 8;

            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasDigits = password.Count(char.IsDigit) >= minimumNumericCharacters;
            bool hasSymbols = password.Count(c => !char.IsLetterOrDigit(c)) >= minimumSymbolCharacters;
            bool hasRequiredLength = password.Length >= preferredPasswordLength;

            if (!hasUpperCase || !hasLowerCase)
            {
                Console.WriteLine("La contraseña debe contener al menos una letra mayúscula y una letra minúscula.");
            }
            if (!hasDigits)
            {
                Console.WriteLine($"La contraseña debe contener al menos {minimumNumericCharacters} dígito(s).");
            }
            if (!hasSymbols)
            {
                Console.WriteLine($"La contraseña debe contener al menos {minimumSymbolCharacters} símbolo(s).");
            }
            if (!hasRequiredLength)
            {
                Console.WriteLine($"La contraseña debe tener al menos {preferredPasswordLength} caracteres.");
            }

            return hasUpperCase && hasLowerCase && hasDigits && hasSymbols && hasRequiredLength;
        }

        // Método para verificar si el usuario ya existe en el archivo
        static bool IsUserExists(string path, string username)
        {
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    string[] fields = line.Split(';');
                    if (fields[0].Trim().Equals(username, StringComparison.OrdinalIgnoreCase)) // Comparación sin distinción de mayúsculas/minúsculas
                    {
                        return true; // Usuario ya existe
                    }
                }
            }
            return false; // Usuario no existe
        }

        static void Main(string[] args)
        {
            bool usuarioEncontrado = false;
            string path_enhanced = "/Users/alejandrogil/Desktop/Uni/Lenguajes formales /Programs/ProyectoMEIA/user.txt";

            if (File.Exists(path_enhanced))
            {
                // Leer todas las líneas del archivo
                string[] lineas = File.ReadAllLines(path_enhanced);

                Console.WriteLine("Ingrese su usuario y contraseña");
                Console.Write("Usuario: ");
                String user = Console.ReadLine();
                Console.Write("Contraseña: ");
                String password = Console.ReadLine();

                // Verificar si el usuario existe en el archivo
                foreach (var linea in lineas)
                {
                    string[] campos = linea.Split(';');

                    if (campos[0].Trim().Equals(user)) // Comprobar si el usuario coincide
                    {
                        string password_guardado = campos[3].Trim(); // Campo de contraseña
                        if (password_guardado.ToLower().Equals(GetMD5Hash(password).ToLower()))
                        {
                            usuarioEncontrado = true;
                             string nombre = campos[1].Trim(); // Campo de nombre
                            string apellido = campos[2].Trim(); // Campo de apellido
                            int rol = int.Parse(campos[4].Trim()); // Campo de rol
                            string telefono = campos[6].Trim(); // Campo de teléfono
                            Console.WriteLine(user +" "+ nombre + " "+ apellido + " "+ (rol == 1 ? "Administrador" : "Usuario") + " "+ telefono );
                            
                            break; // Salir del bucle si se encontró el usuario
                        }
                    }
                }

                if (!usuarioEncontrado)
                {
                    Console.WriteLine("Usuario no encontrado o contraseña incorrecta.");
                    Console.WriteLine("Usuario o contraseña incorrectos. ¿Desea crear uno nuevo? (S/N)");
                    if (Console.ReadLine().ToUpper() == "S")
                    {
                        // Solicitar los nuevos datos del usuario
                        string nuevoUsuario;
                        do
                        {
                            Console.Write("Ingrese un nuevo nombre de usuario: ");
                            nuevoUsuario = Console.ReadLine();

                            // Verificar si el usuario ya existe
                            if (IsUserExists(path_enhanced, nuevoUsuario))
                            {
                                Console.WriteLine("El nombre de usuario ya está en uso. Por favor, elija otro.");
                            }
                            else if (!IsValidByteLength(nuevoUsuario, 20))
                            {
                                Console.WriteLine("El nombre de usuario no puede exceder los 20 bytes.");
                            }
                        } while (IsUserExists(path_enhanced, nuevoUsuario) || !IsValidByteLength(nuevoUsuario, 20));

                        // Validación del nombre
                        string nombre;
                        do
                        {
                            Console.Write("Ingrese su nombre: ");
                            nombre = Console.ReadLine();
                            if (!IsValidName(nombre))
                            {
                                Console.WriteLine("El nombre no puede contener números ni caracteres especiales, excepto acentos.");
                            }
                            else if (!IsValidByteLength(nombre, 30))
                            {
                                Console.WriteLine("El nombre no puede exceder los 30 bytes.");
                            }
                        } while (!IsValidName(nombre) || !IsValidByteLength(nombre, 30));

                        // Validación del apellido
                        string apellido;
                        do
                        {
                            Console.Write("Ingrese su apellido: ");
                            apellido = Console.ReadLine();
                            if (!IsValidName(apellido))
                            {
                                Console.WriteLine("El apellido no puede contener números ni caracteres especiales, excepto acentos.");
                            }
                            else if (!IsValidByteLength(apellido, 30))
                            {
                                Console.WriteLine("El apellido no puede exceder los 30 bytes.");
                            }
                        } while (!IsValidName(apellido) || !IsValidByteLength(apellido, 30));

                        // Validación de la contraseña
                        string nuevaPassword;
                        do
                        {
                            Console.Write("Ingrese una contraseña: ");
                            nuevaPassword = Console.ReadLine();
                        } while (!IsStrongPassword(nuevaPassword));

                        // Validación de la fecha de nacimiento
                        DateTime fecha_nacimiento;
                        string fechaNacimientoInput;
                        do
                        {
                            Console.Write("Ingrese su fecha de nacimiento (dd/MM/yyyy): ");
                            fechaNacimientoInput = Console.ReadLine();
                            if (!IsValidDate(fechaNacimientoInput))
                            {
                                Console.WriteLine("Formato de fecha inválido. Inténtelo de nuevo.");
                            }
                        } while (!IsValidDate(fechaNacimientoInput));
                        fecha_nacimiento = DateTime.ParseExact(fechaNacimientoInput, "dd/MM/yyyy", null);

                        // Validación del teléfono
                        int telefono;
                        string telefonoInput;
                        do
                        {
                            Console.Write("Ingrese su número de teléfono: ");
                            telefonoInput = Console.ReadLine();
                            if (!IsValidPhoneNumber(telefonoInput))
                            {
                                Console.WriteLine("El número de teléfono solo debe contener dígitos.");
                            }
                        } while (!IsValidPhoneNumber(telefonoInput));
                        telefono = int.Parse(telefonoInput);

                        // Crear el nuevo usuario y asignar rol y estatus automáticamente
                        AddNewUser(path_enhanced, nuevoUsuario, nombre, apellido, nuevaPassword, fecha_nacimiento, telefono);
                    }
                }
            }
            else
            {
                Console.WriteLine("El archivo no existe.");
            }
        }
    }
}
