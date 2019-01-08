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

	void append_geometry(TScene& targetScene, uint32_t objectID, uint32_t subMeshID, float* positionArray, float* normalArray, float* texCoordArray, uint32_t numVerts, int32_t* indexArray, uint32_t numTriangles, const float* transformMatrix)
	{
		TGeometry& newGeometry = targetScene.geometryArray.extend();
		newGeometry.gameObjectID = objectID;
		newGeometry.subMeshID = subMeshID;
		newGeometry.vertexArray.resize(numVerts);
		newGeometry.normalArray.resize(numVerts);
		newGeometry.texCoordArray.resize(numVerts);
		memcpy(newGeometry.vertexArray.begin(), positionArray, sizeof(bento::Vector3) * numVerts);
		memcpy(newGeometry.normalArray.begin(), normalArray, sizeof(bento::Vector3) * numVerts);
		memcpy(newGeometry.texCoordArray.begin(), texCoordArray, sizeof(bento::Vector2) * numVerts);
		newGeometry.indexArray.resize(numTriangles);
		memcpy(newGeometry.indexArray.begin(), indexArray, sizeof(bento::IVector3) * numTriangles);

		bento::Matrix4 transform;
		memcpy(transform.m, transformMatrix, 16 * sizeof(float));
		for (uint32_t vertIdx = 0; vertIdx < numVerts; ++vertIdx)
		{
			newGeometry.vertexArray[vertIdx] = transform * newGeometry.vertexArray[vertIdx];
		}

		bento::Matrix4 normalMatrix;
		normalMatrix = bento::Inverse(transform);
		normalMatrix = bento::transpose(normalMatrix);
		for (uint32_t vertIdx = 0; vertIdx < numVerts; ++vertIdx)
		{
			bento::Vector4 normalTransformed = normalMatrix * bento::vector4(newGeometry.normalArray[vertIdx].x, newGeometry.normalArray[vertIdx].y, newGeometry.normalArray[vertIdx].z, 0.0f);
			newGeometry.normalArray[vertIdx] = bento::vector3(normalTransformed.x, normalTransformed.y, normalTransformed.z);
			newGeometry.normalArray[vertIdx] = bento::normalize(newGeometry.normalArray[vertIdx]);
		}

	}
}