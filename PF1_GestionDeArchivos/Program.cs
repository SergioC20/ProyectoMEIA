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
                    if (fields[0].Trim().Equals(username)) 
                    {
                        return true; // Usuario ya existe
                    }
                }
            }
            return false; // Usuario no existe
        }



        static void Backup(string path, string user)
        {
            string backupDirectoryName = "MEIA_Backup";
            string backupLogPath = @"C:\MEIA\bitacora_backup.txt";
            string descLogPath = @"C:\MEIA\desc_bitacora_backup.txt";

            Console.WriteLine("Seleccione la ruta para el respaldo:");
            string destinationPath = Console.ReadLine();

            if (!Directory.Exists(destinationPath))
            {
                Console.WriteLine("La ruta ingresada no existe.");
                return;
            }

            string backupFullPath = Path.Combine(destinationPath, backupDirectoryName);

            try
            {
                // Crear directorio de respaldo
                Directory.CreateDirectory(backupFullPath);

                // Copiar todos los archivos de C:\MEIA al directorio de respaldo
                string sourcePath = @"C:\MEIA";
                foreach (string filePath in Directory.GetFiles(sourcePath))
                {
                    string fileName = Path.GetFileName(filePath);
                    string destFilePath = Path.Combine(backupFullPath, fileName);
                    File.Copy(filePath, destFilePath, true);
                }

                // Registrar la operación en bitacora_backup.txt
                LogBackup(backupFullPath, user, backupLogPath);

                // Actualizar descriptor
                UpdateDescriptorBackup(user, descLogPath);

                Console.WriteLine("Respaldo realizado con éxito.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al realizar el respaldo: " + ex.Message);
            }
        }

        static void LogBackup(string backupFullPath, string user, string logPath)
        {
            string logEntry = $"{backupFullPath};{user};{DateTime.Now:dd/MM/yyyy}";

            // Guardar la entrada en la bitácora
            File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }

        static void UpdateDescriptorBackup(string user, string descLogPath)
        {
            string[] descLines = File.Exists(descLogPath) ? File.ReadAllLines(descLogPath) : null;
            int numRegistros = descLines != null ? descLines.Length - 4 : 0; // Suponiendo que hay 4 líneas fijas en el descriptor

            // Actualizar o crear descriptor
            string descriptorContent = 
            "nombre_simbolico: bitacora_backup.txt\n" +
            $"fecha_creacion: {(descLines != null ? descLines[1].Split(':')[1].Trim() : DateTime.Now.ToString("dd/MM/yyyy"))}\n" +
            $"usuario_creacion: {(descLines != null ? descLines[2].Split(':')[1].Trim() : user)}\n" +
            $"fecha_modificacion: {DateTime.Now:dd/MM/yyyy}\n" +
            $"usuario_modificacion: {user}\n" +
            $"#_registros: {numRegistros + 1}\n";

            // Guardar el descriptor actualizado
            File.WriteAllText(descLogPath, descriptorContent);
        }


        static void ModificarInformacionUsuario(string path_enhanced, string username)
        {
            // Leer todas las líneas del archivo
            string[] lineas = File.ReadAllLines(path_enhanced);
            string updatedRecord = string.Empty;

            for (int i = 0; i < lineas.Length; i++)
            {
                string[] campos = lineas[i].Split(';');
                if (campos[0].Trim().Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                        // Si el usuario coincide, solicitar los nuevos datos
                        Console.WriteLine($"Modificando información para: {username}");

                        // Validación y modificación de la contraseña
                        string nuevaContraseña;
                    do
                    {
                        Console.Write("Ingrese una nueva contraseña (deje en blanco para no modificar): ");
                        nuevaContraseña = Console.ReadLine();
                        if (!string.IsNullOrEmpty(nuevaContraseña) && !IsStrongPassword(nuevaContraseña))
                        {
                            Console.WriteLine("La contraseña no cumple con los requisitos de seguridad. Intente de nuevo.");
                        }
                    } while (!string.IsNullOrEmpty(nuevaContraseña) && !IsStrongPassword(nuevaContraseña));

                    if (!string.IsNullOrEmpty(nuevaContraseña))
                    {
                        campos[3] = GetMD5Hash(nuevaContraseña); // Actualizar contraseña
                    }

                    // Validación y modificación de la fecha de nacimiento
                    DateTime fecha_nacimiento;
                    string fechaNacimientoInput;
                    do
                    {
                        Console.Write("Ingrese la nueva fecha de nacimiento (dd/MM/yyyy) (deje en blanco para no modificar): ");
                        fechaNacimientoInput = Console.ReadLine();
                        if (!string.IsNullOrEmpty(fechaNacimientoInput) && !IsValidDate(fechaNacimientoInput))
                        {
                            Console.WriteLine("Formato de fecha inválido. Inténtelo de nuevo.");
                        }
                    } while (!string.IsNullOrEmpty(fechaNacimientoInput) && !IsValidDate(fechaNacimientoInput));

                    if (!string.IsNullOrEmpty(fechaNacimientoInput))
                    {
                        fecha_nacimiento = DateTime.ParseExact(fechaNacimientoInput, "dd/MM/yyyy", null);
                        campos[5] = fecha_nacimiento.ToString("dd/MM/yyyy"); // Actualizar fecha
                    }

                    // Validación y modificación del teléfono
                    string nuevoTelefono;
                    do
                    {
                        Console.Write("Ingrese el nuevo número de teléfono (deje en blanco para no modificar): ");
                        nuevoTelefono = Console.ReadLine();
                        if (!string.IsNullOrEmpty(nuevoTelefono) && (!IsValidPhoneNumber(nuevoTelefono) || !IsValidByteLength(nuevoTelefono, 4)))
                        {
                            Console.WriteLine("El número de teléfono es inválido o tiene una longitud incorrecta.");
                        }
                    } while (!string.IsNullOrEmpty(nuevoTelefono) && (!IsValidPhoneNumber(nuevoTelefono) || !IsValidByteLength(nuevoTelefono, 4)));

                    if (!string.IsNullOrEmpty(nuevoTelefono))
                    {
                        campos[6] = nuevoTelefono; // Actualizar teléfono
                    }

                    // Validación y modificación del estatus
                    string nuevoEstatus;
                    do
                    {
                        Console.Write("Ingrese el nuevo estatus (1 para activo, 0 para inactivo) (deje en blanco para no modificar): ");
                        nuevoEstatus = Console.ReadLine();
                        if (!string.IsNullOrEmpty(nuevoEstatus) && (nuevoEstatus != "0" && nuevoEstatus != "1"))
                        {
                            Console.WriteLine("Estatus inválido. Debe ser 1 para activo o 0 para inactivo.");
                        }
                    } while (!string.IsNullOrEmpty(nuevoEstatus) && (nuevoEstatus != "0" && nuevoEstatus != "1"));

                    if (!string.IsNullOrEmpty(nuevoEstatus))
                    {
                        campos[7] = nuevoEstatus; // Actualizar estatus
                    }

                    // Crear el nuevo registro actualizado
                    updatedRecord = string.Join(";", campos);
                    lineas[i] = updatedRecord; // Reemplazar la línea antigua
                    Console.WriteLine("Información actualizada con éxito.");
                    break; // Salir del bucle después de actualizar
                }
            }

            // Escribir de nuevo todas las líneas en el archivo
            File.WriteAllLines(path_enhanced, lineas);
        }



        static void ModificarInformacionAdministrador(string path_enhanced, string username)
        {
            // Leer todas las líneas del archivo
            string[] lineas = File.ReadAllLines(path_enhanced);
            string updatedRecord = string.Empty;

            for (int i = 0; i < lineas.Length; i++)
            {
                string[] campos = lineas[i].Split(';');
                if (campos[0].Trim().Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                        // Si el usuario coincide, solicitar los nuevos datos
                        Console.WriteLine($"Modificando información para: {username}");

                        // Validación y modificación de la contraseña
                        string nuevaContraseña;
                    do
                    {
                        Console.Write("Ingrese una nueva contraseña (deje en blanco para no modificar): ");
                        nuevaContraseña = Console.ReadLine();
                        if (!string.IsNullOrEmpty(nuevaContraseña) && !IsStrongPassword(nuevaContraseña))
                        {
                            Console.WriteLine("La contraseña no cumple con los requisitos de seguridad. Intente de nuevo.");
                        }
                    } while (!string.IsNullOrEmpty(nuevaContraseña) && !IsStrongPassword(nuevaContraseña));

                    if (!string.IsNullOrEmpty(nuevaContraseña))
                    {
                        campos[3] = GetMD5Hash(nuevaContraseña); // Actualizar contraseña
                    }

                    // Validación y modificación de la fecha de nacimiento
                    DateTime fecha_nacimiento;
                    string fechaNacimientoInput;
                    do
                    {
                        Console.Write("Ingrese la nueva fecha de nacimiento (dd/MM/yyyy) (deje en blanco para no modificar): ");
                        fechaNacimientoInput = Console.ReadLine();
                        if (!string.IsNullOrEmpty(fechaNacimientoInput) && !IsValidDate(fechaNacimientoInput))
                        {
                            Console.WriteLine("Formato de fecha inválido. Inténtelo de nuevo.");
                        }
                    } while (!string.IsNullOrEmpty(fechaNacimientoInput) && !IsValidDate(fechaNacimientoInput));

                    if (!string.IsNullOrEmpty(fechaNacimientoInput))
                    {
                        fecha_nacimiento = DateTime.ParseExact(fechaNacimientoInput, "dd/MM/yyyy", null);
                        campos[5] = fecha_nacimiento.ToString("dd/MM/yyyy"); // Actualizar fecha
                    }

                    // Validación y modificación del teléfono
                    string nuevoTelefono;
                    do
                    {
                        Console.Write("Ingrese el nuevo número de teléfono (deje en blanco para no modificar): ");
                        nuevoTelefono = Console.ReadLine();
                        if (!string.IsNullOrEmpty(nuevoTelefono) && (!IsValidPhoneNumber(nuevoTelefono) || !IsValidByteLength(nuevoTelefono, 4)))
                        {
                            Console.WriteLine("El número de teléfono es inválido o tiene una longitud incorrecta.");
                        }
                    } while (!string.IsNullOrEmpty(nuevoTelefono) && (!IsValidPhoneNumber(nuevoTelefono) || !IsValidByteLength(nuevoTelefono, 4)));

                    if (!string.IsNullOrEmpty(nuevoTelefono))
                    {
                        campos[6] = nuevoTelefono; // Actualizar teléfono
                    }

                    

                    // Crear el nuevo registro actualizado
                    updatedRecord = string.Join(";", campos);
                    lineas[i] = updatedRecord; // Reemplazar la línea antigua
                    Console.WriteLine("Información actualizada con éxito.");
                    break; // Salir del bucle después de actualizar
                }
            }

            // Escribir de nuevo todas las líneas en el archivo
            File.WriteAllLines(path_enhanced, lineas);
        }


        static void BuscarUsuario(string path_enhanced)
        {
            // Solicitar el nombre de usuario a buscar
            Console.Write("Ingrese el nombre de usuario a buscar: ");
            string username = Console.ReadLine().Trim();

            // Leer todas las líneas del archivo
            string[] lineas = File.ReadAllLines(path_enhanced);
            bool found = false;

            foreach (string linea in lineas)
            {
                string[] campos = linea.Split(';');
                if (campos[0].Trim().Equals(username))
                {
                    // Usuario encontrado
                    found = true;
                    Console.WriteLine($"Usuario {campos[0]} encontrado.");
                    Console.Write("¿Desea modificar la información de este usuario? (S/N): ");
                    string respuesta = Console.ReadLine().Trim().ToUpper();
                    if (respuesta == "S")
                    {
                        // Llamar a la función para modificar la información del usuario
                        ModificarInformacionUsuario(path_enhanced, username);
                    }
                    else
                    {
                        Console.WriteLine("No se realizará ninguna modificación.");
                    }

                    break;
                }
            }

            if (!found)
            {
                // Si no se encuentra el usuario
                Console.WriteLine("Usuario no encontrado.");
            }
        }

        static void MenuUsuario(string path_enhanced, string username)
        {
            while (true)
            {
                Console.WriteLine("Menú de Usuario:");
                Console.WriteLine("1. Modificar mi información");
                Console.WriteLine("2. Salir del programa");

                string eleccion = Console.ReadLine();
                switch (eleccion)
                {
                    case "1":
                        ModificarInformacionUsuario(path_enhanced, username);
                    break;
                    case "2":
                        Console.WriteLine("Saliendo del programa...");
                    return;
                 default:
                        Console.WriteLine("Opción no válida. Intente nuevamente.");
                    break;
                }
            }
        }

        static void MenuAdministrador(string path_enhanced, string username)
        {
            while (true)
            {
                Console.WriteLine("Menú de Administrdor:");
                Console.WriteLine("1. Modificar mi información");
                Console.WriteLine("2. Buscar usuario");
                Console.WriteLine("3. Añadir usuario");
                Console.WriteLine("4. Realizar Backup de información"); 
                Console.WriteLine("5. Salir del programa");

                string eleccion = Console.ReadLine();
                switch (eleccion)
                {
                    case "1":
                        ModificarInformacionAdministrador(path_enhanced, username);
                    break;
                     case "2":
                        BuscarUsuario(path_enhanced);
                    break;
                    case "3":
                        AgregarUsuario(path_enhanced);  
                    break;
                     case "4":
                        Backup(path_enhanced, username); 
                    break;
                    case "5":
                        Console.WriteLine("Saliendo del programa...");
                    return;
                    
                 default:
                        Console.WriteLine("Opción no válida. Intente nuevamente.");
                    break;
                }
            }
        }

        static void AgregarUsuario(string path_enhanced)
        {
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
                else if(!IsValidByteLength(telefonoInput,4))
                {
                    Console.WriteLine("El número de teléfono no puede exceder los 4 bytes.");
                }
            } while (!IsValidPhoneNumber(telefonoInput)||!IsValidByteLength(telefonoInput,4));
            telefono = int.Parse(telefonoInput);

            // Crear el nuevo usuario y asignar rol y estatus automáticamente
            AddNewUser(path_enhanced, nuevoUsuario, nombre, apellido, nuevaPassword, fecha_nacimiento, telefono);
        }


        static void Main(string[] args)
        {
            bool usuarioEncontrado = false;
            string path_enhanced = "C:\\MEIA\\user.txt";

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
                            if (rol == 0)
                            {
                                MenuUsuario(path_enhanced,user);
                            }
                            else
                            {
                                MenuAdministrador(path_enhanced,user);
                            }            

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
                        AgregarUsuario(path_enhanced);

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
