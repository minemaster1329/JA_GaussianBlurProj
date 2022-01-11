// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the LIBASM_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// LIBASM_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef LIBASM_EXPORTS
#define LIBASM_API __declspec(dllexport)
#else
#define LIBASM_API __declspec(dllimport)
#endif

// This class is exported from the dll
class LIBASM_API CLibASM {
public:
	CLibASM(void);
	// TODO: add your methods here.
};

extern LIBASM_API int nLibASM;

LIBASM_API int fnLibASM(void);
