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
using System.Windows.Automation.Provider;
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
        Bitmap angiogram;
        ImageSource angiogramImageSource;
        Bitmap? angiogramBW;
        ImageSource? angiogramBWImageSource;
        System.Windows.Shapes.Rectangle? currRectangle;
        string? angiogramDensity;

        public ImageEditorWindow(Bitmap angiogram, ImageSource angiogramImageSource, Bitmap? angiogramBW, ImageSource? angiogramBWImageSource, bool isBWOn, string result)
        {
            InitializeComponent();
            lblResult.Visibility = Visibility.Hidden;
            lblSegmentation.Visibility = Visibility.Hidden;
            btnSegmentation.Visibility = Visibility.Hidden;
            try
            {
                this.angiogram = angiogram;
                this.angiogramImageSource = angiogramImageSource;
                this.angiogramBW = angiogramBW;
                this.angiogramBWImageSource = angiogramBWImageSource;

                if (isBWOn)
                {
                    angiogramDisplay.Source = angiogramBWImageSource;
                    btnSegmentation.IsChecked = true;

                }
                else
                {
                    angiogramDisplay.Source = angiogramImageSource;
                    btnSegmentation.IsChecked = false;
                }

                if (angiogramBW != null)
                {
                    lblResult.Content = result;
                    angiogramDensity = result;
                    lblResult.Visibility = Visibility.Visible;
                    lblSegmentation.Visibility = Visibility.Visible;
                    btnSegmentation.Visibility = Visibility.Visible;
                }

                // Add the MouseLeftButtonDown event handler to the Image control
                mainCanvas.MouseLeftButtonDown += mainCanvas_MouseLeftButtonDown;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při načtení obrazu: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get the mouse click position relative to the Image control
            System.Windows.Point clickPoint = e.GetPosition(mainCanvas);

            // Calculate the rectangle coordinates with the mouse click as the center
            double rectangleWidth = 100;
            double rectangleHeight = 100;
            double rectangleStartX = clickPoint.X - rectangleWidth / 2;
            double rectangleStartY = clickPoint.Y - rectangleHeight / 2;

            rectangleStartX = Correct_Rectangles_Start_Coordinate_To_Be_Within_Canvas_Bounds(rectangleStartX, mainCanvas.ActualWidth, rectangleWidth);
            rectangleStartY = Correct_Rectangles_Start_Coordinate_To_Be_Within_Canvas_Bounds(rectangleStartY, mainCanvas.ActualHeight, rectangleHeight);

            // Delete existing rectangle
            if (currRectangle != null)
            {
                mainCanvas.Children.Remove(currRectangle);
            }

            // Create a red rectangle
            currRectangle = new System.Windows.Shapes.Rectangle
            {
                Width = rectangleWidth,
                Height = rectangleHeight,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 3
            };

            // Set the position of the red rectangle
            Canvas.SetLeft(currRectangle, rectangleStartX);
            Canvas.SetTop(currRectangle, rectangleStartY);

            // Add the red rectangle to the Canvas
            mainCanvas.Children.Add(currRectangle);
        }

        private double Correct_Rectangles_Start_Coordinate_To_Be_Within_Canvas_Bounds(double startCoordinate, double maxCoordinateValue, double rectangleCoordinateSize)
        {
            if (startCoordinate < 0)
            {
                startCoordinate = 0;
            }
            else if (startCoordinate + rectangleCoordinateSize > maxCoordinateValue)
            {
                startCoordinate = maxCoordinateValue - rectangleCoordinateSize;
            }
            return startCoordinate;
        }

        private void Zoom_button_click(object sender, RoutedEventArgs e)
        {
            if (currRectangle != null)
            {
                int croppedAngiogramWidth = (int)Math.Floor(currRectangle.Width - (2 * currRectangle.StrokeThickness));
                int croppedAngiogramHeight = (int)Math.Floor(currRectangle.Height - (2 * currRectangle.StrokeThickness));

                int rectangleStartX = (int)Math.Floor(Canvas.GetLeft(currRectangle));
                int rectangleStartY = (int)Math.Floor(Canvas.GetTop(currRectangle));

                Bitmap croppedAngiogram = new Bitmap(croppedAngiogramWidth, croppedAngiogramHeight);

                // cut the angiograms relevant part
                for (int i = rectangleStartY; i < rectangleStartY + croppedAngiogramHeight; ++i)
                {
                    for (int j = rectangleStartX; j < rectangleStartX + croppedAngiogramWidth; ++j)
                    {
                        croppedAngiogram.SetPixel(j - rectangleStartX, i - rectangleStartY, angiogram.GetPixel(j, i));
                    }
                }
                ImageSource croppedAngiogramImageSource = SharedFunctions.ImageSourceFromBitmap(croppedAngiogram);
                angiogramDisplay.Source = croppedAngiogramImageSource;
                angiogramDisplay.Width = angiogram.Width;
                angiogramDisplay.Height = angiogram.Height;
            }
        }

        private Bitmap? Get_CroppedAngiogram_By_Selection()
        {
            if (currRectangle != null)
            {
                int croppedAngiogramWidth = (int)Math.Floor(currRectangle.Width - (2 * currRectangle.StrokeThickness));
                int croppedAngiogramHeight = (int)Math.Floor(currRectangle.Height - (2 * currRectangle.StrokeThickness));

                int rectangleStartX = (int)Math.Floor(Canvas.GetLeft(currRectangle));
                int rectangleStartY = (int)Math.Floor(Canvas.GetTop(currRectangle));

                Bitmap croppedAngiogram = new Bitmap(croppedAngiogramWidth, croppedAngiogramHeight);

                // cut the angiograms relevant part
                for (int i = rectangleStartY; i < rectangleStartY + croppedAngiogramHeight; ++i)
                {
                    for (int j = rectangleStartX; j < rectangleStartX + croppedAngiogramWidth; ++j)
                    {
                        croppedAngiogram.SetPixel(j - rectangleStartX, i - rectangleStartY, angiogramBW!.GetPixel(j, i));
                    }
                }

                return croppedAngiogram;
            }

            return null;
        }

        private void Remove_selection_click(object sender, RoutedEventArgs e)
        {
            Remove_selection();

            if (angiogramDensity != null)
            {
                lblResult.Content = angiogramDensity.ToString();
            }
        }

        private void Remove_selection()
        {
            if (currRectangle != null)
            {
                mainCanvas.Children.Remove(currRectangle);
            }
            currRectangle = null;
        }

        private void Density_calculation_click(object sender, RoutedEventArgs e)
        {
            if (angiogramBW == null)
            {
                angiogramBW = SharedFunctions.Otsu_Thresholding(angiogram);
                angiogramBWImageSource = SharedFunctions.ImageSourceFromBitmap(angiogramBW);
            }

            // Calculate only selected area
            Bitmap? angiogramForCalculation = Get_CroppedAngiogram_By_Selection();

            if (angiogramForCalculation == null)
            {
                angiogramForCalculation = angiogramBW;
            }

            double result = SharedFunctions.Density_Calculation(angiogramForCalculation);

            if (angiogramForCalculation == angiogramBW)
            {
                lblResult.Content = "Hustota krevního řečiště: " + result.ToString("N2") + " %";
                angiogramDensity = lblResult.Content.ToString();
            }
            else
            {
                lblResult.Content = "Hustota krevního řečiště vyznačené oblasti: " + result.ToString("N2") + " %";
            }
            lblResult.Visibility = Visibility.Visible;
            lblSegmentation.Visibility = Visibility.Visible;
            btnSegmentation.Visibility = Visibility.Visible;
        }
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            angiogramDisplay.Source = angiogramBWImageSource;
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            angiogramDisplay.Source = angiogramImageSource;
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
            //BitmapSource bitmapWithRows = ConvertCroppedBitmapToBitmapSourceWithRows(currCroppedBitmap, 30, txt.Substring(25));

            // Add the BitmapSource to the encoder
            //encoder.Frames.Add(BitmapFrame.Create(bitmapWithRows));

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
            angiogramDisplay.Source = renderTargetBitmap;

            return renderTargetBitmap;
        }
    }
}
