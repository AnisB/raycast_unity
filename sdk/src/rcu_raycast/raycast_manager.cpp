// sdk includes
#include "rcu_raycast/raycast_manager.h"

// bento includes
#include <bento_base/log.h>

// External includes
#include <embree/include/embree3/rtcore.h>
#include <float.h>

namespace rcu
{
	void error_handler(void*, const RTCError code, const char* str)
	{
		if (code == RTC_ERROR_NONE)
			return;

		switch (code) {
		case RTC_ERROR_UNKNOWN: bento::default_logger()->log(bento::LogLevel::error, "EMBREE", "RTC_ERROR_UNKNOWN"); break;
		case RTC_ERROR_INVALID_ARGUMENT: bento::default_logger()->log(bento::LogLevel::error, "EMBREE", "RTC_ERROR_INVALID_ARGUMENT"); break;
		case RTC_ERROR_INVALID_OPERATION: bento::default_logger()->log(bento::LogLevel::error, "EMBREE", "RTC_ERROR_INVALID_OPERATION"); break;
		case RTC_ERROR_OUT_OF_MEMORY: bento::default_logger()->log(bento::LogLevel::error, "EMBREE", "RTC_ERROR_OUT_OF_MEMORY"); break;
		case RTC_ERROR_UNSUPPORTED_CPU: bento::default_logger()->log(bento::LogLevel::error, "EMBREE", "RTC_ERROR_UNSUPPORTED_CPU"); break;
		case RTC_ERROR_CANCELLED: bento::default_logger()->log(bento::LogLevel::error, "EMBREE", "RTC_ERROR_CANCELLED"); break;
		default: bento::default_logger()->log(bento::LogLevel::error, "EMBREE", "invalid error code"); break;
		}

		bento::default_logger()->log(bento::LogLevel::error, "EMBREE", str);
	}

	TRaycastManager::TRaycastManager(bento::IAllocator& allocator)
	: _allocator(allocator)
	, _geometriesIndexes(allocator)
	{
		// Create the device
		_device = rtcNewDevice("");

		// Set the error handler
		rtcSetDeviceErrorFunction(_device, error_handler, nullptr);
	}

	TRaycastManager::~TRaycastManager()
	{
		// Release the previously created device
		rtcReleaseDevice(_device);
	}

	void TRaycastManager::setup(const TScene& scene)
	{
		// loop through the geometries
		uint32_t numGeometries = scene.geometryArray.size();
		_geometriesIndexes.resize(numGeometries);
		for (uint32_t geoIdx = 0; geoIdx < numGeometries; ++geoIdx)
		{
			// Fetch the current geometry
			const TGeometry& currentGeometry = scene.geometryArray[geoIdx];

			// Create a new geometry
			RTCGeometry newGeo = rtcNewGeometry(_device, RTC_GEOMETRY_TYPE_TRIANGLE);

			// Upload the buffers
			bento::Vector3* vertices = (bento::Vector3*)rtcSetNewGeometryBuffer(newGeo, RTC_BUFFER_TYPE_VERTEX, 0, RTC_FORMAT_FLOAT3, sizeof(bento::Vector3*), currentGeometry.vertexArray.size());
			memcpy(vertices, currentGeometry.vertexArray.begin(), sizeof(bento::Vector3) * currentGeometry.vertexArray.size());
			bento::IVector3* triangles = (bento::IVector3*)rtcSetNewGeometryBuffer(newGeo, RTC_BUFFER_TYPE_INDEX, 0, RTC_FORMAT_UINT3, sizeof(bento::IVector3*), currentGeometry.indexArray.size());
			memcpy(triangles, currentGeometry.indexArray.begin(), sizeof(bento::IVector3) * currentGeometry.indexArray.size());

			// Commit the geometry
			rtcCommitGeometry(newGeo);

			// Attach it to the scene and keep track of the index
			_geometriesIndexes[geoIdx] = rtcAttachGeometry(_scene, newGeo);

			// Release the geometry
			rtcReleaseGeometry(newGeo);
		}

		// Commit the scene
		rtcCommitScene(_scene);
	}

	void TRaycastManager::release()
	{
		rtcReleaseScene(_scene);
		_scene = nullptr;
	}

	void TRaycastManager::run(const TRay* rayArray, TIntersection* intersectionArray, uint32_t numRays)
	{
		// Create an intersection context
		RTCIntersectContext context;
		rtcInitIntersectContext(&context);

		// Create the hit array
		bento::Vector<RTCRayHit> rayHitArray(_allocator, numRays);

		// Initialize the ray array
		for (uint32_t rayIndex = 0; rayIndex < numRays; ++rayIndex)
		{
			// Grab the current ray
			const TRay& currentRay = rayArray[rayIndex];

			// Set the origin
			rayHitArray[rayIndex].ray.org_x = currentRay.origin.x;
			rayHitArray[rayIndex].ray.org_y = currentRay.origin.y;
			rayHitArray[rayIndex].ray.org_z = currentRay.origin.z;

			// Set the direction
			rayHitArray[rayIndex].ray.dir_x = currentRay.direction.x;
			rayHitArray[rayIndex].ray.dir_y = currentRay.direction.y;
			rayHitArray[rayIndex].ray.dir_z = currentRay.direction.z;

			// Set the min/max values
			rayHitArray[rayIndex].ray.tnear = currentRay.tmin;
			rayHitArray[rayIndex].ray.tfar = currentRay.tmax;
		}

		// Throw all our rays
		rtcIntersect1M(_scene, &context, rayHitArray.begin(), numRays, 0);

		// Process the intersections
		for (uint32_t rayIndex = 0; rayIndex < numRays; ++rayIndex)
		{
			// Fetch the embree hit to read and process
			RTCRayHit& currentHit = rayHitArray[rayIndex];

			// Fetch the intersection to fill
			TIntersection& currentIntersection = intersectionArray[rayIndex];

			// Process the hit
			if (currentHit.hit.geomID != RTC_INVALID_GEOMETRY_ID)
			{
				const TGeometry& targetGeometry = _targetScene->geometryArray[currentHit.hit.geomID];
				currentIntersection.validity = 1;
				currentIntersection.geometryID = targetGeometry.gameObjectID;
				currentIntersection.subMeshID = targetGeometry.subMeshID;
				currentIntersection.triangleID = currentHit.hit.primID;
				currentIntersection.barycentricCoordinates = { currentHit.hit.u, currentHit.hit.v };
			}
			else
			{
				currentIntersection.validity = 0;
				currentIntersection.geometryID = (uint32_t)-1;
				currentIntersection.subMeshID = (uint32_t)-1;
				currentIntersection.triangleID = (uint32_t)-1;
				currentIntersection.barycentricCoordinates = { 0, 0 };
			}
		}
	}
}