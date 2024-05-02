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
using System.Text.Json;

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
            if (currShape != null)
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
                if (currShape != null)
                {
                    mainCanvas.Children.Remove(currShape);
                }

                // Create a red rectangle
                currShape = new System.Windows.Shapes.Rectangle
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
                if (currShape != null)
                {
                    mainCanvas.Children.Remove(currShape);
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
            if (currShape != null)
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

                    // Addition to the list of handles used to track them in the main canvas
                    handles.Add(handle);

                    // Add mouse event handlers for handle dragging
                    handle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;

                    // Position handles at the corners and edges of the rectangle
                    switch (i)
                    {
                        case 0: // Top
                            handle.Tag = "Top";
                            handle.Cursor = Cursors.SizeNS;
                            break;
                        case 1: // Bottom
                            handle.Tag = "Bottom";
                            handle.Cursor = Cursors.SizeNS;
                            break;
                        case 2: // Left
                            handle.Tag = "Left";
                            handle.Cursor = Cursors.SizeWE;
                            break;
                        case 3: // Right
                            handle.Tag = "Right";
                            handle.Cursor = Cursors.SizeWE;
                            break;
                    }

                    int shapeLeft = (int)Math.Floor(Canvas.GetLeft(currShape));
                    int shapeTop = (int)Math.Floor(Canvas.GetTop(currShape));

                    // Make handle centered
                    Center_Handle_To_Selection(handle, shapeLeft, shapeTop, currShape);

                    // Add handle to the canvas
                    mainCanvas.Children.Add(handle);
                }
            }
        }
        private void Center_Handle_To_Selection(System.Windows.Shapes.Rectangle handle, int shapeLeft, int shapeTop, Shape shape)
        {
            switch (handle.Tag)
            {
                case "Top":
                    Canvas.SetLeft(handle, shapeLeft + Math.Floor(shape.Width / 2 - handle.Width / 2));
                    Canvas.SetTop(handle, shapeTop);
                    break;
                case "Bottom":
                    Canvas.SetLeft(handle, shapeLeft + Math.Floor(shape.Width / 2 - handle.Width / 2));
                    Canvas.SetTop(handle, shapeTop + shape.Height + 1 - Math.Floor(handle.Height));
                    break;
                case "Left":
                    Canvas.SetLeft(handle, shapeLeft);
                    Canvas.SetTop(handle, shapeTop + Math.Floor(shape.Height / 2 - handle.Height / 2));
                    break;
                case "Right":
                    Canvas.SetLeft(handle, shapeLeft + shape.Width + 1 - Math.Floor(handle.Width));
                    Canvas.SetTop(handle, shapeTop + Math.Floor(shape.Height / 2 - handle.Height / 2));
                    break;
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
                Vector offset = currentMousePos - lastMousePos;

                int shapeLeft = (int)Math.Floor(Canvas.GetLeft(currShape));
                int shapeTop = (int)Math.Floor(Canvas.GetTop(currShape));

                switch (currHandle.Tag)
                {
                    case "Top": // Top handle
                        double newTop = Math.Floor(Canvas.GetTop(currShape)) + offset.Y;
                        double newHeight = currShape!.Height - offset.Y;

                        if (newHeight > currShape.StrokeThickness*2 + 1)
                        {
                            if (newTop < 0)
                            {
                                currHandle = null;
                                break;
                            }
                            Canvas.SetTop(currShape, newTop);
                            currShape.Height = newHeight;
                            Canvas.SetTop(currHandle, Math.Floor(Canvas.GetTop(currHandle)) + offset.Y);
                        }
                        break;

                    case "Bottom": // Bottom handle
                        double newHeightBottom = currShape!.Height + offset.Y;

                        if (newHeightBottom > currShape.StrokeThickness*2 + 1)
                        {
                            if (Math.Floor(Canvas.GetTop(currShape)) + newHeightBottom > mainCanvas.Height)
                            {
                                currHandle = null;
                                break;
                            }
                            currShape.Height = newHeightBottom;
                            Canvas.SetTop(currHandle, Canvas.GetTop(currHandle) + offset.Y);
                        }
                        break;

                    case "Left": // Left handle
                        double newLeft = Math.Floor(Canvas.GetLeft(currShape)) + offset.X;
                        double newWidth = currShape!.Width - offset.X;

                        if (newWidth > currShape.StrokeThickness * 2 + 1)
                        {
                            if (newLeft < 0)
                            {
                                currHandle = null;
                                break;
                            }
                            Canvas.SetLeft(currShape, newLeft);
                            currShape.Width = newWidth;
                            Canvas.SetLeft(currHandle, Math.Floor(Canvas.GetLeft(currHandle)) + offset.X);
                        }
                        break;

                    case "Right": // Right handle
                        double newWidthRight = currShape!.Width + offset.X;

                        if (newWidthRight > currShape.StrokeThickness * 2 + 1)
                        {
                            if (Math.Floor(Canvas.GetLeft(currShape)) + newWidthRight > mainCanvas.Width)
                            {
                                currHandle = null;
                                break;
                            }
                            currShape.Width = newWidthRight;
                            Canvas.SetLeft(currHandle, Canvas.GetLeft(currHandle) + offset.X);
                        }
                        break;
                }

                foreach (System.Windows.Shapes.Rectangle handle in handles)
                {
                    Center_Handle_To_Selection(handle, shapeLeft, shapeTop, currShape!);
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
            if (currShape != null)
            {
                int croppedAngiogramWidth = (int)Math.Floor(currShape.Width - (2 * currShape.StrokeThickness));
                int croppedAngiogramHeight = (int)Math.Floor(currShape.Height - (2 * currShape.StrokeThickness));

                int rectangleStartX = (int)Math.Floor(Canvas.GetLeft(currShape));
                int rectangleStartY = (int)Math.Floor(Canvas.GetTop(currShape));

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
            if (currShape != null)
            {
                mainCanvas.Children.Remove(currShape);
                foreach (UIElement handle in handles)
                {
                    mainCanvas.Children.Remove(handle);
                }
            }
            currShape = null;
        }

        private Bitmap GetPixelsInsideEllipse(double left, double top, double width, double height, Bitmap image)
        {
            bool isFirstRedFound = false;
            bool isFirstRedEnded = false;
            bool isRedColored = false;
            System.Drawing.Color color;

            List<System.Windows.Point> points = new List<System.Windows.Point>();

            for (int x = 0; x < image.Height; x++)
            {
                isFirstRedFound = false;
                isFirstRedEnded = false;

                for (int y = 0; y < image.Width; y++)
                {
                    color = image.GetPixel(y, x);

                    isRedColored = color.R != color.B || color.R != color.G || color.B != color.G;
                    if (!isFirstRedFound)
                    {
                        if (isRedColored)
                        {
                            isFirstRedFound = true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (!isFirstRedEnded)
                    {
                        if (isRedColored)
                        {
                            continue;
                        }
                        else
                        {
                            isFirstRedEnded = true;
                        }
                    }
                    if (isRedColored)
                    {
                        foreach (var p in points)
                        {
                            image.SetPixel((int)p.X, (int)p.Y, System.Drawing.Color.Green);
                        }
                        points.Clear();
                        break;
                    }
                    else
                    {
                        points.Add(new System.Windows.Point(y, x));
                    }
                }
                points.Clear();
            }

            return image;
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
            if (currShape is Ellipse)
            {
                Bitmap angiogramWithEllipse = Draw_Shape_Into_Angiogram(angiogramBW);
                Bitmap temp = GetPixelsInsideEllipse(Canvas.GetLeft(currShape), Canvas.GetTop(currShape), currShape.Width, currShape.Height, angiogramWithEllipse);
                angiogramDisplay.Source = SharedFunctions.ImageSourceFromBitmap(temp);
                //List<int> pixels = Get_All_Pixels_From_Selection(angiogramWithEllipse);
                //MessageBox.Show(pixels.Count().ToString());
                return;
            }

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
        private List<int> Get_All_Pixels_From_Selection(Bitmap image)
        {
            List<int> list = new List<int>();
            int rectangleWidth = (int)Math.Floor(currShape.Width);
            int rectangleHeight = (int)Math.Floor(currShape.Height);
            int rectangleStartX = (int)Math.Floor(Canvas.GetLeft(currShape));
            int rectangleStartY = (int)Math.Floor(Canvas.GetTop(currShape));
            int centerX = rectangleStartX + (int)Math.Floor(rectangleWidth / 2.0);
            int centerY = rectangleStartY + (int)Math.Floor(rectangleHeight / 2.0);

            return Get_All_Pixels(image, centerX, centerY, list);
        }

        private List<int> Get_All_Pixels(Bitmap image, int x, int y, List<int> l)
        {
            System.Drawing.Color color = image.GetPixel(x, y);
            if (color.R != color.B || color.B != color.G || color.R != color.G)
            {
                return l;
            }
            else
            {
                l.Add(color.R);
                image.SetPixel(x, y, System.Drawing.Color.Red);
                l = Get_All_Pixels(image, x + 1, y, l);
                l = Get_All_Pixels(image, x, y + 1, l);
                l = Get_All_Pixels(image, x - 1, y, l);
                l = Get_All_Pixels(image, x, y - 1, l);
            }
            return l;
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

            if (currShape != null)
            {
                // Get rectangles parameters and modify them for the DrawRectangle function, which displays rectangles differently than canvas
                int rectangleStrokeThickness = (int)Math.Floor(currShape.StrokeThickness);
                int rectagnleWidth = (int)Math.Floor(currShape.Width) - rectangleStrokeThickness;
                int rectangleHeight = (int)Math.Floor(currShape.Height) - rectangleStrokeThickness;
                int rectangleStartX = (int)Math.Floor(Canvas.GetLeft(currShape)) + 1;
                int rectangleStartY = (int)Math.Floor(Canvas.GetTop(currShape)) + 1;

                outputImage = AddBorder(outputImage);

                using (Graphics graphics = Graphics.FromImage(outputImage))
                {
                    if (currShape is System.Windows.Shapes.Rectangle)
                    {
                        graphics.DrawRectangle(new System.Drawing.Pen(GetColorFromBorderBrush(currShape.Stroke), rectangleStrokeThickness), rectangleStartX, rectangleStartY, rectagnleWidth, rectangleHeight);
                    }
                    else
                    {
                        graphics.DrawEllipse(new System.Drawing.Pen(GetColorFromBorderBrush(currShape.Stroke), rectangleStrokeThickness), rectangleStartX, rectangleStartY, rectagnleWidth, rectangleHeight);
                    }
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
                    if (currShape != null)
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

        private Bitmap RemoveBorder(Bitmap outputImage)
        {
            int leftThickness = (int)borderMain.BorderThickness.Left;
            int rightThickness = (int)borderMain.BorderThickness.Right;
            int topThickness = (int)borderMain.BorderThickness.Top;
            int bottomThickness = (int)borderMain.BorderThickness.Bottom;


            Bitmap outputImageWithoutBorder = new Bitmap(outputImage.Width - leftThickness - rightThickness, outputImage.Height - topThickness - bottomThickness);

            for (int i = 0; i < outputImage.Height; ++i)
            {
                for (int j = 0; j < outputImage.Width; ++j)
                {
                    if (i >= topThickness && i < outputImage.Height - bottomThickness && j >= leftThickness && j < outputImage.Width - rightThickness)
                    {
                        outputImageWithoutBorder.SetPixel(j - topThickness, i - leftThickness, outputImage.GetPixel(j, i));
                    }
                }
            }
            return outputImageWithoutBorder;
        }

        private System.Drawing.Color GetColorFromBorderBrush(System.Windows.Media.Brush borderBrush)
        {
            // Convert the BorderBrush color to a SolidColorBrush (assuming it's a SolidColorBrush)
            SolidColorBrush solidColorBrush = (SolidColorBrush)borderBrush;

            // Convert the SolidColorBrush color to a System.Windows.Media.Color
            System.Windows.Media.Color mediaColor = solidColorBrush.Color;

            // Convert the System.Windows.Media.Color to a System.Drawing.Color
            System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);

            // Now you can use drawingColor with outputImageWithoutBorder.SetPixel(j, i, drawingColor)
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (currShape is Ellipse)
            {
                int rectangleStrokeThickness = (int)Math.Floor(currShape.StrokeThickness);
                int rectangleWidth = (int)Math.Floor(currShape.Width) - rectangleStrokeThickness;
                int rectangleHeight = (int)Math.Floor(currShape.Height) - rectangleStrokeThickness;
                int rectangleStartX = (int)Math.Floor(Canvas.GetLeft(currShape)) + 1;
                int rectangleStartY = (int)Math.Floor(Canvas.GetTop(currShape)) + 1;
                int centerX = rectangleStartX + (int)Math.Floor(rectangleWidth / 2.0);
                int centerY = rectangleStartY + (int)Math.Floor(rectangleWidth / 2.0);


                Bitmap outputImage = AddBorder(angiogramBW!);

                using (Graphics graphics = Graphics.FromImage(outputImage))
                {
                    if (currShape is System.Windows.Shapes.Rectangle)
                    {
                        graphics.DrawRectangle(new System.Drawing.Pen(GetColorFromBorderBrush(currShape.Stroke), rectangleStrokeThickness), rectangleStartX, rectangleStartY, rectangleWidth, rectangleHeight);
                    }
                    else
                    {
                        graphics.DrawEllipse(new System.Drawing.Pen(GetColorFromBorderBrush(currShape.Stroke), rectangleStrokeThickness), rectangleStartX, rectangleStartY, rectangleWidth, rectangleHeight);
                        Remove_selection();
                        angiogramDisplay.Source = SharedFunctions.ImageSourceFromBitmap(RemoveBorder(outputImage));
                    }
                }
            }
        }

        private Bitmap Draw_Shape_Into_Angiogram(Bitmap image)
        {
            Bitmap outputImage = AddBorder(image);

            if (currShape != null)
            {
                int shapeStrokeThickness = (int)Math.Floor(currShape.StrokeThickness);
                int shapeWidth = (int)Math.Floor(currShape.Width) - shapeStrokeThickness;
                int shapeHeight = (int)Math.Floor(currShape.Height) - shapeStrokeThickness;
                int shapeStartX = (int)Math.Floor(Canvas.GetLeft(currShape)) + 1;
                int shapeStartY = (int)Math.Floor(Canvas.GetTop(currShape)) + 1;

                using (Graphics graphics = Graphics.FromImage(outputImage))
                {
                    if (currShape is System.Windows.Shapes.Rectangle)
                    {
                        graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.Red, shapeStrokeThickness), shapeStartX, shapeStartY, shapeWidth, shapeHeight);
                    }
                    else
                    {
                        graphics.DrawEllipse(new System.Drawing.Pen(System.Drawing.Color.Red, shapeStrokeThickness), shapeStartX, shapeStartY, shapeWidth, shapeHeight);
                    }
                }
                outputImage = RemoveBorder(outputImage);
            }
            return outputImage;
        }

        private void Save_Selection_Click(object sender, RoutedEventArgs e)
        {
            if (currShape != null)
            {
                int shapeLeft = (int)Math.Floor(Canvas.GetLeft(currShape));
                int shapeTop = (int)Math.Floor(Canvas.GetTop(currShape));

                int width = (int)Math.Floor(currShape.Width);
                int height = (int)Math.Floor(currShape.Height);

                string shape = "";

                if (currShape is Ellipse)
                {
                    shape = "Ellipse";
                }
                else
                {
                    shape = "Rectangle";
                }

                ShapeProperties shapeProperties = new ShapeProperties(width, height, shapeTop, shapeLeft, shape);

                // Convert the ShapeProperties instance to JSON
                string json = JsonSerializer.Serialize(shapeProperties);

                // Save the JSON string to a file
                try
                {
                    File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath() + "BloodVesselCalculationAppShapeProperties.json"), json);
                    MessageBox.Show("Selekce úspěšně uložena.");

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Load_Selection_Click(object sender, RoutedEventArgs e)
        {
            // Specify the path to the JSON file
            string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath() + "BloodVesselCalculationAppShapeProperties.json");

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read the JSON data from the file
                string jsonText = File.ReadAllText(filePath);

                // Deserialize the JSON data into your data structure
                try
                {
                    ShapeProperties? data = JsonSerializer.Deserialize<ShapeProperties>(jsonText);
                    if (data != null)
                    {
                        Remove_selection();
                        switch (data.Kind)
                        {
                            case "Ellipse":
                                currShape = new Ellipse()
                                {
                                    Width = data.Width,
                                    Height = data.Height,
                                    Stroke = System.Windows.Media.Brushes.Red,
                                    StrokeThickness = 3
                                };
                                break;
                            case "Rectangle":
                                currShape = new System.Windows.Shapes.Rectangle()
                                {
                                    Width = data.Width,
                                    Height = data.Height,
                                    Stroke = System.Windows.Media.Brushes.Red,
                                    StrokeThickness = 3
                                };
                                break;
                        }

                        // Set the position of the red rectangle
                        Canvas.SetLeft(currShape, data.ShapeLeft);
                        Canvas.SetTop(currShape, data.ShapeTop);

                        mainCanvas.Children.Add(currShape);

                        AddHandles();

                        // need a new density calculation for correct export
                        isLastDensityCalculationCorrect = false;
                    }
                    else
                    {
                        throw new Exception("JSON cannot be deserialized");

                    }

                }
                catch (Exception ex)
                { 
                    MessageBox.Show("Nepodařilo se načíst uloženou selekci: " + ex);
                }
            }
            else
            {
                MessageBox.Show("Soubor s uloženou selekcí nenalezen: " + filePath);
            }
        }
    }
}
