using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SteganographyApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Изображения (*.bmp;*.png;*.jpg;*.jpeg)|*.bmp;*.png;*.jpg;*.jpeg|Все файлы (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                ImageControl.Source = bitmapImage;
            }
        }

        private void HideFileButton_Click(object sender, RoutedEventArgs e)
        {
            string secretKey = SecretKeyTextBox.Text;
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                MessageBox.Show("Введите секретный ключ!");
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Все файлы (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true)
            {
                byte[] bytesToHide = File.ReadAllBytes(openFileDialog.FileName);
                WriteableBitmap writableBitmap = new WriteableBitmap((BitmapSource)ImageControl.Source);

                try
                {
                    string fileExtension = Path.GetExtension(openFileDialog.FileName).TrimStart('.');
                    Steganography.HideBytes(writableBitmap, bytesToHide, fileExtension, secretKey);
                    MessageBox.Show("Файл скрыт в изображении!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private void ExtractFileButton_Click(object sender, RoutedEventArgs e)
        {
            string secretKey = SecretKeyTextBox.Text;
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                MessageBox.Show("Введите секретный ключ!");
                return;
            }

            WriteableBitmap writableBitmap = new WriteableBitmap((BitmapSource)ImageControl.Source);

            try
            {
                (string fileExtension, byte[] extractedBytes) = Steganography.ExtractBytes(writableBitmap, secretKey);

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = $"extracted_file.{fileExtension}";
                saveFileDialog.Filter = $"{fileExtension.ToUpper()} files|*.{fileExtension}";
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, extractedBytes);
                    MessageBox.Show("Файл успешно извлечен!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
