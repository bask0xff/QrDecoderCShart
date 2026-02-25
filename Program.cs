using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// TODO: dotnet add package System.Drawing.Common

namespace QrDecoderCShart
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //if (args.Length == 0)
            //{
            //    Console.WriteLine("Usage: QrDecoderPure.exe <path_to_qr.png_or_jpg>");
            //    return;
            //}

            string imagePath = "pngimg.com - qr_code_PNG34.png";
            if (!File.Exists(imagePath))
            {
                Console.WriteLine("File not found!");
                return;
            }

            try
            {
                using Bitmap bmp = new Bitmap(imagePath);
                bool[,] binaryGrid = BinarizeImage(bmp);   // чёрно-белая матрица

                // Здесь будет вся магия распознавания
                string decodedData = DecodeQr(binaryGrid, bmp.Width, bmp.Height);

                Console.WriteLine("Data recognition:");
                Console.WriteLine(decodedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Простая глобальная пороговая бинаризация (можно улучшить адаптивным порогом)
        /// </summary>
        static bool[,] BinarizeImage(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            bool[,] grid = new bool[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    // Яркость (стандартный коэффициент для человеческого зрения)
                    int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    grid[x, y] = gray < 128; // true = чёрный модуль
                }
            }
            return grid;
        }

        /// <summary>
        /// Заглушка для декодера. Здесь будем писать всю логику.
        /// </summary>
        static string DecodeQr(bool[,] grid, int width, int height)
        {
            var patterns = FinderPatternFinder.Find(grid, width, height);

            if (patterns.Count < 3)
            {
                return "Not found 3 finder pattern.";
            }

            Console.WriteLine("Found finder patterns:");
            foreach (var p in patterns)
            {
                Console.WriteLine(String.Format("{0}, {1}", p.X, p.Y));
            }

            // Дальше: определить, какой top-left, top-right, bottom-left
            // Вычислить module size, ориентацию, версию...
            // Пока просто заглушка
            return "Found: " + patterns.Count + " markers.";
        }
    }
}