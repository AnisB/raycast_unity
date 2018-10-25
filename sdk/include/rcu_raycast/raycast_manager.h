#pragma once

// SDK includes
#include <rcu_model/scene.h>
#include <rcu_raycast/intersection.h>

// External includes
#include <embree/include/embree3/rtcore.h>

namespace rcu
{
	class TRaycastManager
	{
	public:
		ALLOCATOR_BASED;
		TRaycastManager(bento::IAllocator& allocator);
		~TRaycastManager();

		void setup(const TScene& targetScene);
		void release();

		void run(const TRay* rayArray,  TIntersection* intersectionArray, uint32_t numRays);

	private:
		// Embree structures
		RTCDevice _device;
		RTCScene _scene;

		const TScene* _targetScene;
		bento::Vector<uint32_t> _geometriesIndexes;
		bento::Vector<RTCRayHit16> _rayHitGroupArray;
		bento::Vector<RTCRayHit> _rayHitSingleArray;
	public:
		bento::IAllocator& _allocator;

	};
}