using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lab_CS;
using SimplexNoise;
using static System.Windows.Forms.DialogResult;
using static JA_GaussianBlurProj.ImageCalculationClasses;
using MessageBox = System.Windows.Forms.MessageBox;
using Excel = Microsoft.Office.Interop.Excel;

namespace JA_GaussianBlurProj
{
    public partial class MainWindowViewModel
    {
        private volatile bool _benchmark;

        #if DEBUG
        [DllImport(@"C:\Users\StdUser\source\repos\JA_GaussianBlurProj\x64\Debug\LibCPP.dll")]
        #else
        [DllImport(@"C:\Users\StdUser\source\repos\JA_GaussianBlurProj\x64\Release\LibCPP.dll")]
        #endif
        public static extern float CalculatePixelCpp(float[] pixels, float[] weights, float sumOfWeights,
            int diameter);

        #if DEBUG
            [DllImport(@"C:\Users\StdUser\source\repos\JA_GaussianBlurProj\x64\Debug\LibASM.dll")]
        #else
            [DllImport(@"C:\Users\StdUser\source\repos\JA_GaussianBlurProj\x64\Release\LibASM.dll")]
        #endif
        public static extern float CalculatePixelAsm(float[] pixels, float[] weights, float sumOfWeights,
            int diameter);

        private int _numberOfThreadsNotCompleted = 100;
        private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

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

        private async Task CalculateButtonCommandExecute()
        {
            _benchmark = false;
            NotBusy = false;
            await Task.Run((() =>
            {
                bool dirError = false;
                string errors = "";
                if (Sigma == 0.0f)
                {
                    MessageBox.Show("Sigma parameter must be greater than 0", "Error when checking input parameters",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    NotBusy = true;
                    return;
                }

                if (string.IsNullOrEmpty(InputDirectory))
                {
                    errors += "Input directory cannot be empty\n";
                    dirError = true;
                }

                if (string.IsNullOrEmpty(OutputDirectory))
                {
                    errors += "Output directory cannot be empty\n";
                    dirError = true;
                }

                if (dirError)
                {
                    MessageBox.Show(errors, "Error when checking input parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    NotBusy = true;
                    return;
                }

                IEnumerable<FileInfo> photos =
                    new DirectoryInfo(InputDirectory).EnumerateFiles().Where(f => f.Extension == ".png" || f.Extension == ".jpg");

                if (!photos.Any())
                {
                    MessageBox.Show("No Image files have been found", "Error when searching for files", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    NotBusy = true;
                    return;
                }

                ImagesCount = photos.Count();
                Progress = 0.0;

                if (ThreadsCount <= Environment.ProcessorCount)
                {
                    int cpu = 1;
                    for (int i = 1; i < ThreadsCount; i++)
                    {
                        cpu <<= 1;
                        cpu |= 1;
                    }

                    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(cpu);
                    Process.GetCurrentProcess().Refresh();
                }

                else
                {
                    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(65535);
                    Process.GetCurrentProcess().Refresh();
                }

                ThreadPool.SetMaxThreads(ThreadsCount, ThreadsCount);
                _numberOfThreadsNotCompleted = photos.Count();
                WaitCallback waitCallback;
                switch (SelectedLib)
                {
                    case 0:
                        waitCallback = CalculatePhotoCS;
                        break;
                    case 1:
                        waitCallback = CalculatePhotoCPP;
                        break;
                    case 2:
                        waitCallback = CalculatePhotoASM;
                        break;
                    default:
                        NotBusy = true;
                        throw new ArgumentOutOfRangeException("Invalid lib selected");
                }
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (FileInfo file in photos)
                {
                    ThreadPool.QueueUserWorkItem(waitCallback, file);
                }
                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                _manualResetEvent.Reset();
                Duration = $"{stopwatch.ElapsedMilliseconds} ms";
                NotBusy = true;
            }));
        }

        private async Task BenchmarkButtonAsyncCommandExecute()
        {
            NotBusy = false;
            Duration = "Preparing for benchmark";
            DialogResult askForBenchmark =
                MessageBox.Show(
                    "Are you sure you want to run benchmark?\nIt may take several hours to generate output:",
                    "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (askForBenchmark == No) return;

            DialogResult askForGeneratingNewTestdataDialogResult = MessageBox.Show(
                "Would you like to generate new test data", "Benchmark...", MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);

            switch (askForGeneratingNewTestdataDialogResult)
            {
                case Cancel:
                    NotBusy = true;
                    return;
                case Yes:
                    await GenerateTestDataAsync();
                    NotBusy = true;
                    break;
                case No:
                    break;
                default:
                    NotBusy = true;
                    throw new ArgumentOutOfRangeException();
            }
            
            Sigma = 10.0f;
            Radius = 5;

            int[] threads = {3, 15, 255, 65535, 65535, 65535};
            int[] threadsCounts = {2, 4, 8, 16, 32, 64};
            Excel.Application xlApp = new Excel.Application();

            if (xlApp == null)
            {
                MessageBox.Show("Cannot open excel app");
                NotBusy = true;
                return;
            }

            _benchmark = true;
            Excel.Workbook workbook;
            workbook = xlApp.Workbooks.Add(Missing.Value);

            Excel.Worksheet worksheet_small = (Excel.Worksheet)workbook.Worksheets[1];
            worksheet_small.Name = "Results Small data";
            worksheet_small.Cells[1, 1] = "Threads count:";
            worksheet_small.Cells[1, 2] = "Time (ms):";

            Excel.Worksheet worksheet_medium = (Excel.Worksheet)workbook.Worksheets.Add();
            worksheet_medium.Name = "Results Medium Data";
            worksheet_medium.Cells[1, 1] = "Threads count:";
            worksheet_medium.Cells[1, 2] = "Time (ms):";

            Excel.Worksheet worksheet_large = (Excel.Worksheet)workbook.Worksheets.Add();
            worksheet_medium.Name = "Results Large Data";
            worksheet_medium.Cells[1, 1] = "Threads count:";
            worksheet_medium.Cells[1, 2] = "Time (ms):";

            try
            {
                for (int i = 0; i < 6; i++)
                {
                    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(threads[i]);
                    Process.GetCurrentProcess().Refresh();
                    ThreadsCount = threadsCounts[i];
                    ThreadPool.SetMaxThreads(threadsCounts[i], threadsCounts[i]);
                    Duration = $"Benchmarking CS small on {ThreadsCount} threads";
                    long time_small_data = await BenchmarkCSAsyncSmall();
                    worksheet_small.Cells[i + 2, 1] = threadsCounts[i];
                    worksheet_small.Cells[i + 2, 2] = time_small_data;
                    GC.Collect();
                    Thread.Sleep(10000);
                    Duration = $"Benchmarking CPP small on {ThreadsCount} threads";
                    time_small_data = await BenchmarkCPPAsyncSmall();
                    worksheet_small.Cells[i + 2, 3] = time_small_data;
                    GC.Collect();
                    Thread.Sleep(10000);
                    Duration = $"Benchmarking ASM small on {ThreadsCount} threads";
                    time_small_data = await BenchmarkASMAsyncSmall();
                    worksheet_small.Cells[i + 2, 4] = time_small_data;
                }

                GC.Collect();
                Thread.Sleep(100000);

                for (int i = 0; i < 6; i++)
                {
                    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(threads[i]);
                    Process.GetCurrentProcess().Refresh();
                    ThreadPool.SetMaxThreads(threadsCounts[i], threadsCounts[i]);
                    ThreadsCount = threadsCounts[i];
                    Duration = $"Benchmarking CS medium on {ThreadsCount} threads";
                    long time_medium_data = await BenchmarkCSAsyncMedium();
                    worksheet_medium.Cells[i + 2, 1] = threadsCounts[i];
                    worksheet_medium.Cells[i + 2, 2] = time_medium_data;
                    GC.Collect();
                    Thread.Sleep(10000);
                    Duration = $"Benchmarking CPP medium on {ThreadsCount} threads";
                    time_medium_data = await BenchmarkCPPAsyncMedium();
                    worksheet_medium.Cells[i + 2, 3] = time_medium_data;
                    GC.Collect();
                    Thread.Sleep(10000);
                    Duration = $"Benchmarking ASM medium on {ThreadsCount} threads";
                    time_medium_data = await BenchmarkASMAsyncMedium();
                    worksheet_medium.Cells[i + 2, 4] = time_medium_data;
                    Thread.Sleep(10000);
                }
                Thread.Sleep(100000);
                for (int i = 0; i < 6; i++)
                {
                    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(threads[i]);
                    Process.GetCurrentProcess().Refresh();
                    ThreadPool.SetMaxThreads(threadsCounts[i], threadsCounts[i]);
                    ThreadsCount = threadsCounts[i];
                    Duration = $"Benchmarking CS large on {ThreadsCount} threads";
                    long time_large_data = await BenchmarkCSAsyncLarge();
                    worksheet_large.Cells[i + 2, 1] = threadsCounts[i];
                    worksheet_large.Cells[i + 2, 2] = time_large_data;
                    GC.Collect();
                    Thread.Sleep(10000);
                    Duration = $"Benchmarking CPP large on {ThreadsCount} threads";
                    time_large_data = await BenchmarkCPPAsyncLarge();
                    worksheet_large.Cells[i + 2, 3] = time_large_data;
                    GC.Collect();
                    Thread.Sleep(10000);
                    Duration = $"Benchmarking ASM large on {ThreadsCount} threads";
                    time_large_data = await BenchmarkASMAsyncLarge();
                    worksheet_large.Cells[i + 2, 4] = time_large_data;
                }
                GC.Collect();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error when benchmarking app", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Duration = "Saving data";
            workbook.SaveAs(@"D:\TestData\benchmark_results.xlsx", Excel.XlFileFormat.xlWorkbookDefault);
            workbook.Close(true, Missing.Value, Missing.Value);
            xlApp.Quit();

            Marshal.ReleaseComObject(worksheet_small);
            Marshal.ReleaseComObject(worksheet_medium);
            Marshal.ReleaseComObject(worksheet_large);
            Marshal.ReleaseComObject(workbook);
            Marshal.ReleaseComObject(xlApp);
            Duration = "Benchmark done successfully";
            NotBusy = true;
        }

        private Task GenerateTestDataAsync() => Task.Factory.StartNew(() =>
        {
            const int samples = 128;
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

            List<int> seedsList = new List<int>(9 * samples);
            Random rnd = new Random();
            int seed;
            do
            {
                seed = rnd.Next();
                if (!seedsList.Exists(el => el == seed))
                {
                    seedsList.Add(seed);
                }
            } while (seedsList.Count < 9 * samples);

            const int smallImageSize = 500;
            const int mediumImageSize = 1000;
            const int largeImageSize = 2500;

            for (int i = 0, j = 3 * samples, k = 6*samples; i < samples; i++, j++, k++)
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
        });

        #region BENCHMARK SMALL DATA
        private Task<long> BenchmarkCSAsyncSmall()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\small");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoCS, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }

        private Task<long> BenchmarkCPPAsyncSmall()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\small");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoCPP, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }
        private Task<long> BenchmarkASMAsyncSmall()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\small");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoASM, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }
        #endregion

        #region BENCHMARK MEDIUM DATA
        private Task<long> BenchmarkCSAsyncMedium()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\medium");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoCPP, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }

        private Task<long> BenchmarkCPPAsyncMedium()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\medium");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoCPP, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }

        private Task<long> BenchmarkASMAsyncMedium()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\medium");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoCPP, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }
        #endregion

        #region BENCHMARK LARGE DATA
        private Task<long> BenchmarkCSAsyncLarge()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\large");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoCPP, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }

        private Task<long> BenchmarkCPPAsyncLarge()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\large");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoCPP, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }

        private Task<long> BenchmarkASMAsyncLarge()
        {
            return Task.Run(() =>
            {
                DirectoryInfo smallDataDirectory = new DirectoryInfo(@"D:\Testdata\large");
                Stopwatch stopwatch = new Stopwatch();
                _manualResetEvent.Reset();
                _numberOfThreadsNotCompleted = 100;
                stopwatch.Start();
                foreach (FileInfo file in smallDataDirectory.EnumerateFiles())
                {
                    ThreadPool.QueueUserWorkItem(CalculatePhotoCPP, file);
                }

                _manualResetEvent.WaitOne();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            });
        }
        #endregion

        #region CALCULATE PHOTO

        private void CalculatePhotoCS(object param)
        {
            FileInfo file = (FileInfo) param;
            Bitmap bmp = new Bitmap(Image.FromFile(file.FullName));

            float[,] pixelsR = new float[bmp.Height, bmp.Width];
            float[,] pixelsG = new float[bmp.Height, bmp.Width];
            float[,] pixelsB = new float[bmp.Height, bmp.Width];

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    Color color = bmp.GetPixel(j, i);
                    pixelsR[i, j] = color.R;
                    pixelsG[i, j] = color.G;
                    pixelsB[i, j] = color.B;
                }
            }

            float[,] extendedPixelsR = ExtendImage(pixelsR, Radius);
            float[,] extendedPixelsG = ExtendImage(pixelsG, Radius);
            float[,] extendedPixelsB = ExtendImage(pixelsB, Radius);

            (float[] gaussianMatrix, float sumOfWeights) = CalculateGaussianMatrix(Radius, Sigma);

            int diameter = 2 * Radius + 1;

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    float[] kernelR = ExtractKernel(extendedPixelsR, Radius, i, j);
                    float[] kernelG = ExtractKernel(extendedPixelsG, Radius, i, j);
                    float[] kernelB = ExtractKernel(extendedPixelsB, Radius, i, j);
                    pixelsR[i, j] = Calculate.CalculatePixel(kernelR, gaussianMatrix, sumOfWeights, diameter);
                    pixelsG[i, j] = Calculate.CalculatePixel(kernelG, gaussianMatrix, sumOfWeights, diameter);
                    pixelsB[i, j] = Calculate.CalculatePixel(kernelB, gaussianMatrix, sumOfWeights, diameter);
                }
            }

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    bmp.SetPixel(j,i,Color.FromArgb((int) pixelsR[i,j],(int) pixelsG[i,j],(int) pixelsB[i,j]));
                }
            }
            
            if (Interlocked.Decrement(ref _numberOfThreadsNotCompleted) == 0) _manualResetEvent.Set();
            if (!_benchmark)
            {
                Task.Factory.StartNew((() =>
                {
                    lock (bmp)
                    {
                        bmp.Save($@"{OutputDirectory}\{file.Name}");
                    }
                }));
            }
            ++Progress;
        }

        private void CalculatePhotoCPP(object param)
        {
            FileInfo file = (FileInfo)param;
            Bitmap bmp = new Bitmap(Image.FromFile(file.FullName));

            float[,] pixelsR = new float[bmp.Height, bmp.Width];
            float[,] pixelsG = new float[bmp.Height, bmp.Width];
            float[,] pixelsB = new float[bmp.Height, bmp.Width];

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    Color color = bmp.GetPixel(j, i);
                    pixelsR[i, j] = color.R;
                    pixelsG[i, j] = color.G;
                    pixelsB[i, j] = color.B;
                }
            }

            float[,] extendedPixelsR = ExtendImage(pixelsR, Radius);
            float[,] extendedPixelsG = ExtendImage(pixelsG, Radius);
            float[,] extendedPixelsB = ExtendImage(pixelsB, Radius);

            (float[] gaussianMatrix, float sumOfWeights) = CalculateGaussianMatrix(Radius, Sigma);

            int diameter = 2 * Radius + 1;

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    float[] kernelR = ExtractKernel(extendedPixelsR, Radius, i, j);
                    float[] kernelG = ExtractKernel(extendedPixelsG, Radius, i, j);
                    float[] kernelB = ExtractKernel(extendedPixelsB, Radius, i, j);
                    pixelsR[i, j] = CalculatePixelCpp(kernelR, gaussianMatrix, sumOfWeights, diameter);
                    pixelsG[i, j] = CalculatePixelCpp(kernelG, gaussianMatrix, sumOfWeights, diameter);
                    pixelsB[i, j] = CalculatePixelCpp(kernelB, gaussianMatrix, sumOfWeights, diameter);
                }
            }

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    bmp.SetPixel(j, i, Color.FromArgb((int)pixelsR[i, j], (int)pixelsG[i, j], (int)pixelsB[i, j]));
                }
            }
            
            if (Interlocked.Decrement(ref _numberOfThreadsNotCompleted) == 0) _manualResetEvent.Set();
            if (!_benchmark)
            {
                Task.Factory.StartNew((() =>
                {
                    lock (bmp)
                    {
                        bmp.Save($@"{OutputDirectory}\{file.Name}");
                    }
                }));
            }
            ++Progress;
        }

        private void CalculatePhotoASM(object param)
        {
            FileInfo file = (FileInfo)param;
            Bitmap bmp = new Bitmap(Image.FromFile(file.FullName));

            float[,] pixelsR = new float[bmp.Height, bmp.Width];
            float[,] pixelsG = new float[bmp.Height, bmp.Width];
            float[,] pixelsB = new float[bmp.Height, bmp.Width];

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    Color color = bmp.GetPixel(j, i);
                    pixelsR[i, j] = color.R;
                    pixelsG[i, j] = color.G;
                    pixelsB[i, j] = color.B;
                }
            }

            float[,] extendedPixelsR = ExtendImage(pixelsR, Radius);
            float[,] extendedPixelsG = ExtendImage(pixelsG, Radius);
            float[,] extendedPixelsB = ExtendImage(pixelsB, Radius);

            (float[] gaussianMatrix, float sumOfWeights) = CalculateGaussianMatrix(Radius, Sigma);

            int diameter = 2 * Radius + 1;

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    float[] kernelR = ExtractKernel(extendedPixelsR, Radius, i, j);
                    float[] kernelG = ExtractKernel(extendedPixelsG, Radius, i, j);
                    float[] kernelB = ExtractKernel(extendedPixelsB, Radius, i, j);
                    pixelsR[i, j] = CalculatePixelAsm(kernelR, gaussianMatrix, sumOfWeights, diameter);
                    pixelsG[i, j] = CalculatePixelAsm(kernelG, gaussianMatrix, sumOfWeights, diameter);
                    pixelsB[i, j] = CalculatePixelAsm(kernelB, gaussianMatrix, sumOfWeights, diameter);
                }
            }

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    bmp.SetPixel(j, i, Color.FromArgb((int)pixelsR[i, j], (int)pixelsG[i, j], (int)pixelsB[i, j]));
                }
            }
            
            if (Interlocked.Decrement(ref _numberOfThreadsNotCompleted) == 0) _manualResetEvent.Set();
            if (!_benchmark)
            {
                Task.Factory.StartNew((() =>
                {
                    lock (bmp)
                    {
                        bmp.Save($@"{OutputDirectory}\{file.Name}");
                    }
                }));
            }
            ++Progress;
        }

#endregion
    }
}
