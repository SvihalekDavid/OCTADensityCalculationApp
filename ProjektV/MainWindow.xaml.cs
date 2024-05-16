using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Media;

namespace OCTADensityCalculationApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Constants indicating angiograms relevant part
        const int ANGIOGRAM_ROW_START = 0, ANGIOGRAM_COLUMN_START = 266, ANGIOGRAM_ROW_END = 963, ANGIOGRAM_COLUMN_END = 1233;
        Bitmap? angiogram, angiogramFullImage, angiogramBW, angiogramBWFullImage;
        ImageSource? angiogramImageSource, angiogramFullImageImageSource, angiogramBWImageSource, angiogramBWFullImageImageSource;
        bool wasFileSelected = false;
        int threshold = -1;
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
                    angiogram = new Bitmap(ANGIOGRAM_COLUMN_END - ANGIOGRAM_COLUMN_START + 1, ANGIOGRAM_ROW_END - ANGIOGRAM_ROW_START + 1);

                    // cut the angiograms relevant part
                    for (int i = ANGIOGRAM_ROW_START; i <= ANGIOGRAM_ROW_END; ++i)
                    {
                        for (int j = ANGIOGRAM_COLUMN_START; j <= ANGIOGRAM_COLUMN_END; ++j)
                        {
                             angiogram.SetPixel(j - ANGIOGRAM_COLUMN_START, i - ANGIOGRAM_ROW_START, angiogramFullImage.GetPixel(j, i));
                        }
                    }
                    angiogramFullImageImageSource = SharedFunctions.ImageSourceFromBitmap(angiogramFullImage);
                    angiogramImageSource = SharedFunctions.ImageSourceFromBitmap(angiogram);

                    // hide labels and controls which cannot be accessed when a new file is selected
                    lblNoContent.Visibility = Visibility.Hidden;
                    lblResult.Visibility = Visibility.Hidden;
                    lblSegmentation.Visibility = Visibility.Hidden;
                    btnSegmentation.Visibility = Visibility.Hidden;
                    btnSegmentation.IsChecked = false;

                    imageMain.Source = angiogramFullImageImageSource;

                    // null any previous BW images
                    angiogramBW = null;
                    angiogramBWFullImage = null;
                    angiogramBWImageSource = null;
                    angiogramBWFullImageImageSource = null;
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
                if (wasFileSelected)
                {
                    return;
                }
                MessageBox.Show("Nebyl vybrán žádný soubor.");
                lblNoContent.Visibility = Visibility.Visible;
            }
        }

        // Blood vessel density calculation
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
            threshold = SharedFunctions.Otsu_Thresholding(angiogram!);
            angiogramBW = SharedFunctions.Binarize_Image_By_Threshold(angiogram!, threshold);

            // Create a new viewable binarized angiogram
            Create_AngiogramFull_From_AngiogramBW();
            angiogramBWImageSource = SharedFunctions.ImageSourceFromBitmap(angiogramBW);
            angiogramBWFullImageImageSource = SharedFunctions.ImageSourceFromBitmap(angiogramBWFullImage!);

            lblResult.Content = "Hustota krevního řečiště: " + SharedFunctions.Density_Calculation(angiogramBW).ToString("N2") + "%";

            lblResult.Visibility = Visibility.Visible;
            lblSegmentation.Visibility = Visibility.Visible;
            btnSegmentation.Visibility = Visibility.Visible;
        }

        private void Export_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!wasFileSelected)
            {
                MessageBox.Show("Nebyl vybrán žádný soubor");
                return;
            }
            if (lblResult.Visibility != Visibility.Visible)
            {
                MessageBox.Show("Neproběhl výpočet hustoty krevního řečiště");
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "Podporované obrazové formáty (PNG, TIFF) | *.png;*.tif;*.tiff"; ; // Filter files by extension

            // Hook up the FileOk event handler
            dlg.FileOk += SharedFunctions.SaveFileDialog_FileOk!;


            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == false)
            {
                return;
            }

            // Create a Graphics object from the image

            Bitmap outputImage = Determine_Bitmap_From_ImageSource();

            outputImage = SharedFunctions.AddWhiteLayers(outputImage);

            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                // Set the font and brush for the text
                Font font = new Font("Arial", 14);
                SolidBrush brush = new SolidBrush(System.Drawing.Color.Black);

                // Set the position where you want to place the text
                float x = 610;
                float y = 10;

                // Set any other options (e.g., quality, smoothing, etc.)
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Draw the text onto the image
                graphics.DrawString(lblResult.Content.ToString(), font, brush, x, y);

                // Save the image with the text
                SharedFunctions.SaveBitmapImage(outputImage, dlg);
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

        private void Editor_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!wasFileSelected)
            {
                MessageBox.Show("Nebyl vybrán žádný soubor");
                return;
            }
            ImageEditorWindow editorWindow = new ImageEditorWindow(angiogram!, angiogramImageSource!, angiogramBW, angiogramBWImageSource, imageMain.Source == angiogramBWFullImageImageSource, lblResult.Content.ToString()!, threshold);
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
    }
}
