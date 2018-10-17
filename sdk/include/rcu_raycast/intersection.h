#pragma once

// bento includes
#include <bento_math/types.h>

namespace rcu
{
	struct TRay
	{
		bento::Vector3 origin;
		bento::Vector3 direction;
		float tmin;
		float tmax;
	};

	struct TIntersection
	{
		int validity;
		uint32_t geometryID;
		uint32_t subMeshID;
		uint32_t triangleID;
		bento::Vector3 barycentricCoordinates;
	};
}