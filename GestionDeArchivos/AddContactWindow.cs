using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GestionDeArchivos
{
    public partial class AddContactWindow : Window
{
    private const string UsersFilePath = "users.txt";
    private const string ContactsFilePath = "contacts.txt";
    private string currentUsername;
    private TextBox? searchBox;
    private TextBox? contactSearchBox; // Nuevo TextBox para búsqueda de contactos
    private TextBlock? resultBlock;
    private Button? addButton;
    private string? selectedUserData;
    private ListBox? contactsListBox;
    private ObservableCollection<string> contactsList;
    private Button? broadcastListButton; // Nueva línea agregada

    public AddContactWindow(string username)
    {
        currentUsername = username;
        contactsList = new ObservableCollection<string>();
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        LoadContacts();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        searchBox = this.FindControl<TextBox>("SearchBox");
        contactSearchBox = this.FindControl<TextBox>("ContactSearchBox"); // Inicializar el nuevo TextBox
        resultBlock = this.FindControl<TextBlock>("ResultBlock");
        addButton = this.FindControl<Button>("AddButton");
        contactsListBox = this.FindControl<ListBox>("ContactsListBox");
        broadcastListButton = this.FindControl<Button>("BroadcastListButton");
        
        if (contactsListBox != null)
        {
            contactsListBox.ItemsSource = contactsList;
        }
        
        var searchButton = this.FindControl<Button>("SearchButton");
        var searchContactButton = this.FindControl<Button>("SearchContactButton");
        var modifyContactButton = this.FindControl<Button>("ModifyContactButton");
        var deleteContactButton = this.FindControl<Button>("DeleteContactButton");

        if (searchButton != null) searchButton.Click += SearchButton_Click;
        if (searchContactButton != null) searchContactButton.Click += SearchContactButton_Click;
        if (modifyContactButton != null) modifyContactButton.Click += ModifyContactButton_Click;
        if (deleteContactButton != null) deleteContactButton.Click += DeleteContactButton_Click;
        if (broadcastListButton != null) broadcastListButton.Click += BroadcastListButton_Click;
        if (addButton != null) 
        {
            addButton.Click += AddButton_Click;
            addButton.IsEnabled = false;
        }
    }

        private void LoadContacts()
        {
            try 
            {
                if (!File.Exists(ContactsFilePath))
                {
                    contactsList.Clear();
                    return;
                }

                contactsList.Clear();
                var contacts = File.ReadAllLines(ContactsFilePath)
                    .Where(line => !string.IsNullOrEmpty(line))  // Verificar que la línea no esté vacía
                    .Where(line => 
                    {
                        var parts = line.Split(';');
                        // Verificar que la línea tenga todos los campos necesarios
                        return parts.Length >= 5 && 
                            parts[3].Trim().Length > 0 && 
                            parts[3].Trim() == currentUsername.Trim();
                    })
                    .Select(line => 
                    {
                        var parts = line.Split(';');
                        string status = parts[4].Trim() == "1" ? "Activo" : "Inactivo";
                        return $"{parts[1].Trim()} - {status}";
                    });

                foreach (var contact in contacts)
                {
                    contactsList.Add(contact);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al cargar los contactos: {ex.Message}");
                contactsList.Clear();
            }
        }

        private void SearchButton_Click(object? sender, RoutedEventArgs e)
        {
            if (searchBox == null || string.IsNullOrWhiteSpace(searchBox.Text))
            {
                ShowMessage("Error", "Por favor ingrese un término de búsqueda.");
                return;
            }

            string[] users = File.ReadAllLines(UsersFilePath);
            var foundUser = users.FirstOrDefault(u => 
            {
                var fields = u.Split(';');
                return fields[0].Trim().Equals(searchBox.Text.Trim(), StringComparison.OrdinalIgnoreCase) ||
                       fields[1].Trim().Equals(searchBox.Text.Trim(), StringComparison.OrdinalIgnoreCase);
            });

            if (foundUser != null)
            {
                var userInfo = foundUser.Split(';');
                if (userInfo[0].Trim() == currentUsername)
                {
                    ShowMessage("Error", "No puedes agregarte a ti mismo como contacto.");
                    if (addButton != null) addButton.IsEnabled = false;
                    selectedUserData = null;
                    if (resultBlock != null) resultBlock.Text = string.Empty;
                    return;
                }

                selectedUserData = foundUser;

                string status = userInfo[userInfo.Length - 1].Trim();
                string statusText = status == "1" ? "Activo" : "Inactivo";
                
                string role = userInfo[4].Trim() == "1" ? "Administrador" : "Usuario";
                
                if (resultBlock != null)
                {
                    resultBlock.Text = $"Información del usuario encontrado:\n" +
                                     $"Usuario: {userInfo[0].Trim()}\n" +
                                     $"Nombre: {userInfo[1].Trim()}\n" +
                                     $"Apellido: {userInfo[2].Trim()}\n" +
                                     $"Rol: {role}\n" +
                                     $"Estado: {statusText}\n" +
                                     $"Teléfono: {userInfo[6].Trim()}";
                }

                if (addButton != null) addButton.IsEnabled = true;
            }
            else
            {
                if (resultBlock != null) resultBlock.Text = "Usuario no encontrado.";
                if (addButton != null) addButton.IsEnabled = false;
                selectedUserData = null;
            }
        }

        private void SearchContactButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (contactSearchBox == null || string.IsNullOrWhiteSpace(contactSearchBox.Text))
            {
                ShowMessage("Error", "Por favor ingrese un término de búsqueda en el campo de contactos.");
                return;
            }

            if (!File.Exists(ContactsFilePath))
            {
                ShowMessage("Error", "No hay contactos registrados.");
                contactsList.Clear();
                return;
            }

            var searchTerm = contactSearchBox.Text.Trim().ToLower();
            contactsList.Clear();
            
            var contacts = File.ReadAllLines(ContactsFilePath)
                .Where(line => !string.IsNullOrEmpty(line))
                .Where(line => 
                {
                    try
                    {
                        var parts = line.Split(';');
                        if (parts.Length < 5)
                        {
                            return false;
                        }
                        return parts[3].Trim() == currentUsername.Trim() && 
                               (parts[0].Trim().ToLower().Contains(searchTerm) || 
                                parts[1].Trim().ToLower().Contains(searchTerm));
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Select(line => 
                {
                    try
                    {
                        var parts = line.Split(';');
                        return $"{parts[1].Trim()} - {(parts[4].Trim() == "1" ? "Activo" : "Inactivo")}";
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(contact => contact != null);

            foreach (var contact in contacts)
            {
                if (contact != null)
                {
                    contactsList.Add(contact);
                }
            }

            if (!contactsList.Any())
            {
                ShowMessage("Información", "No se encontraron contactos con ese criterio de búsqueda.");
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error", $"Error al buscar contactos: {ex.Message}");
            contactsList.Clear();
        }
    }

        private async void ModifyContactButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (contactsListBox?.SelectedItem == null)
            {
                ShowMessage("Error", "Por favor seleccione un contacto para modificar.");
                return;
            }

            var selectedContact = contactsListBox.SelectedItem.ToString();
            if (selectedContact == null)
            {
                ShowMessage("Error", "Error al obtener el contacto seleccionado.");
                return;
            }

            // Obtener solo el nombre del contacto (antes del " - ")
            var contactNameParts = selectedContact.Split(new[] { " - " }, StringSplitOptions.None);
            if (contactNameParts.Length < 1)
            {
                ShowMessage("Error", "Formato de contacto inválido.");
                return;
            }
            var contactName = contactNameParts[0].Trim();

            if (!File.Exists(ContactsFilePath))
            {
                ShowMessage("Error", "El archivo de contactos no existe.");
                return;
            }

            // Abrir ventana de edición para el nuevo nombre
            var editWindow = new EditNameWindow();
            var result = await editWindow.ShowDialog<bool>(this);

            if (result && !string.IsNullOrWhiteSpace(editWindow.NewName))
            {
                var newName = editWindow.NewName.Trim();

                // Verificar si el nuevo nombre ya existe en los contactos
                var lines = File.ReadAllLines(ContactsFilePath);
                bool nameExists = lines.Any(line =>
                {
                    try
                    {
                        var parts = line.Split(';');
                        return parts.Length >= 4 &&
                            parts[1].Trim().Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                            parts[3].Trim() == currentUsername.Trim();
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (nameExists)
                {
                    ShowMessage("Error", "Ya existe un contacto con ese nombre.");
                    return;
                }

                // Encontrar y actualizar el contacto
                var linesList = lines.ToList();
                var contactToModify = linesList.FirstOrDefault(line =>
                {
                    try
                    {
                        var parts = line.Split(';');
                        return parts.Length >= 5 &&
                            parts[1].Trim() == contactName &&
                            parts[3].Trim() == currentUsername.Trim();
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (contactToModify == null)
                {
                    ShowMessage("Error", "No se encontró el contacto para modificar.");
                    return;
                }

                var parts = contactToModify.Split(';');
                if (parts.Length < 5)
                {
                    ShowMessage("Error", "El formato del contacto es inválido.");
                    return;
                }

                // Actualizar el contacto con el nuevo nombre
                var updatedContact = $"{parts[0]};{newName.PadRight(20).Substring(0, 20)};{DateTime.Now:yyyy-MM-dd HH:mm:ss};{parts[3]};{parts[4]}";

                var index = linesList.IndexOf(contactToModify);
                if (index != -1)
                {
                    linesList[index] = updatedContact;
                    File.WriteAllLines(ContactsFilePath, linesList);
                    ShowMessage("Éxito", "Nombre del contacto actualizado correctamente.");
                    LoadContacts();
                }
                else
                {
                    ShowMessage("Error", "No se pudo actualizar el contacto.");
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error", $"Error al modificar el contacto: {ex.Message}");
        }
    }
        private void DeleteContactButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (contactsListBox?.SelectedItem == null)
                {
                    ShowMessage("Error", "Por favor seleccione un contacto para eliminar.");
                    return;
                }

                var selectedContact = contactsListBox.SelectedItem.ToString();
                if (selectedContact == null)
                {
                    ShowMessage("Error", "Error al obtener el contacto seleccionado.");
                    return;
                }

                // Obtener solo el nombre del contacto (antes del " - ")
                var contactNameParts = selectedContact.Split(new[] { " - " }, StringSplitOptions.None);
                if (contactNameParts.Length < 1)
                {
                    ShowMessage("Error", "Formato de contacto inválido.");
                    return;
                }
                var contactName = contactNameParts[0].Trim();

                if (!File.Exists(ContactsFilePath))
                {
                    ShowMessage("Error", "El archivo de contactos no existe.");
                    return;
                }

                var lines = File.ReadAllLines(ContactsFilePath).ToList();
                var contactToRemove = lines.FirstOrDefault(line =>
                {
                    try
                    {
                        var parts = line.Split(';');
                        return parts.Length >= 5 &&
                            parts[1].Trim() == contactName &&
                            parts[3].Trim() == currentUsername.Trim();
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (contactToRemove != null)
                {
                    lines.Remove(contactToRemove);
                    File.WriteAllLines(ContactsFilePath, lines);
                    ShowMessage("Éxito", "Contacto eliminado correctamente.");
                    LoadContacts();
                }
                else
                {
                    ShowMessage("Error", "No se encontró el contacto para eliminar.");
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al eliminar el contacto: {ex.Message}");
            }
        }

        private async void BroadcastListButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var broadcastWindow = new BroadcastListWindow(currentUsername);
                await broadcastWindow.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al abrir la ventana de listas de difusión: {ex.Message}");
            }
        }

        private void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedUserData == null)
                {
                    ShowMessage("Error", "Por favor, busque un usuario primero.");
                    return;
                }

                var userInfo = selectedUserData.Split(';');
                string username = userInfo[0].Trim().PadRight(20);
                string contactName = $"{userInfo[1].Trim()} {userInfo[2].Trim()}".PadRight(20);
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string userTransaction = currentUsername.PadRight(20);
                string status = userInfo[userInfo.Length - 1].Trim();

                string record = $"{username.Substring(0, 20)};{contactName.Substring(0, 20)};{dateTime};{userTransaction.Substring(0, 20)};{status}\n";

                // Verificar si el archivo existe y crear el directorio si es necesario
                string directoryPath = Path.GetDirectoryName(ContactsFilePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Verificar duplicados solo si el archivo existe
                if (File.Exists(ContactsFilePath))
                {
                    string[] contacts = File.ReadAllLines(ContactsFilePath);
                    bool contactExists = contacts.Any(c => 
                    {
                        var fields = c.Split(';');
                        return fields.Length >= 4 &&  // Verificar que tenga suficientes campos
                            fields[0].Trim() == username.Trim() && 
                            fields[3].Trim() == userTransaction.Trim();
                    });

                    if (contactExists)
                    {
                        ShowMessage("Error", "Este contacto ya existe en tu lista.");
                        return;
                    }
                }

                // Agregar el nuevo contacto
                File.AppendAllText(ContactsFilePath, record, Encoding.UTF8);
                ShowMessage("Éxito", "Contacto agregado correctamente.");
                LoadContacts();
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al agregar el contacto: {ex.Message}");
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