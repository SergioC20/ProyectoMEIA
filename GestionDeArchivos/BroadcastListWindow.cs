using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;

namespace GestionDeArchivos
{
    public partial class BroadcastListWindow : Window
    {
        private const string BroadcastListsFilePath = "Lista.txt";
        private const string BroadcastListsDescriptorPath = "Lista_desc.txt";
        private const string IndiceFilePath = "indice.txt";

        private TextBox? listNameBox;
        private TextBox? descriptionBox;
        private TextBlock? resultBlock;
        private ListBox? broadcastListBox;
        private ObservableCollection<string> broadcastLists;
        private readonly string currentUsername;
        private TextBox? userCountBox;
        private const int NOMBRE_LISTA_SIZE = 30;
        private const int USUARIO_SIZE = 20;
        private const int DESCRIPCION_SIZE = 40;
        private const int NUMERO_USUARIOS_SIZE = 4;

        public BroadcastListWindow(string username)
        {
            currentUsername = username;
            broadcastLists = new ObservableCollection<string>();
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            LoadLists();
            InitializeFiles();
            LoadLists();
        }

         private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            listNameBox = this.FindControl<TextBox>("ListNameBox");
            descriptionBox = this.FindControl<TextBox>("DescriptionBox");
            userCountBox = this.FindControl<TextBox>("UserCountBox");
            resultBlock = this.FindControl<TextBlock>("ResultBlock");
            broadcastListBox = this.FindControl<ListBox>("BroadcastListBox");

            if (broadcastListBox != null)
            {
                broadcastListBox.ItemsSource = broadcastLists;
            }

            var createButton = this.FindControl<Button>("CreateButton");
            var addButton = this.FindControl<Button>("AddButton");
            var modifyButton = this.FindControl<Button>("ModifyButton");
            var searchButton = this.FindControl<Button>("SearchButton");
            var deleteButton = this.FindControl<Button>("DeleteButton");

            if (createButton != null) createButton.Click += CreateButton_Click;
            if (addButton != null) addButton.Click += AddButton_Click;
            if (modifyButton != null) modifyButton.Click += ModifyButton_Click;
            if (searchButton != null) searchButton.Click += SearchButton_Click;
            if (deleteButton != null) deleteButton.Click += DeleteButton_Click;
        }

        private string PadField(string value, int size)
        {
            if (value.Length > size)
                return value.Substring(0, size);
            return value.PadRight(size);
        }

        private void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(listNameBox?.Text))
                {
                    ShowMessage("Error", "Por favor ingrese un nombre para la lista.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(descriptionBox?.Text))
                {
                    ShowMessage("Error", "Por favor ingrese una descripción para la lista.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(userCountBox?.Text) || !int.TryParse(userCountBox.Text, out int userCount))
                {
                    ShowMessage("Error", "Por favor ingrese un número válido de usuarios.");
                    return;
                }

                // Verificar si ya existe una lista con ese nombre
                if (File.Exists(BroadcastListsFilePath))
                {
                    var exists = File.ReadAllLines(BroadcastListsFilePath)
                        .Any(line =>
                        {
                            var parts = line.Split(';');
                            return parts.Length >= 6 &&
                                parts[0].Trim().Equals(listNameBox.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                parts[1].Trim() == currentUsername;
                        });

                    if (exists)
                    {
                        ShowMessage("Error", "Ya existe una lista con ese nombre.");
                        return;
                    }
                }

                var listName = PadField(listNameBox.Text, NOMBRE_LISTA_SIZE);
                var usuario = PadField(currentUsername, USUARIO_SIZE);
                var description = PadField(descriptionBox.Text, DESCRIPCION_SIZE);
                var numUsuarios = userCount.ToString().PadLeft(NUMERO_USUARIOS_SIZE, '0');
                var fechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var estatus = "1"; // 1 para activo

                string record = $"{listName};{usuario};{description};{numUsuarios};{fechaCreacion};{estatus}\n";

                // Obtener la posición en Lista.txt
                int posicion = 0;
                if (File.Exists(BroadcastListsFilePath))
                {
                    var lines = File.ReadAllLines(BroadcastListsFilePath);
                    posicion = lines.Length; // La nueva línea irá en esta posición
                }

                // Escribir en Lista.txt
                File.AppendAllText(BroadcastListsFilePath, record);

                // Manejar el archivo índice
                int registro = 0;
                if (!File.Exists(IndiceFilePath))
                {
                    // Si el archivo no existe, crear con el primer registro
                    record = $"{registro};{posicion};{listName};{usuario};{description};{numUsuarios};{fechaCreacion};{estatus}\n";
                    File.WriteAllText(IndiceFilePath, record);
                }
                else
                {
                    // Si existe, obtener el último registro y agregar uno nuevo
                    var lines = File.ReadAllLines(IndiceFilePath);
                    if (lines.Length > 0)
                    {
                        var lastLine = lines.Last();
                        var parts = lastLine.Split(';');
                        if (parts.Length > 0 && int.TryParse(parts[0], out int lastRegistro))
                        {
                            registro = lastRegistro + 1;
                        }
                    }
                    record = $"{registro};{posicion};{listName};{usuario};{description};{numUsuarios};{fechaCreacion};{estatus}\n";
                    File.AppendAllText(IndiceFilePath, record);
                }

                UpdateDescriptor(isNewRecord: true);
                
                ShowMessage("Éxito", "Lista de difusión creada correctamente.");
                LoadLists();

                // Limpiar campos
                if (listNameBox != null) listNameBox.Text = string.Empty;
                if (descriptionBox != null) descriptionBox.Text = string.Empty;
                if (userCountBox != null) userCountBox.Text = string.Empty;
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al crear la lista: {ex.Message}");
            }
        }
        private void LoadLists()
        {
            try
            {
                broadcastLists.Clear();
                if (File.Exists(BroadcastListsFilePath))
                {
                    var lists = File.ReadAllLines(BroadcastListsFilePath)
                        .Where(line =>
                        {
                            var parts = line.Split(';');
                            return parts.Length >= 6 &&
                                   parts[1].Trim() == currentUsername &&
                                   parts[5].Trim() == "1"; // Solo listas activas
                        })
                        .Select(line =>
                        {
                            var parts = line.Split(';');
                            return $"{parts[0].Trim()} - {parts[2].Trim()} (Usuarios: {int.Parse(parts[3])})";
                        });

                    foreach (var list in lists)
                    {
                        broadcastLists.Add(list);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al cargar las listas: {ex.Message}");
            }
        }


        private async void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var selectContactsWindow = new SelectContactsWindow(currentUsername);
                await selectContactsWindow.ShowDialog(this);
                LoadLists(); // Recargar las listas después de posible creación
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al abrir la ventana de selección: {ex.Message}");
            }
        }
        private void ModifyButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (broadcastListBox?.SelectedItem == null)
                {
                    ShowMessage("Error", "Por favor seleccione una lista para modificar.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(descriptionBox?.Text))
                {
                    ShowMessage("Error", "Por favor ingrese una descripción para la lista.");
                    return;
                }

                var selectedList = broadcastListBox.SelectedItem.ToString();
                if (selectedList == null)
                {
                    ShowMessage("Error", "Error al obtener la lista seleccionada.");
                    return;
                }

                // Obtener el nombre de la lista seleccionada
                var listName = selectedList.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();

                // Leer todas las líneas del archivo
                var lines = File.ReadAllLines(BroadcastListsFilePath).ToList();
                
                // Encontrar la lista a modificar
                var index = lines.FindIndex(line =>
                {
                    var parts = line.Split(';');
                    return parts.Length >= 6 &&
                        parts[0].Trim() == listName &&
                        parts[1].Trim() == currentUsername;
                });

                if (index != -1)
                {
                    // Separar la línea en sus partes
                    var parts = lines[index].Split(';');
                    
                    // Actualizar solo la descripción manteniendo los demás campos
                    var newDescription = PadField(descriptionBox.Text, DESCRIPCION_SIZE);
                    
                    // Reconstruir la línea con la nueva descripción
                    var updatedLine = $"{parts[0]};{parts[1]};{newDescription};{parts[3]};{parts[4]};{parts[5]}";
                    
                    // Actualizar la línea en el archivo
                    lines[index] = updatedLine;
                    File.WriteAllLines(BroadcastListsFilePath, lines);
                    
                    ShowMessage("Éxito", "Descripción de la lista modificada correctamente.");
                    LoadLists();

                    // Limpiar campo de descripción
                    if (descriptionBox != null) descriptionBox.Text = string.Empty;
                }
                else
                {
                    ShowMessage("Error", "No se encontró la lista para modificar.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al modificar la lista: {ex.Message}");
            }
        }

        private void SearchButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(listNameBox?.Text))
                {
                    ShowMessage("Error", "Por favor ingrese un nombre para buscar.");
                    return;
                }

                var searchTerm = listNameBox.Text.Trim().ToLower();
                
                broadcastLists.Clear();
                if (File.Exists(BroadcastListsFilePath))
                {
                    var lists = File.ReadAllLines(BroadcastListsFilePath)
                        .Where(line =>
                        {
                            var parts = line.Split(';');
                            return parts.Length >= 4 &&
                                   parts[3].Trim() == currentUsername &&
                                   (parts[0].Trim().ToLower().Contains(searchTerm) ||
                                    parts[1].Trim().ToLower().Contains(searchTerm));
                        })
                        .Select(line =>
                        {
                            var parts = line.Split(';');
                            return $"{parts[0].Trim()} - {parts[1].Trim()}";
                        });

                    foreach (var list in lists)
                    {
                        broadcastLists.Add(list);
                    }

                    if (!broadcastLists.Any())
                    {
                        ShowMessage("Información", "No se encontraron listas con ese criterio.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al buscar listas: {ex.Message}");
            }
        }

        private void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (broadcastListBox?.SelectedItem == null)
                {
                    ShowMessage("Error", "Por favor seleccione una lista para eliminar.");
                    return;
                }

                var selectedList = broadcastListBox.SelectedItem.ToString();
                if (selectedList == null)
                {
                    ShowMessage("Error", "Error al obtener la lista seleccionada.");
                    return;
                }

                // Obtener el nombre de la lista seleccionada
                var listName = selectedList.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();

                // Leer todas las líneas del archivo
                var lines = File.ReadAllLines(BroadcastListsFilePath).ToList();
                
                // Encontrar la lista a eliminar
                var index = lines.FindIndex(line =>
                {
                    var parts = line.Split(';');
                    return parts.Length >= 6 &&
                        parts[0].Trim() == listName &&
                        parts[1].Trim() == currentUsername;
                });

                if (index != -1)
                {
                    // Eliminar la línea
                    lines.RemoveAt(index);
                    
                    // Guardar el archivo actualizado
                    File.WriteAllLines(BroadcastListsFilePath, lines);
                    
                    // Actualizar el descriptor
                    UpdateDescriptor(isDeactivation: true);
                    
                    ShowMessage("Éxito", "Lista eliminada correctamente.");
                    LoadLists();
                }
                else
                {
                    ShowMessage("Error", "No se encontró la lista para eliminar.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al eliminar la lista: {ex.Message}");
            }
        }

        private void UpdateDescriptor(bool isNewRecord = false, bool isActivation = false, bool isDeactivation = false)
        {
            try
            {
                var descriptorLines = File.ReadAllLines(BroadcastListsDescriptorPath).ToList();
                var totalRecords = descriptorLines.FirstOrDefault(l => l.StartsWith("numero_registros="));
                var activeRecords = descriptorLines.FirstOrDefault(l => l.StartsWith("registros_activos="));
                var inactiveRecords = descriptorLines.FirstOrDefault(l => l.StartsWith("registros_inactivos="));

                if (isNewRecord)
                {
                    int currentTotal = int.Parse(totalRecords?.Split('=')[1] ?? "0") + 1;
                    int currentActive = int.Parse(activeRecords?.Split('=')[1] ?? "0") + 1;

                    descriptorLines[3] = $"numero_registros={currentTotal}";
                    descriptorLines[4] = $"registros_activos={currentActive}";
                }
                else if (isActivation)
                {
                    int currentActive = int.Parse(activeRecords?.Split('=')[1] ?? "0") + 1;
                    int currentInactive = int.Parse(inactiveRecords?.Split('=')[1] ?? "0") - 1;

                    descriptorLines[4] = $"registros_activos={currentActive}";
                    descriptorLines[5] = $"registros_inactivos={currentInactive}";
                }
                else if (isDeactivation)
                {
                    int currentActive = int.Parse(activeRecords?.Split('=')[1] ?? "0") - 1;
                    int currentInactive = int.Parse(inactiveRecords?.Split('=')[1] ?? "0") + 1;

                    descriptorLines[4] = $"registros_activos={currentActive}";
                    descriptorLines[5] = $"registros_inactivos={currentInactive}";
                }

                File.WriteAllLines(BroadcastListsDescriptorPath, descriptorLines);
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al actualizar el descriptor: {ex.Message}");
            }
        }

        private void InitializeFiles()
        {
            try
            {
                if (!File.Exists(BroadcastListsFilePath))
                {
                    // Crear el archivo de listas si no existe
                    using (FileStream fs = File.Create(BroadcastListsFilePath)) { }
                    
                    // Crear y escribir el descriptor inicial
                    string descriptorContent = $"nombre_simbolico=Lista\n" +
                                            $"fecha_creacion={DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                            $"usuario_creacion={currentUsername}\n" +
                                            $"numero_registros=0\n" +
                                            $"registros_activos=0\n" +
                                            $"registros_inactivos=0\n";
                    
                    File.WriteAllText(BroadcastListsDescriptorPath, descriptorContent);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al inicializar los archivos: {ex.Message}");
            }
        }
        private void ShowMessage(string title, string message)
        {
            var messageBox = new Window
            {
                Title = title,
                Width = 250,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var messageBlock = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            okButton.Click += (s, e) => messageBox.Close();

            panel.Children.Add(messageBlock);
            panel.Children.Add(okButton);
            messageBox.Content = panel;

            messageBox.ShowDialog(this);
        }
    }
}