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
    public partial class DatesPage : Page
    {
        private FirebaseClient firebaseClient;
        private const string FIREBASE_URL = "https://diary-ae3ea-default-rtdb.firebaseio.com/";
        private Dictionary<string, string> entries = new Dictionary<string, string>(); // key -> text
        private Dictionary<string, string> displayNames = new Dictionary<string, string>(); // key -> display name
        private string selectedEntryKey = "";

        public DatesPage()
        {
            InitializeComponent();
            firebaseClient = new FirebaseClient(FIREBASE_URL);
            LoadEntries();
        }

        private async void LoadEntries()
        {
            var data = await firebaseClient
                .Child("dates")
                .OnceAsync<string>();

            lstEntries.Items.Clear();
            entries.Clear();
            displayNames.Clear();

            foreach (var entry in data)
            {
                string entryKey = entry.Key; // Ключ в Firebase (с _)
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

                // Создаем красивое отображаемое имя (без _)
                string displayName = entryKey.Replace("_", " ");
                displayNames[entryKey] = displayName;

                ListBoxItem item = new ListBoxItem();
                item.Content = displayName; // Показываем без _
                item.Tag = entryKey; // Храним ключ с _
                item.ToolTip = $"Ключ: {entryKey}";
                lstEntries.Items.Add(item);
            }
        }

        private void LstEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstEntries.SelectedItem == null) return;

            var selectedItem = (ListBoxItem)lstEntries.SelectedItem;
            selectedEntryKey = selectedItem.Tag.ToString(); // Ключ с _

            if (entries.ContainsKey(selectedEntryKey))
            {
                // Показываем красивое имя без _
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

            // Проверяем название заметки
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "новая заметка";
                txtNoteName.Text = displayName;
            }

            // Преобразуем красивое имя в ключ для Firebase
            string firebaseKey = ConvertToFirebaseKey(displayName);

            // Сериализуем текст в JSON
            string jsonData = JsonConvert.SerializeObject(message);

            try
            {
                if (btnSave.Content.ToString() == "Создать")
                {
                    // Создание новой заметки
                    await firebaseClient
                        .Child("dates")
                        .Child(firebaseKey)
                        .PutAsync(jsonData);

                    // Обновляем список
                    LoadEntries();

                    // Очищаем поля
                    txtNoteName.Text = "";
                    txtMessage.Text = "";
                }
                else
                {
                    // Обновление существующей заметки
                    if (!string.IsNullOrEmpty(selectedEntryKey))
                    {
                        // Если название изменилось
                        if (firebaseKey != selectedEntryKey)
                        {
                            // Создаем новую заметку с новым названием
                            await firebaseClient
                                .Child("dates")
                                .Child(firebaseKey)
                                .PutAsync(jsonData);

                            // Удаляем старую
                            await firebaseClient
                                .Child("dates")
                                .Child(selectedEntryKey)
                                .DeleteAsync();

                            LoadEntries();
                        }
                        else
                        {
                            // Просто обновляем содержимое
                            await firebaseClient
                                .Child("dates")
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

        // Преобразуем красивое имя в ключ для Firebase
        private string ConvertToFirebaseKey(string displayName)
        {
            // Заменяем запрещенные символы
            string key = displayName
                .Replace(".", "_")
                .Replace("$", "_")
                .Replace("#", "_")
                .Replace("[", "_")
                .Replace("]", "_")
                .Replace("/", "_");

            // Заменяем пробелы на _
            key = key.Replace(" ", "_");

            // Убираем несколько подряд идущих _
            while (key.Contains("__"))
            {
                key = key.Replace("__", "_");
            }

            // Убираем _ в начале и конце
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
                        .Child("dates")
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