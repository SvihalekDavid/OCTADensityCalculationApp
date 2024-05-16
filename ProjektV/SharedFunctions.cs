using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;

namespace OCTADensityCalculationApp
{
    public static class SharedFunctions
    {
        public static void SaveBitmapImage(Bitmap outputImage, SaveFileDialog dlg)
        {
            if (System.IO.Path.GetExtension(dlg.FileName).ToLower() == ".png")
            {
                outputImage.Save(dlg.FileName, ImageFormat.Png);
            }
            else
            {
                outputImage.Save(dlg.FileName, ImageFormat.Tiff);
            }
        }

        private static int Return_Number_Of_Pixels_Of_Value(Bitmap img, int value)
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

        // Adding white pixels above Bitmap image for text input
        public static Bitmap AddWhiteLayers(Bitmap outputImage)
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
        public static double Density_Calculation(List<System.Drawing.Color> colors)
        {
            // Counting white pixels in the binary image
            const int MAX_VAL = 255;
            int whitePixelCount = 0;

            foreach (System.Drawing.Color color in colors)
            {
                if (color.R == MAX_VAL)
                {
                    ++whitePixelCount;
                }
            }

            int pixelCount = colors.Count;

            return (double)whitePixelCount / pixelCount * 100;
        }

        public static double Density_Calculation(Bitmap imgBW)
        {
            // Counting white pixels in the binary image
            const int MAX_VAL = 255;
            int whitePixelCount = Return_Number_Of_Pixels_Of_Value(imgBW, MAX_VAL);

            int pixelCount = imgBW.Width * imgBW.Height;

            return (double)whitePixelCount / pixelCount * 100;
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        // Creation of coressponding ImageSource class for Bitmap image
        public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        public static int Otsu_Thresholding(Bitmap image)
        {
            // Compute histogram
            int[] histogram = new int[256];
            for (int i = 0; i < image.Height; ++i)
            {
                for (int j = 0; j < image.Width; ++j)
                {
                    System.Drawing.Color pixel = image.GetPixel(j, i);
                    histogram[pixel.R]++;
                }
            }
            // Total number of pixels
            int totalPixels = image.Width * image.Height;

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

                sumB += t * histogram[t];

                double mB = sumB / wB; // Mean background
                double mF = (sum - sumB) / wF; // Mean foreground

                // Calculate between-class variance
                double betweenVariance = wB * (double)wF * (mB - mF) * (mB - mF);
                if (betweenVariance > maxVariance)
                {
                    maxVariance = betweenVariance;
                    threshold = t;
                }
            }

            return threshold;
        }
        // Create binary image using the threshold
        public static Bitmap Binarize_Image_By_Threshold(Bitmap image, int threshold)
        {
            Bitmap binaryImage = new Bitmap(image.Width, image.Height);

            for (int i = 0; i < image.Height; i++)
            {
                for (int j = 0; j < image.Width; j++)
                {
                    if (image.GetPixel(j, i).R <= threshold)
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

        // Saving dialog handle
        public static void SaveFileDialog_FileOk(object sender, CancelEventArgs e)
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
    }
}
