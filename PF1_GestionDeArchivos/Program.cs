using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace EjemploLogin
{
    internal class Program
    {
        static int RECORD_SIZE = 84;

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

        //Método AddNewUser para agregar un usuario 
        static void AddNewUser(string path, string path_enhanced, string username, string email, string password)
        {
            // Cargar las líneas del archivo users_index
            List<string> lineasIndex = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();

            // Encontrar el índice máximo actual en el archivo users_index
            int maxIndex = 0;
            if (lineasIndex.Count > 0)
            {
                maxIndex = lineasIndex.Max(linea => Convert.ToInt32(linea.Split('|')[1].Trim()));
            }

            // Asignar el nuevo índice al usuario (incrementar el índice máximo)
            int nuevoIndice = maxIndex + 1;

            // Añadir el nuevo correo en el archivo users_index con 32 caracteres
            lineasIndex.Add($"{email.PadRight(32)}|{nuevoIndice}");
            File.WriteAllLines(path, lineasIndex);

            // Añadir el nuevo registro en el archivo users_enhanded
            string hashedPassword = GetMD5Hash(password);
            string newRecord = $"{username.PadRight(16)}|{email.PadRight(32)}|{hashedPassword.PadRight(32)}\n"; // 84 caracteres con salto de línea
            using (FileStream fs = new FileStream(path_enhanced, FileMode.Append, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                byte[] recordBytes = Encoding.ASCII.GetBytes(newRecord);
                writer.Write(recordBytes);
            }

            Console.WriteLine("Usuario creado con éxito.");
        }

        static void Main(string[] args)
        {
            string path = "C:\\MEIA\\users_index.txt";
            string[] lineas = { };
            bool usuarioEncontrado = false;

            string path_enhanced = "C:\\MEIA\\users_enhanced.txt";

            if (File.Exists(path))
            {
                lineas = File.ReadAllLines(path);
            }

            Console.WriteLine("Ingrese su correo y contraseña");
            Console.Write("Correo: ");
            String email = Console.ReadLine();
            Console.Write("Contraseña: ");
            String password = Console.ReadLine();

            for (int i = 0; i < lineas.Length; i++)
            {
                string[] campos = lineas[i].Split('|');
                if (campos[0].Trim().Equals(email))
                {
                    int index_enhanced_file = Convert.ToInt32(campos[1]);
                    string registro = GetRecord(path_enhanced, index_enhanced_file);
                    string nombre = registro.Split('|')[0].Trim();
                    string password_guardado = registro.Split('|')[2].Trim();

                    if (password_guardado.ToLower().Equals(GetMD5Hash(password).ToLower()))
                    {
                        usuarioEncontrado = true;
                        Console.WriteLine("Bienvenido " + nombre);
                    }
                }
            }

            if (!usuarioEncontrado) 
            {
                Console.WriteLine("Usuario no encontrado. ¿Desea crear uno nuevo? (S/N)");
                if (Console.ReadLine().ToUpper() == "S")
                {
                    // Solicitar el nombre del usuario
                    Console.Write("Ingrese su nombre: ");
                    string nombre = Console.ReadLine();

                    AddNewUser(path, path_enhanced, nombre, email, password); // en caso de que no haya encontrado el usuario creara un nuevo usuario con la informcion ingresada
                }
                else
                {
                    Console.WriteLine("Operación cancelada."); // en caso de que no quiera crear un nuevo usuario
                }
            }

            Console.ReadKey();
        }
    }
}
