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
        System.Windows.Shapes.Rectangle? currHandle;
        Shape? currShape;
        List<UIElement> handles = new List<UIElement>();
        string? angiogramWholeAreaDensity;
        bool isLastDensityCalculationCorrect = false;
        int threshold = 0;
        private System.Windows.Point lastMousePos;

        public ImageEditorWindow(Bitmap angiogram, ImageSource angiogramImageSource, Bitmap? angiogramBW, ImageSource? angiogramBWImageSource, bool isBWOn, string result, int threshold)
        {
            InitializeComponent();
            sliderThreshold.Value = threshold;
            lblThreshold.Content = threshold;
            lblResult.Visibility = Visibility.Hidden;
            lblSegmentation.Visibility = Visibility.Hidden;
            btnSegmentation.Visibility = Visibility.Hidden;
            try
            {
                this.angiogram = angiogram;
                this.angiogramImageSource = angiogramImageSource;
                this.angiogramBW = angiogramBW;
                this.angiogramBWImageSource = angiogramBWImageSource;
                this.threshold = threshold;

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
                    angiogramWholeAreaDensity = result;
                    lblResult.Visibility = Visibility.Visible;
                    lblSegmentation.Visibility = Visibility.Visible;
                    btnSegmentation.Visibility = Visibility.Visible;
                }

                // Add the MouseLeftButtonDown event handler to the Image control
                mainCanvas.MouseLeftButtonDown += mainCanvas_MouseLeftButtonDown;
                mainCanvas.MouseLeftButtonUp += mainCanvas_MouseLeftButtonUp;
                mainCanvas.MouseMove += mainCanvas_MouseMove;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při načtení obrazu: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currRectangle != null)
            {
                return;
            }
            if (btnRectangleSelection.IsChecked == true)
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

                AddHandles();

                // need a new density calculation for correct export
                isLastDensityCalculationCorrect = false;
            } 
            else
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
                currShape = new Ellipse
                {
                    Width = rectangleWidth,
                    Height = rectangleHeight,
                    Stroke = System.Windows.Media.Brushes.Red,
                    StrokeThickness = 3
                };

                // Set the position of the red rectangle
                Canvas.SetLeft(currShape, rectangleStartX);
                Canvas.SetTop(currShape, rectangleStartY);

                // Add the red rectangle to the Canvas
                mainCanvas.Children.Add(currShape);

                AddHandles();

                // need a new density calculation for correct export
                isLastDensityCalculationCorrect = false;
            }
        }

        // Add resize handles
        private void AddHandles()
        {
            if (currRectangle != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    System.Windows.Shapes.Rectangle handle = new System.Windows.Shapes.Rectangle
                    {
                        Width = 10,
                        Height = 10,
                        Fill = System.Windows.Media.Brushes.Green,
                        Opacity = 0.6,
                    };

                    handles.Add(handle);

                    // Add mouse event handlers for handle dragging
                    handle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;

                    double rectangleLeft = Math.Floor(Canvas.GetLeft(currRectangle));
                    double rectangleTop = Math.Floor(Canvas.GetTop(currRectangle));

                    // Position handles at the corners and edges of the rectangle
                    switch (i)
                    {
                        case 0: // Top
                            Canvas.SetLeft(handle, rectangleLeft + Math.Floor(currRectangle.Width/2 - handle.Width/2));
                            Canvas.SetTop(handle, rectangleTop);
                            handle.Cursor = Cursors.SizeNS;
                            handle.Tag = "Top";
                            break;
                        case 1: // Bottom
                            Canvas.SetLeft(handle, rectangleLeft + Math.Floor(currRectangle.Width / 2 - handle.Width / 2));
                            Canvas.SetTop(handle, rectangleTop + currRectangle.Height + 1 - Math.Floor(handle.Height));
                            handle.Cursor = Cursors.SizeNS;
                            handle.Tag = "Bottom";
                            break;
                        case 2: // Left
                            Canvas.SetLeft(handle, rectangleLeft);
                            Canvas.SetTop(handle, rectangleTop + Math.Floor(currRectangle.Height / 2 - handle.Height / 2));
                            handle.Cursor = Cursors.SizeWE;
                            handle.Tag = "Left";
                            break;
                        case 3: // Right
                            Canvas.SetLeft(handle, rectangleLeft + currRectangle.Width + 1 - Math.Floor(handle.Width));
                            Canvas.SetTop(handle, rectangleTop + Math.Floor(currRectangle.Height / 2 - handle.Height / 2));
                            handle.Cursor = Cursors.SizeWE;
                            handle.Tag = "Right";
                            break;
                    }

                    // Add handle to the canvas
                    mainCanvas.Children.Add(handle);
                }
            }
        }
        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePos = e.GetPosition(mainCanvas);
            currHandle = (System.Windows.Shapes.Rectangle)sender;
        }

        private void mainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            currHandle = null;
        }

        private void mainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (currHandle != null)
            {
                System.Windows.Point currentMousePos = e.GetPosition(mainCanvas);
                lblX.Content = currentMousePos.X;
                lblY.Content = currentMousePos.Y;
                Vector offset = currentMousePos - lastMousePos;

                switch (currHandle.Tag)
                {
                    case "Top": // Top handle
                        double newTop = Math.Floor(Canvas.GetTop(currRectangle)) + offset.Y;
                        double newHeight = currRectangle!.Height - offset.Y;

                        if (newHeight > currRectangle.StrokeThickness*2 + 1)
                        {
                            if (newTop < 0)
                            {
                                currHandle = null;
                                break;
                            }
                            Canvas.SetTop(currRectangle, newTop);
                            currRectangle.Height = newHeight;
                            Canvas.SetTop(currHandle, Math.Floor(Canvas.GetTop(currHandle)) + offset.Y);
                        }
                        break;

                    case "Bottom": // Bottom handle
                        double newHeightBottom = currRectangle!.Height + offset.Y;

                        if (newHeightBottom > currRectangle.StrokeThickness*2 + 1)
                        {
                            if (Math.Floor(Canvas.GetTop(currRectangle)) + newHeightBottom > mainCanvas.Height)
                            {
                                currHandle = null;
                                break;
                            }
                            currRectangle.Height = newHeightBottom;
                            Canvas.SetTop(currHandle, Canvas.GetTop(currHandle) + offset.Y);
                        }
                        break;

                    case "Left": // Left handle
                        double newLeft = Math.Floor(Canvas.GetLeft(currRectangle)) + offset.X;
                        double newWidth = currRectangle!.Width - offset.X;

                        if (newWidth > currRectangle.StrokeThickness * 2 + 1)
                        {
                            if (newLeft < 0)
                            {
                                currHandle = null;
                                break;
                            }
                            Canvas.SetLeft(currRectangle, newLeft);
                            currRectangle.Width = newWidth;
                            Canvas.SetLeft(currHandle, Math.Floor(Canvas.GetLeft(currHandle)) + offset.X);
                        }
                        break;

                    case "Right": // Right handle
                        double newWidthRight = currRectangle!.Width + offset.X;

                        if (newWidthRight > currRectangle.StrokeThickness * 2 + 1)
                        {
                            if (Math.Floor(Canvas.GetLeft(currRectangle)) + newWidthRight > mainCanvas.Width)
                            {
                                currHandle = null;
                                break;
                            }
                            currRectangle.Width = newWidthRight;
                            Canvas.SetLeft(currHandle, Canvas.GetLeft(currHandle) + offset.X);
                        }
                        break;
                }

                lastMousePos = currentMousePos;
            }
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

            if (angiogramWholeAreaDensity != null)
            {
                lblResult.Content = angiogramWholeAreaDensity.ToString();
                isLastDensityCalculationCorrect = true;
            }
        }

        private void Remove_selection()
        {
            if (currRectangle != null)
            {
                mainCanvas.Children.Remove(currRectangle);
                foreach (UIElement handle in handles)
                {
                    mainCanvas.Children.Remove(handle);
                }
            }
            currRectangle = null;
        }

        private void Density_calculation_click(object sender, RoutedEventArgs e)
        {
            if (angiogramBW == null)
            {
                threshold = SharedFunctions.Otsu_Thresholding(angiogram);
                angiogramBW = SharedFunctions.Binarize_Image_By_Threshold(angiogram, threshold);
                angiogramBWImageSource = SharedFunctions.ImageSourceFromBitmap(angiogramBW);
                sliderThreshold.Value = threshold;
                lblThreshold.Content = threshold;
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
                lblResult.Content = "Hustota krevního řečiště: " + result.ToString("N2") + "%";
                angiogramWholeAreaDensity = lblResult.Content.ToString();
            }
            else
            {
                lblResult.Content = "Hustota krevního řečiště vyznačené oblasti: " + result.ToString("N2") + "%";
            }

            isLastDensityCalculationCorrect = true;
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

        private void btnRectangleSelection_Checked(object sender, RoutedEventArgs e)
        {
            if (btnEllipseSelection != null)
            {
                btnEllipseSelection.IsChecked = false;
            }
        }

        private void btnRectangleSelection_Unchecked(object sender, RoutedEventArgs e)
        {
            if (btnEllipseSelection != null)
            {
                btnEllipseSelection.IsChecked = true;
            }
        }

        private void btnEllipseSelection_Checked(object sender, RoutedEventArgs e)
        {
            if (btnRectangleSelection != null)
            {
                btnRectangleSelection.IsChecked = false;
            }
        }

        private void btnEllipseSelection_Unchecked(object sender, RoutedEventArgs e)
        {
            if (btnRectangleSelection != null)
            {
                btnRectangleSelection.IsChecked = true;
            }
        }

        private void Export_button_click(object sender, RoutedEventArgs e)
        {
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
            Bitmap outputImage = new Bitmap(Determine_Bitmap_From_ImageSource());

            if (currRectangle != null)
            {
                // Get rectangles parameters and modify them for the DrawRectangle function, which displays rectangles differently than canvas
                int rectangleStrokeThickness = (int)Math.Floor(currRectangle.StrokeThickness);
                int rectagnleWidth = (int)Math.Floor(currRectangle.Width) - rectangleStrokeThickness;
                int rectangleHeight = (int)Math.Floor(currRectangle.Height) - rectangleStrokeThickness;
                int rectangleStartX = (int)Math.Floor(Canvas.GetLeft(currRectangle)) + 1;
                int rectangleStartY = (int)Math.Floor(Canvas.GetTop(currRectangle)) + 1;

                outputImage = AddBorder(outputImage);

                using (Graphics graphics = Graphics.FromImage(outputImage))
                {
                    graphics.DrawRectangle(new System.Drawing.Pen(GetColorFromBorderBrush(currRectangle.Stroke), rectangleStrokeThickness), rectangleStartX, rectangleStartY, rectagnleWidth, rectangleHeight);
                }
            }

            if (isLastDensityCalculationCorrect)
            {
                outputImage = AddWhiteLayers(outputImage);
                using (Graphics graphics = Graphics.FromImage(outputImage))
                {
                    // Set the font and brush for the text
                    Font font = new Font("Arial", 14);
                    SolidBrush brush = new SolidBrush(System.Drawing.Color.Black);

                    // Set the position where you want to place the text
                    float x = 115;
                    if (currRectangle != null)
                    {
                        x = 30;
                    }
                    float y = 10;

                    // Set any other options (e.g., quality, smoothing, etc.)
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    // Draw the text onto the image
                    graphics.DrawString(lblResult.Content.ToString(), font, brush, x, y);

                    SharedFunctions.SaveBitmapImage(outputImage, dlg);
                }
            }
            else
            {
                SharedFunctions.SaveBitmapImage(outputImage, dlg);
            }
            outputImage.Dispose();

        }

        private Bitmap Determine_Bitmap_From_ImageSource()
        {
            if (angiogramDisplay.Source == angiogramBWImageSource)
            {
                return angiogramBW!;
            }
            return angiogram;
        }

        private Bitmap AddBorder(Bitmap outputImage)
        {
            int leftThickness = (int)borderMain.BorderThickness.Left;
            int rightThickness = (int)borderMain.BorderThickness.Right;
            int topThickness = (int)borderMain.BorderThickness.Top;
            int bottomThickness = (int)borderMain.BorderThickness.Bottom;


            Bitmap outputImageWithBorder = new Bitmap(outputImage.Width + leftThickness + rightThickness, outputImage.Height + topThickness + bottomThickness);

            System.Drawing.Color borderColor = GetColorFromBorderBrush(borderMain.BorderBrush);

            for (int i = 0; i < outputImageWithBorder.Height; ++i)
            {
                for (int j = 0; j < outputImageWithBorder.Width; ++j)
                {
                    if (i < topThickness || i >= outputImageWithBorder.Height - bottomThickness || j < leftThickness || j >= outputImageWithBorder.Width - rightThickness)   
                    {
                        outputImageWithBorder.SetPixel(j, i, borderColor);
                    }
                    else
                    {
                        outputImageWithBorder.SetPixel(j, i, outputImage.GetPixel(j - leftThickness, i - topThickness));
                    }
                }
            }
            return outputImageWithBorder;
        }

        private System.Drawing.Color GetColorFromBorderBrush(System.Windows.Media.Brush borderBrush)
        {
            // Convert the BorderBrush color to a SolidColorBrush (assuming it's a SolidColorBrush)
            SolidColorBrush solidColorBrush = (SolidColorBrush)borderBrush;

            // Convert the SolidColorBrush color to a System.Windows.Media.Color
            System.Windows.Media.Color mediaColor = solidColorBrush.Color;

            // Convert the System.Windows.Media.Color to a System.Drawing.Color
            System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);

            // Now you can use drawingColor with outputImageWithBorder.SetPixel(j, i, drawingColor)
            return drawingColor;
        }

        private Bitmap AddWhiteLayers(Bitmap outputImage)
        {
            int WHITE_LAYERS_HEIGHT = 40;

            Bitmap outputImageWithLayers = new Bitmap(outputImage.Width, outputImage.Height + WHITE_LAYERS_HEIGHT);

            for (int i = 0; i < outputImageWithLayers.Height; ++i)
            {
                for (int j = 0; j < outputImageWithLayers.Width; ++j)
                {
                    if (i < WHITE_LAYERS_HEIGHT)
                    {
                        outputImageWithLayers.SetPixel(j, i, System.Drawing.Color.White);
                    }
                    else
                    {
                        outputImageWithLayers.SetPixel(j, i, outputImage.GetPixel(j, i - WHITE_LAYERS_HEIGHT));
                    }
                }
            }

            return outputImageWithLayers;
        }

        private void sliderThreshold_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // Not chaning value case
            if (angiogramDisplay.Source == angiogramImageSource)
            {
                MessageBox.Show("Prahovou hodnotu segmentace lze měnit pouze při zobrazeném segmentovaném angiogramu.");
                sliderThreshold.Value = threshold;
                lblThreshold.Content = threshold; 
                return;
            }

            Bitmap angiogramWithChangedThreshold = SharedFunctions.Binarize_Image_By_Threshold(angiogram, (int)sliderThreshold.Value);
            ImageSource angiogramWithChangedThresholdImageSource = SharedFunctions.ImageSourceFromBitmap(angiogramWithChangedThreshold);
            angiogramDisplay.Source = angiogramWithChangedThresholdImageSource;
            lblThreshold.Content = (int)sliderThreshold.Value;
        }
    }
}
