// CAPI includes
#include "raycast_manager_c_api.h"

// SDK Includes
#include <rcu_raycast/raycast_manager.h>

// Bento includes
#include <bento_base/security.h>

RCURaycastManagerObject* rcu_create_raycast_manager(RCUAllocatorObject* allocator)
{
	assert_msg(allocator != nullptr, "Allocator was null");
	bento::IAllocator* allocPtr = (bento::IAllocator*)allocator;
	rcu::TRaycastManager* raycastManager = bento::make_new<rcu::TRaycastManager>(*allocPtr, *allocPtr);
	return (RCURaycastManagerObject*)raycastManager;

}

void rcu_raycast_manager_setup(RCURaycastManagerObject* raycastManager, RCUSceneObject* scene)
{
	assert_msg(raycastManager != nullptr, "RaycastManager was null");
	assert_msg(scene != nullptr, "Scene was null");
	rcu::TRaycastManager* raycastManagerPtr = (rcu::TRaycastManager*)raycastManager;
	rcu::TScene* scenePtr = (rcu::TScene*)scene;
	raycastManagerPtr->setup(*scenePtr);

}

void rcu_raycast_manager_release(RCURaycastManagerObject* raycastManager)
{
	assert_msg(raycastManager != nullptr, "RaycastManager was null");
	rcu::TRaycastManager* raycastManagerPtr = (rcu::TRaycastManager*)raycastManager;
	raycastManagerPtr->release();
}

void rcu_raycast_manager_run(RCURaycastManagerObject* raycastManager, TRay* rayArray, TIntersection* intersectionArray, uint32_t numRays)
{
	assert_msg(raycastManager != nullptr, "RaycastManager was null");
	rcu::TRaycastManager* raycastManagerPtr = (rcu::TRaycastManager*)raycastManager;
	raycastManagerPtr->run((rcu::TRay*)rayArray, (rcu::TIntersection*)intersectionArray, numRays);
}

void rcu_destroy_raycast_manager(RCURaycastManagerObject* raycastManager)
{
	assert_msg(raycastManager != nullptr, "RaycastManager was null");
	rcu::TRaycastManager* raycastManagerPtr = (rcu::TRaycastManager*)raycastManager;
	bento::make_delete<rcu::TRaycastManager>(raycastManagerPtr->_allocator, raycastManagerPtr);
}