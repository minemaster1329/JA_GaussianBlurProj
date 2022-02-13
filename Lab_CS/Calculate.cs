using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lab_CS
{
    public class Calculate
    {
        public static float CalculatePixel(float[] pixels, float[] weights, float sumOfWeights, int diameter)
        {
            float output = 0.0f;
            diameter *= diameter;

            for (int i = 0; i < diameter; i++)
            {
                output += pixels[i] * weights[i];
            }

            return output / sumOfWeights;
        }
    }
}
