#pragma once

#include "types_c_api.h"

extern "C"
{
	// Function to create a new rcu raycast_manager
	RCU_EXPORT RCURaycastManagerObject* rcu_create_raycast_manager(RCUAllocatorObject* allocator);

	// Function to setup a scene into the raycast manager
	RCU_EXPORT void rcu_raycast_manager_setup(RCURaycastManagerObject* raycastManager, RCUSceneObject* scene);

	// Function to release a scene from the raycast manager
	RCU_EXPORT void rcu_raycast_manager_release(RCURaycastManagerObject* raycastManager);

	RCU_EXPORT void rcu_raycast_manager_run(RCURaycastManagerObject* raycastManager, float* rayArrayData, int* intersectionDataArray, uint32_t numRays);

	// Function to destroy a rcu raycast manager
	RCU_EXPORT void rcu_destroy_raycast_manager(RCURaycastManagerObject* raycastManager);
}