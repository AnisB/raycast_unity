// sdk includes
#include "rcu_model/scene.h"

namespace rcu
{
	TScene::TScene(bento::IAllocator& allocator)
	: _allocator(allocator)
	, sceneName(allocator)
	, geometryArray(allocator)
	{
	}

	void append_geometry(TScene& targetScene, uint32_t objectID, uint32_t subMeshID, float* positionArray, uint32_t numVerts, int32_t* indexArray, uint32_t numTriangles, const char* debugName)
	{
		TGeometry& newGeometry = targetScene.geometryArray.extend();
		newGeometry.gameObjectID = objectID;
		newGeometry.subMeshID = subMeshID;
		newGeometry.debugName = debugName != nullptr ? debugName : "UNKNOWN";
		newGeometry.vertexArray.resize(numVerts);
		memcpy(newGeometry.vertexArray.begin(), positionArray, sizeof(bento::Vector3) * numVerts);
		newGeometry.indexArray.resize(numTriangles);
		memcpy(newGeometry.indexArray.begin(), indexArray, sizeof(bento::IVector3) * numTriangles);
	}
}