// Lib_ASM.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "Lib_ASM.h"


// This is an example of an exported variable
LIBASM_API int nLibASM=0;

// This is an example of an exported function.
LIBASM_API int fnLibASM(void)
{
    return 0;
}

// This is the constructor of a class that has been exported.
CLibASM::CLibASM()
{
    return;
}
