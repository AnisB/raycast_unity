#pragma once

#include "types_c_api.h"

extern "C"
{
	// Function to create a new rcu scene
	RCU_EXPORT RCUSceneObject* rcu_create_scene(RCUAllocatorObject* allocator);

	// Function to push a new object to the scene
	RCU_EXPORT void rcu_scene_append_geometry(RCUSceneObject* scene, uint32_t geoID, uint32_t submeshID, float* positionArray,  uint32_t numVerts, int32_t* indexArray, int32_t numTriangles, float* transformMatrix);

	// Function to destroy a rcu scene
	RCU_EXPORT void rcu_destroy_scene(RCUSceneObject* scene);
}