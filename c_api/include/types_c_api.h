#pragma once

// Base includes
#include <stdint.h>

#ifdef WINDOWSPC
	#define RCU_EXPORT __declspec(dllexport)
#else
	#define RCU_EXPORT
#endif

struct RCURaycastManagerObject;
struct RCUAllocatorObject;
struct RCUSceneObject;
