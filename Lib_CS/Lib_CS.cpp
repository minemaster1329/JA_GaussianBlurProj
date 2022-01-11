// Lib_CS.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "Lib_CS.h"


// This is an example of an exported variable
LIBCS_API int nLibCS=0;

// This is an example of an exported function.
LIBCS_API int fnLibCS(void)
{
    return 0;
}

// This is the constructor of a class that has been exported.
CLibCS::CLibCS()
{
    return;
}
