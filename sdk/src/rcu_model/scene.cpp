// sdk includes
#include "rcu_model/scene.h"

// bento includes
#include <bento_math/matrix4.h>

namespace rcu
{
	TScene::TScene(bento::IAllocator& allocator)
	: _allocator(allocator)
	, sceneName(allocator)
	, geometryArray(allocator)
	{
	}

	void append_geometry(TScene& targetScene, uint32_t objectID, uint32_t subMeshID, float* positionArray, uint32_t numVerts, int32_t* indexArray, uint32_t numTriangles, const float* transformMatrix)
	{
		TGeometry& newGeometry = targetScene.geometryArray.extend();
		newGeometry.gameObjectID = objectID;
		newGeometry.subMeshID = subMeshID;
		newGeometry.vertexArray.resize(numVerts);
		memcpy(newGeometry.vertexArray.begin(), positionArray, sizeof(bento::Vector3) * numVerts);
		newGeometry.indexArray.resize(numTriangles);
		memcpy(newGeometry.indexArray.begin(), indexArray, sizeof(bento::IVector3) * numTriangles);

		bento::Matrix4 transform;
		memcpy(transform.m, transformMatrix, 16 * sizeof(float));
		for (uint32_t vertIdx = 0; vertIdx < numVerts; ++vertIdx)
		{
			newGeometry.vertexArray[vertIdx] = transform * newGeometry.vertexArray[vertIdx];
		}
	}
}