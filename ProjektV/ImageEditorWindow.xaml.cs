using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using static System.Net.Mime.MediaTypeNames;

namespace ProjektV
{
    /// <summary>
    /// Interaction logic for ImageEditorWindow.xaml
    /// </summary>
    public partial class ImageEditorWindow : Window
    {
        string imagePath = "";
        CroppedBitmap croppedBitmap;
        CroppedBitmap currCroppedBitmap;
        System.Windows.Shapes.Rectangle currRectangle;
        double currX = 0;
        double currY = 0;
        public ImageEditorWindow(string imagePath)
        {
            InitializeComponent();
            lblResult.Visibility = Visibility.Hidden;
            this.imagePath = imagePath;
            try
            {
                BitmapImage bitmap = new BitmapImage(new Uri(imagePath));
                // Crop the original image
                croppedBitmap = new CroppedBitmap(
                    bitmap,
                    new Int32Rect(519, 237, 1031 - 518, 748 - 236)
                );


                // <Image x:Name="img1" Stretch="Fill" Margin="0,26,0,0"/>
                // Set the cropped image as the source for the Image control
                img1.Source = croppedBitmap;

                // Add the MouseLeftButtonDown event handler to the Image control
                mainCanvas.MouseLeftButtonDown += Img1_MouseLeftButtonDown;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Img1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get the mouse click position relative to the Image control
            System.Windows.Point clickPoint = e.GetPosition(imageVb);
            var point = PointToScreen(clickPoint);

            // Calculate the rectangle coordinates with the mouse click as the center
            double rectangleWidth = 100;
            double rectangleHeight = 100;
            double rectangleX = clickPoint.X - rectangleWidth / 2;
            double rectangleY = clickPoint.Y - rectangleHeight / 2;

            if (rectangleX <= 0)
            {
                rectangleX = 0;
            }
            if (rectangleY <= 0)
            {
                rectangleY = 0;
            }

            System.Windows.Shapes.Rectangle existingRedRectangle = mainCanvas.Children.OfType<System.Windows.Shapes.Rectangle>().FirstOrDefault();
            if (existingRedRectangle != null)
            {
                mainCanvas.Children.Remove(existingRedRectangle);
            }

            // Create a red rectangle
            System.Windows.Shapes.Rectangle redRectangle = new System.Windows.Shapes.Rectangle
            {
                Width = rectangleWidth,
                Height = rectangleHeight,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 2
            };

            // Set the position of the red rectangle
            Canvas.SetLeft(redRectangle, rectangleX);
            Canvas.SetTop(redRectangle, rectangleY);

            // Add the red rectangle to the Canvas
            mainCanvas.Children.Add(redRectangle);
            currRectangle = redRectangle;
            currX = rectangleX;
            currY = rectangleY;
        }

        private void Confirm_button_click(object sender, RoutedEventArgs e)
        {
            CroppedBitmap newCroppedBitmap = new CroppedBitmap(
                croppedBitmap,
                new Int32Rect((int)currX, (int)currY, (int)currRectangle.Width, (int)currRectangle.Height)
            );
            img1.Source = newCroppedBitmap;
            currCroppedBitmap = newCroppedBitmap;
            Remove_selection();
        }

        private void Remove_selection_click(object sender, RoutedEventArgs e)
        {
            img1.Source = croppedBitmap;
            Remove_selection();
        }

        private void Remove_selection()
        {
            System.Windows.Shapes.Rectangle existingRedRectangle = mainCanvas.Children.OfType<System.Windows.Shapes.Rectangle>().FirstOrDefault();
            if (existingRedRectangle != null)
            {
                mainCanvas.Children.Remove(existingRedRectangle);
            }
        }

        private void Density_calculation_click(object sender, RoutedEventArgs e)
        {
            double sumOfAllColors = 0;
            double sumOfAllPixels = 0;

            for (int i = 0; i <= currCroppedBitmap.Height; ++i)
            {
                for (int j = 0; j <= currCroppedBitmap.Width; ++j)
                {
                    System.Drawing.Color pixel = GetPixelColor(currCroppedBitmap, j, i);
                    sumOfAllColors += pixel.R;
                    ++sumOfAllPixels;
                }
            }
            double avgColor = sumOfAllColors / sumOfAllPixels;

            double avgColorInPercentage = (100 * avgColor) / 255;

            lblResult.Content = "Hustota krevního řečiště: " + avgColorInPercentage.ToString("N2") + " %";
            lblResult.Visibility = Visibility.Visible;


        }

        private System.Drawing.Color GetPixelColor(CroppedBitmap croppedBitmap, int x, int y)
        {
            // Define the size of the array based on the cropped bitmap dimensions
            int stride = croppedBitmap.PixelWidth * (croppedBitmap.Format.BitsPerPixel / 8);
            byte[] pixelData = new byte[stride * croppedBitmap.PixelHeight];

            // Copy pixel data from the cropped bitmap to the array
            croppedBitmap.CopyPixels(pixelData, stride, 0);

            // Calculate the index of the desired pixel in the array
            int index = y * stride + x * (croppedBitmap.Format.BitsPerPixel / 8);

            // Extract color components from the array
            byte blue = pixelData[index];
            byte green = pixelData[index + 1];
            byte red = pixelData[index + 2];
            byte alpha = pixelData[index + 3];

            // Create a Color object from the components
            System.Drawing.Color pixelColor = System.Drawing.Color.FromArgb(alpha, red, green, blue);

            return pixelColor;
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
            dlg.Filter = "PNG Files (*.png)|*.png"; // Filter files by extension

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

            // Create a PngBitmapEncoder
            PngBitmapEncoder encoder = new PngBitmapEncoder();

            // Convert CroppedBitmap to BitmapSource with additional rows
            string txt = lblResult.Content.ToString();
            BitmapSource bitmapWithRows = ConvertCroppedBitmapToBitmapSourceWithRows(currCroppedBitmap, 30, txt.Substring(25));

            // Add the BitmapSource to the encoder
            encoder.Frames.Add(BitmapFrame.Create(bitmapWithRows));

            // Save the encoder to the specified path
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        private BitmapSource ConvertCroppedBitmapToBitmapSourceWithRows(CroppedBitmap croppedBitmap, int additionalRows, string text)
        {
            // Create a DrawingVisual and set its properties
            DrawingVisual drawingVisual = new DrawingVisual();

            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Draw the original CroppedBitmap
                drawingContext.DrawImage(croppedBitmap, new Rect(0, additionalRows, croppedBitmap.PixelWidth, croppedBitmap.PixelHeight));

                // Draw text on the image
                Typeface typeface = new Typeface(new System.Windows.Media.FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 14, System.Windows.Media.Brushes.Black);
                drawingContext.DrawText(formattedText, new System.Windows.Point(23, 12));
            }

            // Convert the DrawingVisual to a RenderTargetBitmap
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
                croppedBitmap.PixelWidth,
                croppedBitmap.PixelHeight + additionalRows,
                croppedBitmap.DpiX,
                croppedBitmap.DpiY,
                PixelFormats.Pbgra32);

            renderTargetBitmap.Render(drawingVisual);

            // Set the RenderTargetBitmap as the source for img1
            img1.Source = renderTargetBitmap;

            return renderTargetBitmap;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Bitmap img = new Bitmap(imagePath);
            Bitmap binaryImage = new Bitmap(1031 - 518, 748 - 236);

            for (int i = 237; i <= 748; ++i)
            {
                for (int j = 519; j <= 1031; ++j)
                {
                    System.Drawing.Color pixel = img.GetPixel(j, i);
                    binaryImage.SetPixel(j - 519, i - 237, pixel);
                }
            }

            if (imagePath == "")
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

            binaryImage.Save(path, ImageFormat.Png);

            // Dispose of the original image after saving
            img.Dispose();
            binaryImage.Dispose();

        }
    }
}
