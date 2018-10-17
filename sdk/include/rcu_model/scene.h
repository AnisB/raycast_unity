#pragma once

// SDK includes
#include "rcu_model/geometry_instance.h"

// bento includes
#include <bento_collection/dynamic_string.h>
#include <bento_memory/common.h>

namespace rcu
{
	struct TScene
	{
		// Generic Data
		ALLOCATOR_BASED;
		TScene(bento::IAllocator& allocator);
		bento::IAllocator& _allocator;

		// Scene Data
		bento::DynamicString sceneName;
		bento::Vector<TGeometry> geometryArray;
	};

	// Function to append	
	void append_geometry(TScene& targetScene, uint32_t objectID, uint32_t subMeshID, float* positionArray, uint32_t numVerts, int32_t* indexArray, uint32_t numTriangles, const char* debugName = nullptr);
}