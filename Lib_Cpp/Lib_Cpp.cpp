// Lib_Cpp.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "Lib_Cpp.h"

int MyProcCPP(int a, int b) {
	return a + b;
}

float calculate(float** pixels, float** weights, float weights_sum, int pixel_size) {
    float sum = 0.0;
    for (int i = 0; i < 2 * pixel_size; i++) {
        for (int j = 0; j < 2 * pixel_size; j++) {
            int i2 = i - pixel_size;
            int j2 = j - pixel_size;
            sum += pixels[i2][j2] * weights[i2][j2];
        }
    }
    return sum / weights_sum;
}
