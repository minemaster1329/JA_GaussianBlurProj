using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JA_GaussianBlurProj
{
    public class ImageCalculationClasses
    {
        public static float[,] ExtendImage(float[,] pixels, int radius)
        {
            float[,] output = new float[pixels.GetLength(0) + (2 * radius), pixels.GetLength(1) + (2 * radius)];

            for (int i = radius; i < pixels.GetLength(0) + radius; i++)
            {
                for (int j = radius; j < pixels.GetLength(1) + radius; j++)
                {
                    output[i, j] = pixels[i - radius, j - radius];
                }
            }

            for (int i = 0; i < radius; i++)
            {
                for (int j = 0; j < pixels.GetLength(1); j++)
                {
                    output[i, j] = pixels[0, j];
                }
            }

            for (int i = radius + pixels.GetLength(0); i < (2 * radius) + pixels.GetLength(0); i++)
            {
                for (int j = 0; j < pixels.GetLength(1); j++)
                {
                    output[i, j] = pixels[pixels.GetLength(0) - 1, j];
                }
            }

            for (int i = 0; i < output.GetLength(0); i++)
            {
                for (int j = 0; j < radius; j++)
                {
                    output[i, j] = output[i, radius];
                }
            }

            for (int i = 0; i < output.GetLength(0); i++)
            {
                for (int j = output.GetLength(1) - radius; j < output.GetLength(1); j++)
                {
                    output[i, j] = output[i, output.GetLength(1) - radius];
                }
            }

            return output;
        }

        public static float[] ExtractKernel(float[,] pixels, int radius, int x, int y)
        {
            int diameter = 2 * radius + 1;
            float[] output = new float[diameter * diameter];

            for (int i = x, i1 = 0; i1 < diameter; i++, i1++)
            {
                for (int j = y, j1 = 0; j1 < diameter; j++, j1++)
                {
                    output[i1 * diameter + j1] = pixels[i, j];
                }
            }

            return output;
        }

        public static (float[], float) CalculateGaussianMatrix(int radius, float sigma)
        {
            int diameter = 2 * radius + 1;
            float[] output = new float[diameter * diameter];
            float param = 0.0f;
            float sumOfWeight = 0.0f;

            for (int i = 0; i < diameter; i++)
            {
                for (int j = 0; j < diameter; j++)
                {
                    param = (float)(1 / (Math.PI * 2.0 * Math.Pow(sigma, 2.0)) *
                                    Math.Exp(-(Math.Pow(i - radius, 2.0) + Math.Pow(j - radius, 2.0)) /
                                             (2.0 * Math.Pow(sigma, 2.0))));
                    output[i * diameter + j] = param;
                    sumOfWeight += param;
                }
            }

            return (output, sumOfWeight);
        }
    }
}
