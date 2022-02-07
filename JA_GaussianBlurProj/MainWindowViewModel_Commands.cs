using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using JA_GaussianBlurProj.Annotations;
using Lab_CS;
using SimplexNoise;
using static System.Windows.Forms.DialogResult;
using static JA_GaussianBlurProj.ImageCalculationClasses;
using MessageBox = System.Windows.Forms.MessageBox;

namespace JA_GaussianBlurProj
{
    public unsafe partial class MainWindowViewModel
    {
        [DllImport(@"C:\Users\StdUser\source\repos\JA_GaussianBlurProj\x64\Debug\LibCPP.dll")]
        public static extern unsafe float CalculatePixelCpp(float* pixels, float* weights, float sumOfWeights,
            int diameter);
        
        private void SelectInputDirectoryCommandExecute(object parameter)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                InputDirectory = folderBrowserDialog.SelectedPath;
            }

            Debug.WriteLine("Selecting input directory");
        }

        private void SelectOutputDirectoryCommandExecute(object parameter)
        {
            Debug.WriteLine("Selecting output directory");
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                OutputDirectory = folderBrowserDialog.SelectedPath;
            }
        }

        private unsafe void CalculateButtonCommandExecute(object parameter)
        {
            if (Sigma == 0.0f) return;
            DirectoryInfo outputDir;
            if (!Directory.Exists("D:\\TestData\\Output"))
                outputDir = Directory.CreateDirectory("D:\\TestData\\Output");
            else outputDir = new DirectoryInfo("D:\\TestData\\Output");
            Bitmap bmp = new Bitmap(Image.FromFile("D:\\TestData\\small\\194702399.jpg"));
            float[,] imageR = new float[bmp.Height, bmp.Width];
            float[,] imageG = new float[bmp.Height, bmp.Width];
            float[,] imageB = new float[bmp.Height, bmp.Width];
                            
            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    imageR[i, j] = bmp.GetPixel(j, i).R;
                    imageG[i, j] = bmp.GetPixel(j, i).G;
                    imageB[i, j] = bmp.GetPixel(j, i).B;
                }
            }

            float[,] imageRExtended = ExtendImage(imageR, Radius);
            float[,] imageGExtended = ExtendImage(imageG, Radius);
            float[,] imageBExtended = ExtendImage(imageB, Radius);

            (float[] gaussianMatrix, float sumOfWeights) = CalculateGaussianMatrix(Radius, Sigma);
            fixed (float* gaussianMatrixPtr = gaussianMatrix)
            {
                for (int i = 0; i < bmp.Height; i++)
                {
                    for (int j = 0; j < bmp.Width; j++)
                    {
                        float[] imageRKernel = ExtractKernel(imageRExtended, Radius, i, j);
                        float[] imageGKernel = ExtractKernel(imageGExtended, Radius, i, j);
                        float[] imageBKernel = ExtractKernel(imageBExtended, Radius, i, j);

                        fixed (float* imageRKernelPtr = imageRKernel, imageGKernelPtr = imageGKernel, imageBKernelPtr = imageBKernel)
                        {
                            imageR[i, j] = Calculate.CalculatePixel(
                                imageRKernelPtr,
                                gaussianMatrixPtr, sumOfWeights, 2 * Radius + 1);

                            imageG[i, j] = Calculate.CalculatePixel(
                                imageGKernelPtr,
                                gaussianMatrixPtr, sumOfWeights, 2 * Radius + 1);

                            imageB[i, j] = Calculate.CalculatePixel(
                                imageBKernelPtr,
                                gaussianMatrixPtr, sumOfWeights, 2 * Radius + 1);

                            //imageR[i, j] = CalculatePixelCpp(
                            //   imageRKernelPtr,
                            //   gaussianMatrixPtr, sumOfWeights, 2 * Radius + 1);

                            //imageG[i, j] = CalculatePixelCpp(
                            //    imageGKernelPtr,
                            //    gaussianMatrixPtr, sumOfWeights, 2 * Radius + 1);

                            //imageB[i, j] = CalculatePixelCpp(
                            //    imageBKernelPtr,
                            //    gaussianMatrixPtr, sumOfWeights, 2 * Radius + 1);
                        }
                    }
                }
            }

            for (int i = 0, i1 = Radius; i < bmp.Height; i++, i1++)
            {
                for (int j = 0, j1 = Radius; j < bmp.Width; j++, j1++)
                {
                    bmp.SetPixel(j,i,Color.FromArgb((int)imageR[i,j],(int) imageG[i,j],(int) imageB[i,j]));
                }
            }

            bmp.Save("D:\\TestData\\small\\image.png", ImageFormat.Png);
            Debug.WriteLine("Calculating...");
        }

        private async Task BenchmarkButtonAsyncCommandExecute()
        {
            //Debug.WriteLine("Benchmarking...");
            //DialogResult messageBoxResult = MessageBox.Show("Would you like to generate new test data?",
            //    "Confirm generating new test data", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
            //    MessageBoxDefaultButton.Button1);
            //float[,] gaussianMatrix;
            //if (Sigma != 0.0f)
            //{
            //    gaussianMatrix = ImageCalculationClasses.CalculateGaussianMatrix(Radius, Sigma);
            //}
            //else
            //{
            //    MessageBox.Show("Sigma patameter must be different from 0");
            //}
            //if (messageBoxResult == Yes)
            //{
            //    await GenerateTestDataAsync();
            //}
            //else if (messageBoxResult == Cancel) return;

            //DirectoryInfo smallDataDir = new DirectoryInfo("D:\\TestData\\small");
            ////DirectoryInfo mediumDataDir = new DirectoryInfo("D:\\TestData\\medium");
            ////DirectoryInfo largeDataDir = new DirectoryInfo("D:\\TestData\\large");

            //ConcurrentBag<FileInfo> imagesSmall = new ConcurrentBag<FileInfo>(smallDataDir.EnumerateFiles());
            ////ConcurrentBag<FileInfo> imagesMedium = new ConcurrentBag<FileInfo>(mediumDataDir.EnumerateFiles());
            ////ConcurrentBag<FileInfo> imagesLarge = new ConcurrentBag<FileInfo>(largeDataDir.EnumerateFiles());
        }

        private Task GenerateTestDataAsync() => Task.Factory.StartNew((() =>
        {
            const int samples = 50;
            DirectoryInfo smallDataDir;
            DirectoryInfo mediumDataDir;
            DirectoryInfo largeDataDir;

            #region Clear Prevoius Test Data

            if (!Directory.Exists("D:\\TestData"))
            {
                Directory.CreateDirectory("D:\\TestData");
            }

            smallDataDir = !Directory.Exists("D:\\TestData\\small")
                ? Directory.CreateDirectory("D:\\TestData\\small")
                : new DirectoryInfo("D:\\TestData\\small");

            mediumDataDir = !Directory.Exists("D:\\TestData\\medium")
                ? Directory.CreateDirectory("D:\\TestData\\medium")
                : new DirectoryInfo("D:\\TestData\\medium");

            largeDataDir = !Directory.Exists("D:\\TestData\\large")
                ? Directory.CreateDirectory("D:\\TestData\\large")
                : new DirectoryInfo("D:\\TestData\\large");

            foreach (DirectoryInfo directory in smallDataDir.EnumerateDirectories())
            {
                directory.Delete(true);
            }

            foreach (FileInfo file in smallDataDir.EnumerateFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo directory in mediumDataDir.EnumerateDirectories())
            {
                directory.Delete(true);
            }

            foreach (FileInfo fileInfo in mediumDataDir.EnumerateFiles())
            {
                fileInfo.Delete();
            }

            foreach (DirectoryInfo directory in largeDataDir.EnumerateDirectories())
            {
                directory.Delete(true);
            }

            foreach (FileInfo file in largeDataDir.EnumerateFiles())
            {
                file.Delete();
            }

            #endregion

            #region Create New test Data

            List<int> seedsList = new List<int>(900);
            Random rnd = new Random();
            int seed;
            do
            {
                seed = rnd.Next();
                if (!seedsList.Exists(el => el == seed))
                {
                    seedsList.Add(seed);
                }
            } while (seedsList.Count < 900);

            const int smallImageSize = 500;
            const int mediumImageSize = 2500;
            const int largeImageSize = 5000;

            for (int i = 0, j = 300, k = 600; i < 100; i++, j++, k++)
            {
                Noise.Seed = seedsList[i];
                float[,] noiseR = SimplexNoise.Noise.Calc2D(smallImageSize, smallImageSize, 0.4f);
                Noise.Seed = seedsList[j];
                float[,] noiseG = SimplexNoise.Noise.Calc2D(smallImageSize, smallImageSize, 0.4f);
                Noise.Seed = seedsList[k];
                float[,] noiseB = SimplexNoise.Noise.Calc2D(smallImageSize, smallImageSize, 0.4f);
                Bitmap bitmap = new Bitmap(smallImageSize, smallImageSize);

                for (int l = 0; l < smallImageSize; l++)
                {
                    for (int m = 0; m < smallImageSize; m++)
                    {
                        bitmap.SetPixel(l, m, Color.FromArgb(
                            (int)noiseR[l, m],
                            (int)noiseG[l, m],
                            (int)noiseB[l, m]
                        ));
                    }
                }

                bitmap.Save($"D:\\TestData\\small\\img_small{i}.png", ImageFormat.Png);

                bitmap = new Bitmap(mediumImageSize, mediumImageSize);
                Noise.Seed = seedsList[i + 100];
                noiseR = Noise.Calc2D(mediumImageSize, mediumImageSize, 0.4f);

                Noise.Seed = seedsList[j + 100];
                noiseG = Noise.Calc2D(mediumImageSize, mediumImageSize, 0.4f);

                Noise.Seed = seedsList[k + 100];
                noiseB = Noise.Calc2D(mediumImageSize, mediumImageSize, 0.4f);

                for (int l = 0; l < mediumImageSize; l++)
                {
                    for (int m = 0; m < mediumImageSize; m++)
                    {
                        bitmap.SetPixel(l, m, Color.FromArgb(
                            (int)noiseR[l, m],
                            (int)noiseG[l, m],
                            (int)noiseB[l, m]
                        ));
                    }
                }

                bitmap.Save($"D:\\TestData\\medium\\img_medium{i}.png", ImageFormat.Png);

                bitmap = new Bitmap(largeImageSize, largeImageSize);
                Noise.Seed = seedsList[i + 200];
                noiseR = Noise.Calc2D(largeImageSize, largeImageSize, 0.4f);

                Noise.Seed = seedsList[j + 200];
                noiseG = Noise.Calc2D(largeImageSize, largeImageSize, 0.4f);

                Noise.Seed = seedsList[k + 200];
                noiseB = Noise.Calc2D(largeImageSize, largeImageSize, 0.4f);

                for (int l = 0; l < largeImageSize; l++)
                {
                    for (int m = 0; m < largeImageSize; m++)
                    {
                        bitmap.SetPixel(l, m, Color.FromArgb(
                            (int)noiseR[l, m],
                            (int)noiseG[l, m],
                            (int)noiseB[l, m]
                        ));
                    }
                }

                bitmap.Save($"D:\\TestData\\large\\img_large{i}.png", ImageFormat.Png);
            }

            #endregion
        }));
        
    }
}
