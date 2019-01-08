#pragma once

// Bento includes
#include <bento_collection//dynamic_string.h>
#include <bento_math/types.h>

// External includes
#include <stdint.h>

namespace rcu
{
	struct TGeometry
	{
		ALLOCATOR_BASED;
		TGeometry(bento::IAllocator& allocator)
		: vertexArray(allocator)
		, normalArray(allocator)
		, texCoordArray(allocator)
		, indexArray(allocator)
		{

		}
		uint32_t gameObjectID;
		uint32_t subMeshID;
		bento::Vector<bento::Vector3> vertexArray;
		bento::Vector<bento::Vector3> normalArray;
		bento::Vector<bento::Vector2> texCoordArray;
		bento::Vector<bento::IVector3> indexArray;
	};
}