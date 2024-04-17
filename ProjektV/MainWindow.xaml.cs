using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Xml.Linq;

namespace ProjektV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string imagePath = "";
        public MainWindow()
        {
            InitializeComponent();
            lblResult.Visibility = Visibility.Hidden;
        }

        private void Choose_file_button_click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new();
            fileDialog.Filter = "Supported image files (PNG, TIFF) | *.png;*.tif;*.tiff";

            bool? success = fileDialog.ShowDialog();

            if (success == true)
            {
                string path = fileDialog.FileName;
                imagePath = path;
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(path));
                    lblNoContent.Visibility = Visibility.Hidden;
                    lblResult.Visibility = Visibility.Hidden;
                    img1.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Nebyl vybrán žádný soubor.");
                lblNoContent.Visibility = Visibility.Visible;
            }
        }

        private void Calculate_density_button_click(object sender, RoutedEventArgs e)
        {
            if (imagePath == "")
            {
                MessageBox.Show("Nebyl vybrán žádný soubor.");
                return;
            }
            Bitmap img = new Bitmap(imagePath);

            double sumOfAllColors = 0;
            double sumOfAllPixels = 0;

            for (int i = 237; i <= 748; ++i)
            {
                for (int j = 519; j <= 1031; ++j)
                {
                    System.Drawing.Color pixel = img.GetPixel(j, i);
                    sumOfAllColors += pixel.R;
                    ++sumOfAllPixels;
                }
            }

            double avgColor = sumOfAllColors / sumOfAllPixels;
            Console.WriteLine(avgColor);

            double avgColorInPercentage = (100 * avgColor) / 255;
            Console.WriteLine(avgColorInPercentage);
            lblResult.Content = "Hustota krevního řečiště: " + avgColorInPercentage.ToString("N2") + " %";
            lblResult.Visibility = Visibility.Visible;
            img.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (imagePath == "")
            {
                MessageBox.Show("Nebyl vybrán žádný soubor.");
                return;
            }
            Bitmap img = new Bitmap(imagePath);

            // Otsu's thresholding to convert the image to binary
            Bitmap binaryImg = OtsuThresholding(img);

            // Counting white pixels in the binary image
            int whitePixelCount = 0;
            int pixelCount = 0;

            for (int i = 237; i <= 748; ++i)
            {
                for (int j = 519; j <= 1031; ++j)
                {
                    System.Drawing.Color pixel = binaryImg.GetPixel(j, i);
                    if (pixel.R == 255) // Assuming white pixel is (255, 255, 255) in RGB
                    {
                        whitePixelCount++;
                    }
                    pixelCount++;
                }
            }

            double whitePixelPercentage = ((double)whitePixelCount / pixelCount) * 100;
            lblResult.Content += "\nHustota krevního řečiště (po Otsu): " + whitePixelPercentage.ToString("N2") + " %";

            lblResult.Visibility = Visibility.Visible;
            img1.Source = ImageSourceFromBitmap(binaryImg);

            binaryImg.Dispose();
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        public BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void Export_button_click(object sender, RoutedEventArgs e)
        {
            if (imagePath == "" || lblResult.Visibility == Visibility.Hidden)
            {
                MessageBox.Show("Nebyl vybrán žádný soubor, nebo nebyla vypočítána hustota krevního řečiště.");
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();  
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "Supported image files (PNG, TIFF) | *.png;*.tif;*.tiff"; ; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();
            string path = "";

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                path = dlg.FileName;
            }
            else
            {
                return;
            }

            Bitmap img = new Bitmap(imagePath);

            // Create a Graphics object from the image
            using (Graphics graphics = Graphics.FromImage(img))
            {
                // Set the font and brush for the text
                Font font = new Font("Arial", 14);
                SolidBrush brush = new SolidBrush(System.Drawing.Color.Black);

                // Set the position where you want to place the text
                float x = 640;
                float y = 30;

                // Set any other options (e.g., quality, smoothing, etc.)
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Draw the text onto the image
                graphics.DrawString(lblResult.Content.ToString(), font, brush, x, y);

                // Save the image with the text
                img.Save(path, ImageFormat.Png);
            }

            // Dispose of the original image after saving
            img.Dispose();
        }

        private void Editor_button_click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                ImageEditorWindow editorWindow = new ImageEditorWindow(imagePath);
                editorWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Nebyl vybrán žádný soubor.");
            }
        }

        private Bitmap OtsuThresholding(Bitmap image)
        {
            // Convert the image to grayscale
            Bitmap grayImage = image;

            // Compute histogram
            int[] histogram = new int[256];
            for (int i = 237; i <= 748; ++i)
            {
                for (int j = 519; j <= 1031; ++j)
                {
                    System.Drawing.Color pixel = grayImage.GetPixel(j, i);
                    histogram[pixel.R]++;
                }
            }
            // Total number of pixels
            int totalPixels = (1031 - 518) * (748 - 236);

            // Calculate sum of all pixel values
            double sum = 0;
            for (int t = 0; t < 256; t++)
            {
                sum += t * histogram[t];
            }

            // Variables for Otsu's method
            double sumB = 0;
            int wB = 0;
            int wF = 0;
            double maxVariance = 0;
            int threshold = 0;

            // Find optimal threshold
            for (int t = 0; t < 256; t++)
            {
                wB += histogram[t]; // Weight background
                if (wB == 0)
                    continue;

                wF = totalPixels - wB; // Weight foreground
                if (wF == 0)
                    break;

                sumB += (double)(t * histogram[t]);

                double mB = sumB / wB; // Mean background
                double mF = (sum - sumB) / wF; // Mean foreground

                // Calculate between-class variance
                double betweenVariance = (double)wB * (double)wF * (mB - mF) * (mB - mF);
                if (betweenVariance > maxVariance)
                {
                    maxVariance = betweenVariance;
                    threshold = t;
                }
            }

            // Create binary image using the threshold
            Bitmap binaryImage = new Bitmap(grayImage.Width, grayImage.Height);

            for (int i = 237; i <= 748; ++i)
            {
                for (int j = 519; j <= 1031; ++j)
                {
                    System.Drawing.Color pixel = grayImage.GetPixel(j, i);
                    histogram[pixel.R]++;
                }
            }

            for (int i = 0; i < grayImage.Height; i++)
            {
                for (int j = 0; j < grayImage.Width; j++)
                {
                    System.Drawing.Color pixel = grayImage.GetPixel(j, i);

                    if ((i >= 237 && i <= 748) && (j >= 519 && j <= 1031))
                    {
                        if (pixel.R <= threshold)
                        {
                            binaryImage.SetPixel(j, i, System.Drawing.Color.Black);
                        }
                        else
                        {
                            binaryImage.SetPixel(j, i, System.Drawing.Color.White);
                        }
                    }
                    else if (i > 100 && i < 200 && j > 100 && j < 200)
                    {
                        binaryImage.SetPixel(j, i, System.Drawing.Color.Cyan);
                    }
                    else
                    {
                        binaryImage.SetPixel(j, i, pixel);
                    }
                }
            }

            return binaryImage;
        }
    }
}
