using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Personal_Diary.Pages
{
    public partial class NotesPage : Page
    {
        private FirebaseClient firebaseClient;
        private const string FIREBASE_URL = "https://diary-ae3ea-default-rtdb.firebaseio.com/";
        private Dictionary<string, string> entries = new Dictionary<string, string>();
        private Dictionary<string, string> displayNames = new Dictionary<string, string>();
        private string selectedEntryKey = "";

        public NotesPage()
        {
            InitializeComponent();
            firebaseClient = new FirebaseClient(FIREBASE_URL);
            LoadEntries();
        }

        private async void LoadEntries()
        {
            var data = await firebaseClient
                .Child("notes")
                .OnceAsync<string>();

            lstEntries.Items.Clear();
            entries.Clear();
            displayNames.Clear();

            foreach (var entry in data)
            {
                string entryKey = entry.Key;
                string entryValue = entry.Object;

                try
                {
                    if (!string.IsNullOrEmpty(entryValue))
                    {
                        entryValue = JsonConvert.DeserializeObject<string>(entryValue);
                    }
                }
                catch { }

                entries[entryKey] = entryValue ?? "";

                // Создаем красивое отображаемое имя
                string displayName = entryKey.Replace("_", " ");
                displayNames[entryKey] = displayName;

                ListBoxItem item = new ListBoxItem();
                item.Content = displayName;
                item.Tag = entryKey;
                item.ToolTip = $"Ключ: {entryKey}";
                lstEntries.Items.Add(item);
            }
        }

        private void LstEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstEntries.SelectedItem == null) return;

            var selectedItem = (ListBoxItem)lstEntries.SelectedItem;
            selectedEntryKey = selectedItem.Tag.ToString();

            if (entries.ContainsKey(selectedEntryKey))
            {
                txtNoteName.Text = displayNames.ContainsKey(selectedEntryKey)
                    ? displayNames[selectedEntryKey]
                    : selectedEntryKey.Replace("_", " ");

                txtMessage.Text = entries[selectedEntryKey];
                btnDelete.IsEnabled = true;
                btnSave.Content = "Обновить";
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            txtNoteName.Text = "";
            txtMessage.Text = "";
            selectedEntryKey = "";
            lstEntries.SelectedIndex = -1;
            btnDelete.IsEnabled = false;
            btnSave.Content = "Создать";
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string displayName = txtNoteName.Text.Trim();
            string message = txtMessage.Text;

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "новая заметка";
                txtNoteName.Text = displayName;
            }

            string firebaseKey = ConvertToFirebaseKey(displayName);
            string jsonData = JsonConvert.SerializeObject(message);

            try
            {
                if (btnSave.Content.ToString() == "Создать")
                {
                    await firebaseClient
                        .Child("notes")
                        .Child(firebaseKey)
                        .PutAsync(jsonData);

                    LoadEntries();
                    txtNoteName.Text = "";
                    txtMessage.Text = "";
                }
                else
                {
                    if (!string.IsNullOrEmpty(selectedEntryKey))
                    {
                        if (firebaseKey != selectedEntryKey)
                        {
                            await firebaseClient
                                .Child("notes")
                                .Child(firebaseKey)
                                .PutAsync(jsonData);

                            await firebaseClient
                                .Child("notes")
                                .Child(selectedEntryKey)
                                .DeleteAsync();

                            LoadEntries();
                        }
                        else
                        {
                            await firebaseClient
                                .Child("notes")
                                .Child(selectedEntryKey)
                                .PutAsync(jsonData);

                            entries[selectedEntryKey] = message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private string ConvertToFirebaseKey(string displayName)
        {
            string key = displayName
                .Replace(".", "_")
                .Replace("$", "_")
                .Replace("#", "_")
                .Replace("[", "_")
                .Replace("]", "_")
                .Replace("/", "_");

            key = key.Replace(" ", "_");

            while (key.Contains("__"))
            {
                key = key.Replace("__", "_");
            }

            key = key.Trim('_');

            if (string.IsNullOrEmpty(key))
            {
                key = "new_note";
            }

            return key;
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedEntryKey))
            {
                try
                {
                    await firebaseClient
                        .Child("notes")
                        .Child(selectedEntryKey)
                        .DeleteAsync();

                    LoadEntries();
                    txtNoteName.Text = "";
                    txtMessage.Text = "";
                    selectedEntryKey = "";
                    btnDelete.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}");
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadEntries();
        }
    }
}