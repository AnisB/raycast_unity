#pragma once

// Base includes
#include <stdint.h>

#ifdef WINDOWSPC
	#define RCU_EXPORT __declspec(dllexport)
#else
	#define RCU_EXPORT
#endif

struct TRay
{
	float origin[3];
	float direction[3];
	float tmin;
	float tmax;
};

struct TIntersection
{
	int validity;
	uint32_t objectID;
	uint32_t subMeshID;
	uint32_t triangleID;
	float barycentricCoordinates[3];
};

struct RCURaycastManagerObject;
struct RCUAllocatorObject;
struct RCUSceneObject;
