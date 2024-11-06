using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace GestionDeArchivos
{
    public partial class SelectContactsWindow : Window
    {
        private const string ListsFilePath = "Lista.txt";
        private const string ContactsFilePath = "contacts.txt";
        
        private TextBox? searchBox;
        private TextBlock? resultBlock;
        private ListBox? contactsListBox;
        private Button? addButton;
        private ObservableCollection<string>? contacts;
        private readonly string currentUsername = string.Empty;
        private List<string>? currentListInfo;

        private const int NOMBRE_LISTA_SIZE = 30;
        private const int USUARIO_SIZE = 20;
        private const int DESCRIPCION_SIZE = 40;
        private const int NUMERO_USUARIOS_SIZE = 4;

        public SelectContactsWindow()
        {
            InitializeComponent();
            contacts = new ObservableCollection<string>();
        }

        public SelectContactsWindow(string username) : this()
        {
            currentUsername = username;
            
            searchBox = this.FindControl<TextBox>("SearchBox");
            resultBlock = this.FindControl<TextBlock>("ResultBlock");
            contactsListBox = this.FindControl<ListBox>("ContactsListBox");
            addButton = this.FindControl<Button>("AddButton");

            if (contactsListBox != null)
            {
                contactsListBox.ItemsSource = contacts;
            }

            var searchButton = this.FindControl<Button>("SearchButton");
            if (searchButton != null)
            {
                searchButton.Click += SearchButton_Click!;
            }

            if (addButton != null)
            {
                addButton.Click += AddButton_Click!;
            }

            LoadContacts();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadContacts()
        {
            try
            {
                if (contacts != null && File.Exists(ContactsFilePath))
                {
                    var lines = File.ReadAllLines(ContactsFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(';');
                        if (parts.Length >= 2)
                        {
                            contacts.Add(parts[1].Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al cargar contactos: {ex.Message}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchBox?.Text))
                {
                    ShowMessage("Error", "Por favor ingrese un nombre de lista para buscar.");
                    return;
                }

                if (File.Exists(ListsFilePath))
                {
                    var lines = File.ReadAllLines(ListsFilePath);
                    var foundList = lines.FirstOrDefault(line =>
                    {
                        var parts = line.Split(';');
                        return parts.Length >= 6 &&
                               parts[0].Trim().Equals(searchBox.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                               parts[1].Trim() == currentUsername;
                    });

                    if (foundList != null)
                    {
                        currentListInfo = foundList.Split(';').ToList();
                        if (resultBlock != null)
                        {
                            resultBlock.Text = $"Lista encontrada: {currentListInfo[0].Trim()} - Usuarios actuales: {int.Parse(currentListInfo[3])}";
                        }
                        if (addButton != null)
                        {
                            addButton.IsEnabled = true;
                        }
                    }
                    else
                    {
                        if (resultBlock != null)
                        {
                            resultBlock.Text = "Lista no encontrada.";
                        }
                        if (addButton != null)
                        {
                            addButton.IsEnabled = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al buscar la lista: {ex.Message}");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentListInfo == null)
                {
                    ShowMessage("Error", "Por favor busque y seleccione una lista primero.");
                    return;
                }

                if (contactsListBox?.SelectedItems == null || contactsListBox.SelectedItems.Count == 0)
                {
                    ShowMessage("Error", "Por favor seleccione al menos un contacto.");
                    return;
                }

                // Leer todas las líneas
                var lines = File.ReadAllLines(ListsFilePath).ToList();
                
                // Encontrar el índice de la lista actual
                int listIndex = lines.FindIndex(line =>
                {
                    var parts = line.Split(';');
                    return parts.Length >= 6 &&
                           parts[0].Trim().Equals(currentListInfo[0].Trim(), StringComparison.OrdinalIgnoreCase) &&
                           parts[1].Trim() == currentUsername;
                });

                if (listIndex != -1)
                {
                    // Actualizar el número de usuarios
                    var currentUsers = int.Parse(currentListInfo[3]);
                    var newUsers = currentUsers + contactsListBox.SelectedItems.Count;
                    currentListInfo[3] = newUsers.ToString().PadLeft(NUMERO_USUARIOS_SIZE, '0');

                    // Reconstruir la línea
                    lines[listIndex] = string.Join(";", currentListInfo);

                    // Guardar el archivo actualizado
                    File.WriteAllLines(ListsFilePath, lines);

                    ShowMessage("Éxito", $"Se agregaron {contactsListBox.SelectedItems.Count} contactos a la lista.");
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error al agregar contactos a la lista: {ex.Message}");
            }
        }

        private void ShowMessage(string title, string message)
        {
            var messageBox = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
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