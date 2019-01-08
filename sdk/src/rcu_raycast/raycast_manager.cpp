// sdk includes
#include "rcu_raycast/raycast_manager.h"

// bento includes
#include <bento_base/log.h>
#include <bento_math/vector3.h>
#include <bento_math/vector2.h>

// External includes
#include <embree/include/embree3/rtcore.h>
#include <float.h>

namespace rcu
{
	void error_handler(void*, const RTCError code, const char* str = nullptr)
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
	, _rayHitGroupArray(allocator)
	, _rayHitSingleArray(allocator, 16)
	{
		// Create the device
		_device = rtcNewDevice("");

		error_handler(nullptr, rtcGetDeviceError(_device));

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

		// Create a new scene
		_scene = rtcNewScene(_device);

		// Set the target scene
		_targetScene = &scene;

		for (uint32_t geoIdx = 0; geoIdx < numGeometries; ++geoIdx)
		{
			// Fetch the current geometry
			const TGeometry& currentGeometry = scene.geometryArray[geoIdx];

			// Create a new geometry
			RTCGeometry newGeo = rtcNewGeometry(_device, RTC_GEOMETRY_TYPE_TRIANGLE);

			// Upload the positions
			bento::Vector3* vertices = (bento::Vector3*)rtcSetNewGeometryBuffer(newGeo, RTC_BUFFER_TYPE_VERTEX, 0, RTC_FORMAT_FLOAT3, sizeof(bento::Vector3), currentGeometry.vertexArray.size());
			memcpy(vertices, currentGeometry.vertexArray.begin(), sizeof(bento::Vector3) * currentGeometry.vertexArray.size());

			// Upload the triangles
			bento::IVector3* triangles = (bento::IVector3*)rtcSetNewGeometryBuffer(newGeo, RTC_BUFFER_TYPE_INDEX, 0, RTC_FORMAT_UINT3, sizeof(bento::IVector3), currentGeometry.indexArray.size());
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

		int32_t numRayGroups = (uint32_t)(numRays / 16);
		int32_t rayBatchGroupSize = (uint32_t)(numRayGroups * 16);
		uint32_t rayRemain = numRays % 16;

		// Compute the ray quotient and remain
		if (true)
		{
			numRayGroups = (uint32_t)(numRays / 16);
			rayBatchGroupSize = (uint32_t)(numRayGroups * 16);
			rayRemain = numRays % 16;
		}
		else
		{
			numRayGroups = 0;
			rayBatchGroupSize = 0;
			rayRemain = numRays;
		}

		// Make sure the arrays are the right size
		if (_rayHitGroupArray.size() < (uint32_t)numRayGroups)
		{
			_rayHitGroupArray.resize(numRayGroups);
		}
		if (_rayHitSingleArray.size() < rayRemain)
		{
			_rayHitSingleArray.resize(rayRemain);
		}

		// Initialize the ray group array
		for (int32_t rayGroupIndex = 0; rayGroupIndex < numRayGroups; ++rayGroupIndex)
		{
			// Fetch the target ray
			RTCRayHit16& rayHitGroup = _rayHitGroupArray[rayGroupIndex];

			for (uint32_t rayIdx = 0; rayIdx < 16; ++rayIdx)
			{
				// Grab the current ray to tpush to the group
				const TRay& currentRay = rayArray[ 16 * rayGroupIndex + rayIdx];

				// Set the origin
				rayHitGroup.ray.org_x[rayIdx] = currentRay.origin.x;
				rayHitGroup.ray.org_y[rayIdx] = currentRay.origin.y;
				rayHitGroup.ray.org_z[rayIdx] = currentRay.origin.z;

				// Set the direction
				rayHitGroup.ray.dir_x[rayIdx] = currentRay.direction.x;
				rayHitGroup.ray.dir_y[rayIdx] = currentRay.direction.y;
				rayHitGroup.ray.dir_z[rayIdx] = currentRay.direction.z;

				// Set the min/max values
				rayHitGroup.ray.tnear[rayIdx] = currentRay.tmin;
				rayHitGroup.ray.tfar[rayIdx] = currentRay.tmax;

				rayHitGroup.hit.instID[rayIdx][0] = RTC_INVALID_GEOMETRY_ID;
				rayHitGroup.hit.geomID[rayIdx] = RTC_INVALID_GEOMETRY_ID;
				rayHitGroup.ray.mask[rayIdx] = 0xffffffff;
				rayHitGroup.ray.time[rayIdx] = 0.0f;
			}
		}

		// Initialize the single rays
		for (uint32_t raySingleIndex = 0; raySingleIndex < rayRemain; ++raySingleIndex)
		{
			// Fetch the target ray
			RTCRayHit& rayHitSingle = _rayHitSingleArray[raySingleIndex];

			// Grab the current ray to tpush to the group
			const TRay& currentRay = rayArray[rayBatchGroupSize + raySingleIndex];

			// Set the origin
			rayHitSingle.ray.org_x = currentRay.origin.x;
			rayHitSingle.ray.org_y = currentRay.origin.y;
			rayHitSingle.ray.org_z = currentRay.origin.z;

			// Set the direction
			rayHitSingle.ray.dir_x = currentRay.direction.x;
			rayHitSingle.ray.dir_y = currentRay.direction.y;
			rayHitSingle.ray.dir_z = currentRay.direction.z;

			// Set the min/max values
			rayHitSingle.ray.tnear = currentRay.tmin;
			rayHitSingle.ray.tfar = currentRay.tmax;

			rayHitSingle.hit.instID[0] = RTC_INVALID_GEOMETRY_ID;
			rayHitSingle.hit.geomID = RTC_INVALID_GEOMETRY_ID;
			rayHitSingle.ray.mask = 0xffffffff;
			rayHitSingle.ray.time = 0.0f;
		}

		// All the flags that
		int validityFlags = 0xff;

		// Let's run all the SIMD rays
		#pragma omp parallel for
		for (int32_t rayGroupIndex = 0; rayGroupIndex < numRayGroups; ++rayGroupIndex)
		{
			rtcIntersect16(&validityFlags, _scene, &context, &_rayHitGroupArray[rayGroupIndex]);
		}

		// Let's run all non-SIMD rays
		for (uint32_t raySingleIndex = 0; raySingleIndex < rayRemain; ++raySingleIndex)
		{
			rtcIntersect1(_scene, &context, &_rayHitSingleArray[raySingleIndex]);
		}

		// Process the intersections
		for (int32_t rayGroupIndex = 0; rayGroupIndex < numRayGroups; ++rayGroupIndex)
		{
			// Fetch the target ray
			RTCRayHit16& rayHitGroup = _rayHitGroupArray[rayGroupIndex];

			for (uint32_t rayIdx = 0; rayIdx < 16; ++rayIdx)
			{
				// Fetch the intersection to fill
				TIntersection& currentIntersection = intersectionArray[16 * rayGroupIndex + rayIdx];

				// Process the hit
				if (rayHitGroup.hit.geomID[rayIdx] != RTC_INVALID_GEOMETRY_ID)
				{
					const TGeometry& targetGeometry = _targetScene->geometryArray[rayHitGroup.hit.geomID[rayIdx]];
					currentIntersection.validity = 1;
					currentIntersection.t = rayHitGroup.ray.tfar[rayIdx];
					currentIntersection.geometryID = targetGeometry.gameObjectID;
					currentIntersection.subMeshID = targetGeometry.subMeshID;
					currentIntersection.triangleID = rayHitGroup.hit.primID[rayIdx];
					currentIntersection.barycentricCoordinates = { 1.0f - rayHitGroup.hit.u[rayIdx] - rayHitGroup.hit.v[rayIdx], rayHitGroup.hit.u[rayIdx], rayHitGroup.hit.v[rayIdx] };

					// Grab the face's indexes
					const bento::IVector3& currentFace = targetGeometry.indexArray[currentIntersection.triangleID];

					// Interpolate the position
					currentIntersection.position = targetGeometry.vertexArray[currentFace.x] * currentIntersection.barycentricCoordinates.x
						+ targetGeometry.vertexArray[currentFace.y] * currentIntersection.barycentricCoordinates.y
						+ targetGeometry.vertexArray[currentFace.z] * currentIntersection.barycentricCoordinates.z;

					// Interpolate the normal
					currentIntersection.normal = targetGeometry.normalArray[currentFace.x] * currentIntersection.barycentricCoordinates.x
						+ targetGeometry.normalArray[currentFace.y] * currentIntersection.barycentricCoordinates.y
						+ targetGeometry.normalArray[currentFace.z] * currentIntersection.barycentricCoordinates.z;

					// Interpolate the texCoord
					currentIntersection.texCoord = targetGeometry.texCoordArray[currentFace.x] * currentIntersection.barycentricCoordinates.x
						+ targetGeometry.texCoordArray[currentFace.y] * currentIntersection.barycentricCoordinates.y
						+ targetGeometry.texCoordArray[currentFace.z] * currentIntersection.barycentricCoordinates.z;
				}
				else
				{
					currentIntersection.validity = 0;
					currentIntersection.t = FLT_MAX;
					currentIntersection.geometryID = (uint32_t)-1;
					currentIntersection.subMeshID = (uint32_t)-1;
					currentIntersection.triangleID = (uint32_t)-1;
					currentIntersection.barycentricCoordinates = { 0, 0, 0 };
					currentIntersection.position = { 0, 0, 0 };
					currentIntersection.normal = { 0, 0, 0 };
					currentIntersection.texCoord = { 0, 0 };
				}
			}
		}

		for (uint32_t raySingleIndex = 0; raySingleIndex < rayRemain; ++raySingleIndex)
		{
			// Fetch the target ray
			RTCRayHit& rayHitSingle = _rayHitSingleArray[raySingleIndex];

			// Fetch the intersection to fill
			TIntersection& currentIntersection = intersectionArray[rayBatchGroupSize + raySingleIndex];

			// Process the hit
			if (rayHitSingle.hit.geomID != RTC_INVALID_GEOMETRY_ID)
			{
				const TGeometry& targetGeometry = _targetScene->geometryArray[rayHitSingle.hit.geomID];
				currentIntersection.validity = 1;
				currentIntersection.t = rayHitSingle.ray.tfar;
				currentIntersection.geometryID = targetGeometry.gameObjectID;
				currentIntersection.subMeshID = targetGeometry.subMeshID;
				currentIntersection.triangleID = rayHitSingle.hit.primID;
				currentIntersection.barycentricCoordinates = { 1.0f - rayHitSingle.hit.u - rayHitSingle.hit.v, rayHitSingle.hit.u, rayHitSingle.hit.v };

				// Grab the face's indexes
				const bento::IVector3& currentFace = targetGeometry.indexArray[currentIntersection.triangleID];

				// Interpolate the position
				currentIntersection.position = targetGeometry.vertexArray[currentFace.x] * currentIntersection.barycentricCoordinates.x
					+ targetGeometry.vertexArray[currentFace.y] * currentIntersection.barycentricCoordinates.y
					+ targetGeometry.vertexArray[currentFace.z] * currentIntersection.barycentricCoordinates.z;

				// Interpolate the normal
				currentIntersection.normal = targetGeometry.normalArray[currentFace.x] * currentIntersection.barycentricCoordinates.x
					+ targetGeometry.normalArray[currentFace.y] * currentIntersection.barycentricCoordinates.y
					+ targetGeometry.normalArray[currentFace.z] * currentIntersection.barycentricCoordinates.z;

				// Interpolate the texCoord
				currentIntersection.texCoord = targetGeometry.texCoordArray[currentFace.x] * currentIntersection.barycentricCoordinates.x
					+ targetGeometry.texCoordArray[currentFace.y] * currentIntersection.barycentricCoordinates.y
					+ targetGeometry.texCoordArray[currentFace.z] * currentIntersection.barycentricCoordinates.z;
			}
			else
			{
				currentIntersection.validity = 0;
				currentIntersection.t = FLT_MAX;
				currentIntersection.geometryID = (uint32_t)-1;
				currentIntersection.subMeshID = (uint32_t)-1;
				currentIntersection.triangleID = (uint32_t)-1;
				currentIntersection.barycentricCoordinates = { 0, 0, 0 };
				currentIntersection.position = { 0, 0, 0 };
				currentIntersection.normal = { 0, 0, 0 };
				currentIntersection.texCoord = { 0, 0 };
			}
		}
	}
}