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

        // Método para agregar un nuevo usuario con validaciones
        static void AddNewUser(string path_enhanced, string user, string nombre, string apellido, string password, bool rol, DateTime fecha_nacimiento, int telefono, bool estatus)
        {
            string hashedPassword = GetMD5Hash(password);
            string fechaFormatted = fecha_nacimiento.ToString("dd/MM/yyyy");
            string newRecord = $"{user.PadRight(20)};{nombre.PadRight(30)};{apellido.PadRight(30)};{hashedPassword.PadRight(32)};{(rol ? 1 : 0)};{fechaFormatted};{telefono.ToString().PadRight(4)};{(estatus ? 1 : 0)}\n";

            using (FileStream fs = new FileStream(path_enhanced, FileMode.Append, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                byte[] recordBytes = Encoding.ASCII.GetBytes(newRecord);
                writer.Write(recordBytes);
            }

            Console.WriteLine("Usuario creado con éxito.");
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

        // Método para verificar que el rol y el estatus solo sean 0 o 1
        static bool IsValidRoleOrStatus(string input)
        {
            return input == "0" || input == "1";
        }

        // Método para verificar que un campo no exceda el tamaño en bytes permitido
        static bool IsValidByteLength(string input, int maxBytes)
        {
            return Encoding.ASCII.GetByteCount(input) <= maxBytes;
        }

        static void Main(string[] args)
        {
            string[] lineas = { };
            bool usuarioEncontrado = false;

            string path_enhanced = "C:\\MEIA\\user.txt";

            Console.WriteLine("Ingrese su usuario y contraseña");
            Console.Write("Usuario: ");
            String user = Console.ReadLine();
            Console.Write("Contraseña: ");
            String password = Console.ReadLine();

              for (int i = 0; i < lineas.Length; i++)
            {
                string[] campos = lineas[i].Split(';');
                if (campos[0].Trim().Equals(user))
                {
                    int index_enhanced_file = Convert.ToInt32(campos[1]);
                    string registro = GetRecord(path_enhanced, index_enhanced_file);
                    string name = registro.Split(';')[0].Trim();
                    string password_guardado = registro.Split(';')[3].Trim();

                    if (password_guardado.ToLower().Equals(GetMD5Hash(password).ToLower()))
                    {
                        usuarioEncontrado = true;
                        Console.WriteLine("Bienvenido " + user);
                    }
                }
            }

            // Validaciones y creación de usuario si no existe
            Console.WriteLine("Usuario no encontrado. ¿Desea crear uno nuevo? (S/N)");
            if (Console.ReadLine().ToUpper() == "S")
            {
                // Solicitar los nuevos datos del usuario

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

                // Validación del rol
                bool rol;
                string rolInput;
                do
                {
                    Console.Write("Ingrese su rol (1 para Admin, 0 para Usuario): ");
                    rolInput = Console.ReadLine();
                    if (!IsValidRoleOrStatus(rolInput))
                    {
                        Console.WriteLine("El rol debe ser 1 (Admin) o 0 (Usuario).");
                    }
                } while (!IsValidRoleOrStatus(rolInput));
                rol = rolInput == "1";

                // Validación de la fecha de nacimiento
                DateTime fecha_nacimiento;
                string fechaNacimientoInput;
                do
                {
                    Console.Write("Ingrese su fecha de nacimiento (dd/mm/aaaa): ");
                    fechaNacimientoInput = Console.ReadLine();
                    if (!IsValidDate(fechaNacimientoInput))
                    {
                        Console.WriteLine("La fecha debe estar en formato dd/mm/aaaa.");
                    }
                } while (!IsValidDate(fechaNacimientoInput));
                fecha_nacimiento = DateTime.ParseExact(fechaNacimientoInput, "dd/MM/yyyy", null);

                // Validación del teléfono (máximo 4 bytes)
                int telefono;
                string telefonoInput;
                do
                {
                    Console.Write("Ingrese su número de teléfono (máximo 9999): ");
                    telefonoInput = Console.ReadLine();
                    if (!IsValidPhoneNumber(telefonoInput))
                    {
                        Console.WriteLine("El teléfono solo debe contener números.");
                    }
                    else if (!IsValidByteLength(telefonoInput, 4))
                    {
                        Console.WriteLine("El número de teléfono no puede exceder los 4 bytes (máximo 9999).");
                    }
                } while (!IsValidPhoneNumber(telefonoInput) || !IsValidByteLength(telefonoInput, 4));
                telefono = Convert.ToInt32(telefonoInput);

                // Validación del estatus
                bool estatus;
                string estatusInput;
                do
                {
                    Console.Write("Ingrese su estatus (1 para activo, 0 para inactivo): ");
                    estatusInput = Console.ReadLine();
                    if (!IsValidRoleOrStatus(estatusInput))
                    {
                        Console.WriteLine("El estatus debe ser 1 (Activo) o 0 (Inactivo).");
                    }
                } while (!IsValidRoleOrStatus(estatusInput));
                estatus = estatusInput == "1";

                // Validación del tamaño del usuario (máximo 20 bytes)
                if (!IsValidByteLength(user, 20))
                {
                    Console.WriteLine("El nombre de usuario no puede exceder los 20 bytes.");
                }
                else
                {
                    // Crear el usuario
                    AddNewUser(path_enhanced, user, nombre, apellido, password, rol, fecha_nacimiento, telefono, estatus);
                }
            }
            else
            {
                Console.WriteLine("Operación cancelada.");
            }

            Console.ReadKey();
        }
    }
}
