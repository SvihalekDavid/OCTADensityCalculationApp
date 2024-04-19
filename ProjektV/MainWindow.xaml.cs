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
using System.ComponentModel;
using Windows.Services.Maps.LocalSearch;

namespace ProjektV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int ANGIOGRAM_ROW_START = 237, ANGIOGRAM_COLUMN_START = 519, ANGIOGRAM_ROW_END = 748, ANGIOGRAM_COLUMN_END = 1031;
        Bitmap? angiogramFullImage, angiogramBW, angiogramBWFullImage;
        Bitmap angiogram = new Bitmap(ANGIOGRAM_COLUMN_END - ANGIOGRAM_COLUMN_START + 1, ANGIOGRAM_ROW_END - ANGIOGRAM_ROW_START + 1);
        ImageSource? angiogramImageSource, angiogramFullImageImageSource, angiogramBWImageSource, angiogramBWFullImageImageSource;
        bool wasFileSelected = false;
        public MainWindow()
        {
            InitializeComponent();
            lblResult.Visibility = Visibility.Hidden;
            lblSegmentation.Visibility = Visibility.Hidden;
            btnSegmentation.Visibility = Visibility.Hidden;
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

                    // hide labels and controls which cannot be accessed when a new file is selected
                    lblNoContent.Visibility = Visibility.Hidden;
                    lblResult.Visibility = Visibility.Hidden;
                    lblSegmentation.Visibility = Visibility.Hidden;
                    btnSegmentation.Visibility = Visibility.Hidden;
                    btnSegmentation.IsChecked = false;

                    imageMain.Source = angiogramFullImageImageSource;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba při načtení obrazu: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (lblResult.Visibility == Visibility.Visible)
            {
                return;
            }

            // Otsu's thresholding to convert the image to binary
            angiogramBW = Otsu_Thresholding(angiogram);

            // Create a new viewable binarized angiogram
            Create_AngiogramFull_From_AngiogramBW();
            angiogramBWImageSource = ImageSourceFromBitmap(angiogramBW);
            angiogramBWFullImageImageSource = ImageSourceFromBitmap(angiogramBWFullImage!);

            // Counting white pixels in the binary image
            const int MAX_VAL = 255;
            int whitePixelCount = Return_Number_Of_Pixels_Of_Value(angiogramBW, MAX_VAL);

            int pixelCount = angiogramBW.Width * angiogramBW.Height;

            double whitePixelPercentage = ((double)whitePixelCount / pixelCount) * 100;
            lblResult.Content = "Hustota krevního řečiště: " + whitePixelPercentage.ToString("N2") + " %";

            lblResult.Visibility = Visibility.Visible;
            lblSegmentation.Visibility = Visibility.Visible;
            btnSegmentation.Visibility = Visibility.Visible;
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
            dlg.Filter = "Podporované obrazové formáty (PNG, TIFF) | *.png;*.tif;*.tiff"; ; // Filter files by extension

            // Hook up the FileOk event handler
            dlg.FileOk += SaveFileDialog_FileOk!;


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

            Bitmap outputImage = Determine_Bitmap_From_ImageSource();

            using (Graphics graphics = Graphics.FromImage(outputImage))
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
                string selectedExtension = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                if (selectedExtension == ".png")
                {
                    outputImage.Save(path, ImageFormat.Png);
                }
                else
                {
                    outputImage.Save(path, ImageFormat.Tiff);
                }
            }
        }

        private Bitmap Determine_Bitmap_From_ImageSource()
        {
            if (imageMain.Source == angiogramFullImageImageSource)
            {
                return angiogramFullImage!;
            }
            return angiogramBWFullImage!;
        }

        private void SaveFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            SaveFileDialog? dlg = sender as SaveFileDialog;
            if (dlg != null)
            {
                string selectedExtension = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                if (selectedExtension != ".png" && selectedExtension != ".tif" && selectedExtension != ".tiff")
                {
                    MessageBox.Show("Neplatný typ souboru. Vyberte prosím soubor s příponou PNG nebo TIFF.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Cancel = true; // Cancel the file dialog
                }
            }
        }

        private void Editor_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!wasFileSelected)
            {
                MessageBox.Show("Nebyl vybrán žádný soubor");
                return;
            }
            ImageEditorWindow editorWindow = new ImageEditorWindow(angiogram, angiogramImageSource!);
            editorWindow.ShowDialog();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            imageMain.Source = angiogramBWFullImageImageSource;
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            imageMain.Source = angiogramFullImageImageSource;
        }

        private int Return_Number_Of_Pixels_Of_Value(Bitmap img, int value)
        {
            int result = 0;

            for (int i = 0; i < img.Height; ++i)
            {
                for (int j = 0; j < img.Width; ++j)
                {
                    if (img.GetPixel(j, i).R == value)
                    {
                        result++;
                    }
                }
            }

            return result;
        }

        private void Create_AngiogramFull_From_AngiogramBW()
        {
            angiogramBWFullImage = new Bitmap(angiogramFullImage!);

            for (int i = ANGIOGRAM_ROW_START; i <= ANGIOGRAM_ROW_END; ++i)
            {
                for (int j = ANGIOGRAM_COLUMN_START; j <= ANGIOGRAM_COLUMN_END; ++j)
                {
                    angiogramBWFullImage.SetPixel(j, i, angiogramBW!.GetPixel(j - ANGIOGRAM_COLUMN_START, i - ANGIOGRAM_ROW_START));
                }
            }
        }

        private Bitmap Otsu_Thresholding(Bitmap image)
        {

            // Compute histogram
            int[] histogram = new int[256];
            for (int i = 0; i < angiogram.Height; ++i)
            {
                for (int j = 0; j < angiogram.Width; ++j)
                {
                    System.Drawing.Color pixel = image.GetPixel(j, i);
                    histogram[pixel.R]++;
                }
            }
            // Total number of pixels
            int totalPixels = angiogram.Width * angiogram.Height;

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
            Bitmap binaryImage = new Bitmap(angiogram.Width, angiogram.Height);

            for (int i = 0; i < angiogram.Height; i++)
            {
                for (int j = 0; j < angiogram.Width; j++)
                {
                    if (angiogram.GetPixel(j, i).R <= threshold)
                    {
                        binaryImage.SetPixel(j, i, System.Drawing.Color.Black);
                    }
                    else
                    {
                        binaryImage.SetPixel(j, i, System.Drawing.Color.White);
                    }
                }
            }

            return binaryImage;
        }
    }
}
