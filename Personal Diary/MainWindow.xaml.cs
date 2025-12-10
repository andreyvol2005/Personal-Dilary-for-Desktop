using Personal_Diary.Pages;
using System.Windows;
using System.Windows.Controls;

namespace Personal_Diary
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Загружаем страницу dates по умолчанию
            MainFrame.Navigate(new DatesPage());

            // Выделяем кнопку dates
            btnDates.FontWeight = FontWeights.Bold;
            btnNotes.FontWeight = FontWeights.Normal;
        }

        private void BtnDates_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу dates
            MainFrame.Navigate(new DatesPage());

            // Обновляем выделение кнопок
            btnDates.FontWeight = FontWeights.Bold;
            btnNotes.FontWeight = FontWeights.Normal;
        }

        private void BtnNotes_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу notes
            MainFrame.Navigate(new NotesPage());

            // Обновляем выделение кнопок
            btnDates.FontWeight = FontWeights.Normal;
            btnNotes.FontWeight = FontWeights.Bold;
        }
    }
}