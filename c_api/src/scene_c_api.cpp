// Bento includes
#include <bento_memory/system_allocator.h>
#include <bento_memory/common.h>
#include <bento_base/security.h>

// Internal includes
#include "rcu_model/scene.h"
#include "scene_c_api.h"

static bento::SystemAllocator _base_allocator;

RCUSceneObject* rcu_create_scene(RCUAllocatorObject* allocator)
{
	assert_msg(allocator != nullptr, "Allocator was null");
	bento::IAllocator* allocPtr = (bento::IAllocator*)allocator;
	rcu::TScene* newScene = bento::make_new<rcu::TScene>(*allocPtr, *allocPtr);
	return (RCUSceneObject*) newScene;
}

void rcu_scene_append_geometry(RCUSceneObject* scene, uint32_t geoID, uint32_t submeshID, float* positionArray, uint32_t numVerts, int32_t* indexArray, int32_t numTriangles, float* transformMatrix)
{
	assert_msg(scene != nullptr, "Scene was null");
	rcu::TScene* scenePtr = (rcu::TScene*)scene;
	rcu::append_geometry(*scenePtr, geoID, submeshID, positionArray, numVerts, indexArray, numTriangles, transformMatrix);
}

void rcu_destroy_scene(RCUSceneObject* scene)
{
	assert_msg(scene != nullptr, "Scene was null");
	rcu::TScene* scenePtr = (rcu::TScene*)scene;
	bento::make_delete<rcu::TScene>(scenePtr->_allocator, scenePtr);
}
