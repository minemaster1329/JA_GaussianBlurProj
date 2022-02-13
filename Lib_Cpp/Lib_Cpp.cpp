// Lib_Cpp.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "Lib_Cpp.h"
#include <immintrin.h>

float CalculatePixelCpp(float* pixels, float* weights, float sumOfWeights, int diameter)
{
    float output = 0.0f;
    diameter *= diameter;

    for (int i = 0; i < diameter; i++)
    {
        output += pixels[i] * weights[i];
    }
    return output / sumOfWeights;
}

float CalculatePixelCpp2(float* pixels, float* weights, float sumOfweights, int diameter)
{
    float output = 0.0f;
    diameter *= diameter;
    int diameter2 = diameter / 8;

    for (int i = 0; i < diameter2; i++)
    {
        __m256 pixelsVector = _mm256_set_ps(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3], pixels[i + 4], pixels[i + 5], pixels[i + 6], pixels[i + 7]);
        __m256 weightsVector = _mm256_set_ps(weights[i], weights[i + 1], weights[i + 2], weights[i + 3], weights[i + 4], weights[i + 5], weights[i + 6], weights[i + 7]);
        __m256 result = _mm256_mul_ps(pixelsVector, weightsVector);

        for (int j = 0; j < 8; j++)
        {
            output += ((float*)&result)[j];
        }
    }

    diameter2 *= 8;

    for (int i = diameter2; i < diameter; i++)
    {
        output += pixels[i] * weights[i];
    }

    return output / sumOfweights;
}

float Fun1(float* pixels, float* weights, float sumOfWeights, int diameter)
{
    __m256 vect = _mm256_set_ps(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f);
    __m256 vect2 = _mm256_set_ps(2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f);
    vect = _mm256_mul_ps(vect, vect2);

    float output = 0.0f;

    for (int i = 0; i < 8; i++)
    {
        output += ((float*)&vect)[i];
    }

    return output;
}
