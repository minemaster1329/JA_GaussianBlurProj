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
