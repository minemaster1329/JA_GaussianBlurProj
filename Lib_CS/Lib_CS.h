// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the LIBCS_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// LIBCS_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef LIBCS_EXPORTS
#define LIBCS_API __declspec(dllexport)
#else
#define LIBCS_API __declspec(dllimport)
#endif

// This class is exported from the dll
class LIBCS_API CLibCS {
public:
	CLibCS(void);
	// TODO: add your methods here.
};

extern LIBCS_API int nLibCS;

LIBCS_API int fnLibCS(void);
