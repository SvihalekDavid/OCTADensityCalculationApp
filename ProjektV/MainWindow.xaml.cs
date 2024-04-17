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
        const int ANGIOGRAM_ROW_START = 237, ANGIOGRAM_COLUMN_START = 519, ANGIOGRAM_ROW_END = 748, ANGIOGRAM_COLUMN_END = 1031;
        Bitmap? angiogramFullImage;
        Bitmap angiogram = new Bitmap(ANGIOGRAM_COLUMN_END - ANGIOGRAM_COLUMN_START + 1, ANGIOGRAM_ROW_END - ANGIOGRAM_ROW_START + 1);
        ImageSource? angiogramImageSource, angiogramFullImageImageSource;
        bool wasFileSelected = false;
        public MainWindow()
        {
            InitializeComponent();
            lblResult.Visibility = Visibility.Hidden;
        }

        // Getting OCTA angiogram from a file functionality
        private void Choose_File_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new();
            fileDialog.Filter = "Podporované obrazové formáty (PNG, TIFF) | *.png;*.tif;*.tiff";

            bool? success = fileDialog.ShowDialog();

            if (success == true)
            {
                try
                {
                    angiogramFullImage = new Bitmap(fileDialog.FileName);
             
                    // cut the angiograms relevant part
                    for (int i = ANGIOGRAM_ROW_START; i <= ANGIOGRAM_ROW_END; ++i)
                    {
                        for (int j = ANGIOGRAM_COLUMN_START; j <= ANGIOGRAM_COLUMN_END; ++j)
                        {
                            angiogram.SetPixel(j - ANGIOGRAM_COLUMN_START, i - ANGIOGRAM_ROW_START, angiogramFullImage.GetPixel(j, i));
                        }
                    }
                    angiogramFullImageImageSource = ImageSourceFromBitmap(angiogramFullImage);
                    angiogramImageSource = ImageSourceFromBitmap(angiogram);

                    // hide labels
                    lblNoContent.Visibility = Visibility.Hidden;
                    lblResult.Visibility = Visibility.Hidden;

                    img1.Source = angiogramFullImageImageSource;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba při načtení obrazu: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                wasFileSelected = true;
            }
            else
            {
                MessageBox.Show("Nebyl vybrán žádný soubor.");
                lblNoContent.Visibility = Visibility.Visible;
            }
        }

        private void Calculate_Density_Button_Click(object sender, RoutedEventArgs e)
        {
            // Check for no file cases
            if (!wasFileSelected)
            {
                MessageBox.Show("Nebyl vybrán žádný soubor.");
                return;
            }

            double sumOfAllColors = 0;
            double sumOfAllPixels = 0;

            for (int i = 237; i <= 748; ++i)
            {
                for (int j = 519; j <= 1031; ++j)
                {
                    System.Drawing.Color pixel = angiogramFullImage!.GetPixel(j, i);
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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!wasFileSelected)
            {
                MessageBox.Show("Nebyl vybrán žádný soubor.");
                return;
            }

            // Otsu's thresholding to convert the image to binary
            Bitmap binaryImg = Otsu_Thresholding(angiogramFullImage!);

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

        private void Export_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!wasFileSelected)
            {
                MessageBox.Show("Nebyl vybrán žádný soubor");
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

            // Create a Graphics object from the image
            using (Graphics graphics = Graphics.FromImage(angiogramFullImage!))
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
                angiogramFullImage!.Save(path, ImageFormat.Png);
            }
        }

        private void Editor_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!wasFileSelected)
            {
                ImageEditorWindow editorWindow = new ImageEditorWindow(angiogram, angiogramImageSource!);
                editorWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Nebyl vybrán žádný soubor.");
            }
        }

        private Bitmap Otsu_Thresholding(Bitmap image)
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
