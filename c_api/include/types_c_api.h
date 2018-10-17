#pragma once

#ifdef WINDOWSPC
	#define RCU_EXPORT __declspec(dllexport)
#else
	#define RCU_EXPORT
#endif

struct RCUAllocatorObject;
