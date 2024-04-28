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

namespace ProjektV
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
        public static double Density_Calculation(Bitmap imgBW)
        {
            // Counting white pixels in the binary image
            const int MAX_VAL = 255;
            int whitePixelCount = Return_Number_Of_Pixels_Of_Value(imgBW, MAX_VAL);

            int pixelCount = imgBW.Width * imgBW.Height;

            return ((double)whitePixelCount / pixelCount) * 100;
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        public static Bitmap Otsu_Thresholding(Bitmap image)
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
