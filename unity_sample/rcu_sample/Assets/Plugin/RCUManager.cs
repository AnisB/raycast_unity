using System;
using System.Diagnostics;
using UnityEngine;

public class RCUManager
{
    // Allocator used for the RCU SDK allocation
    private IntPtr rcuAllocator = IntPtr.Zero;
    private IntPtr rcuScene = IntPtr.Zero;
    private IntPtr rcuRaycastManager = IntPtr.Zero;

    // Structure that stores pointed to the array of MeshFilters
    MeshFilter[] meshFilterArray = null;

    // Data that hold all the ray and intersection data
    int currentMaxNumRays = 1;
    float[] rayDataArray = null;
    int[] intersectionDataArray = null;

    // Stopwatch used for the performance measures
    Stopwatch sw = new Stopwatch();

    public void SetupRaycastEnvironment(MeshRenderer[] meshRendererArray, int maxNumRays)
	{
        // Initialize the stopwatch
        sw.Restart();

        // Allocate the ray arrays
        ResetRayCount(maxNumRays);

        // Create all the native pointers
        rcuAllocator = RCUCApi.rcu_create_allocator();
        rcuScene = RCUCApi.rcu_create_scene(rcuAllocator);
        rcuRaycastManager = RCUCApi.rcu_create_raycast_manager(rcuAllocator);

        // Array that holds the matrix
        float[] transformMatrix = new float[16];

        int maxVertCount = 0;
        int numGameObjects = meshRendererArray.Length;
        int numMeshFilters = 0;
        for (int geoIdx = 0; geoIdx < numGameObjects; ++geoIdx)
        {
            // Grab the next mesh renderer
            MeshRenderer meshRenderer = meshRendererArray[geoIdx];

            // Grab the next game object
            GameObject gameObject = meshRenderer.gameObject;

            // Grab the mesh filter
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh)
            {
                // Fetch the mesh to push
                Mesh currentMesh = meshFilter.sharedMesh;

                // Contrbute to the max
                maxVertCount = Math.Max(currentMesh.vertexCount, maxVertCount);

                // Increase the mesh filter count
                numMeshFilters++;
            }
        }

        // Create the mesh filter array
        meshFilterArray = new MeshFilter[numMeshFilters];

        // Allocate the array for the marshaling
        float[] vertArray = new float[3 * maxVertCount];
        float[] normalDataArray = new float[3 * maxVertCount];
        float[] texDataCoord = new float[2 * maxVertCount];

        int meshFilterIterator = 0;

        // Let's now push all the geometry to the plugin
        for (int geoIdx = 0; geoIdx < numGameObjects; ++geoIdx)
        {
            // Grab the next mesh renderer
            MeshRenderer meshRenderer = meshRendererArray[geoIdx];

            // Grab the next game object
            GameObject gameObject = meshRenderer.gameObject;

            // Grab the mesh filter
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if(meshFilter != null && meshFilter.sharedMesh)
            {
                // Set it in the array
                meshFilterArray[meshFilterIterator] = meshFilter;

                // Fetch the mesh to push
                Mesh currentMesh = meshFilter.sharedMesh;

                uint subMeshCount = (uint)currentMesh.subMeshCount;
                for(uint subMeshIdx = 0; subMeshIdx < subMeshCount; ++subMeshIdx)
                {

                    Matrix4x4 transform = gameObject.transform.localToWorldMatrix.transpose;
                    for(int i = 0; i < 16; ++i)
                    {
                        transformMatrix[i] = transform[i];
                    }

                    // Flatten the position array
                    Vector3[] positionArray = currentMesh.vertices;
                    uint numVerts = (uint)positionArray.Length;
                    for (int vIdx = 0; vIdx < numVerts; ++vIdx)
                    {
                        vertArray[3 * vIdx] = positionArray[vIdx].x;
                        vertArray[3 * vIdx + 1] = positionArray[vIdx].y;
                        vertArray[3 * vIdx + 2] = positionArray[vIdx].z;
                    }

                    Vector3[] normalArray = currentMesh.normals;
                    uint numNormals = (uint)normalArray.Length;
                    for (int nIdx = 0; nIdx < numNormals; ++nIdx)
                    {
                        normalDataArray[3 * nIdx] = normalArray[nIdx].x;
                        normalDataArray[3 * nIdx + 1] = normalArray[nIdx].y;
                        normalDataArray[3 * nIdx + 2] = normalArray[nIdx].z;
                    }

                    Vector2[] uvArray = currentMesh.uv;
                    int numTexCoords = uvArray.Length;
                    if(numTexCoords > 0)
                    {
                        for (int tIdx = 0; tIdx < numTexCoords; ++tIdx)
                        {
                            texDataCoord[2 * tIdx] = uvArray[tIdx].x;
                            texDataCoord[2 * tIdx + 1] = uvArray[tIdx].y;
                        }
                    }

                    // Flatten the index array
                    int[] subMeshIndices = currentMesh.GetIndices((int)subMeshIdx);
                    uint numTriangles = (uint)(subMeshIndices.Length / 3);

                    // Push the geometry to the scene
                    RCUCApi.rcu_scene_append_geometry(rcuScene, (uint)meshFilterIterator, subMeshIdx, vertArray, normalDataArray, texDataCoord, numVerts, subMeshIndices, numTriangles, transformMatrix);
                }

                meshFilterIterator++;
            }
        }
        sw.Stop();

        UnityEngine.Debug.Log("RCU: Pushing data to the plugin took " + sw.Elapsed.ToString());

        // Init the raycast manager
        sw.Restart();
        RCUCApi.rcu_raycast_manager_setup(rcuRaycastManager, rcuScene);
        sw.Stop();
        UnityEngine.Debug.Log("RCU: Initializing the raycasting structures took " + sw.Elapsed.ToString());
    }

    public void Run(int numRays)
	{
        sw.Restart();
        RCUCApi.rcu_raycast_manager_run(rcuRaycastManager, rayDataArray, intersectionDataArray, (uint)numRays);
        sw.Stop();
        UnityEngine.Debug.Log("RCU: Throwing the rays took " + sw.Elapsed.ToString());
    }

    public void ReleaseRaycastEnvironment()
	{
        meshFilterArray = null;
        rayDataArray = null;
        intersectionDataArray = null;
        RCUCApi.rcu_destroy_raycast_manager(rcuRaycastManager);
        RCUCApi.rcu_destroy_scene(rcuScene);
        RCUCApi.rcu_destroy_allocator(rcuAllocator);
    }

    public void ResetRayCount(int maxNumRays)
    {
        currentMaxNumRays = maxNumRays;
        rayDataArray = new float[currentMaxNumRays * RCUCApi.RayDataSize];
        intersectionDataArray = new int[currentMaxNumRays * RCUCApi.IntersectionDataSize];
    }

    public void SetRayData(Vector3 origin, Vector3 direction, float tmin, float tmax, int rayIdx)
    {
        rayDataArray[RCUCApi.RayDataSize * rayIdx + RCUCApi.RayOriginX] = origin.x;
        rayDataArray[RCUCApi.RayDataSize * rayIdx + RCUCApi.RayOriginY] = origin.y;
        rayDataArray[RCUCApi.RayDataSize * rayIdx + RCUCApi.RayOriginZ] = origin.z;

        rayDataArray[RCUCApi.RayDataSize * rayIdx + RCUCApi.RayDirectionX] = direction.x;
        rayDataArray[RCUCApi.RayDataSize * rayIdx + RCUCApi.RayDirectionY] = direction.y;
        rayDataArray[RCUCApi.RayDataSize * rayIdx + RCUCApi.RayDirectionZ] = direction.z;

        rayDataArray[RCUCApi.RayDataSize * rayIdx + RCUCApi.RayMinRange] = tmin;
        rayDataArray[RCUCApi.RayDataSize * rayIdx + RCUCApi.RayMaxRange] = tmax;
    }

    public void RayOrigin(int targetRay, ref Vector3 outOrigin)
    {
        outOrigin.Set(rayDataArray[RCUCApi.RayDataSize * targetRay], rayDataArray[RCUCApi.RayDataSize * targetRay + 1], rayDataArray[RCUCApi.RayDataSize * targetRay + 2]);
    }

    public void RayDirection(int targetRay, ref Vector3 outDirection)
    {
        outDirection.Set(rayDataArray[RCUCApi.RayDataSize * targetRay + 3], rayDataArray[RCUCApi.RayDataSize * targetRay + 4], rayDataArray[RCUCApi.RayDataSize * targetRay + 5]);
    }

    public bool IntersectionValidity(int targetIntersection)
    {
        return intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionValidity] == 1;
    }

    public float IntersectionDistance(int targetIntersection)
    {
        int distance = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionDistance];
        byte[] tBytes = BitConverter.GetBytes(distance);
        return BitConverter.ToSingle(tBytes, 0);
    }

    public MeshFilter IntersectionMeshFilter(int targetIntersection)
    {
        int geoIndex = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionGeoIndex];
        return meshFilterArray[geoIndex];
    }

    public int IntersectionSubMeshIndex(int targetIntersection)
    {
        return intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionSubmeshIndex];
    }

    public int IntersectionTriangleIndex(int targetIntersection)
    {
        return intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionTriIndex];
    }

    public void IntersectionBarycentrics(int targetIntersection, ref Vector3 outBarycentrics)
    {
        int uIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionBarycentricUIndex];
        int vIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionBarycentricVIndex];
        int wIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionBarycentricWIndex];
        byte[] uBytes = BitConverter.GetBytes(uIdx);
        byte[] vBytes = BitConverter.GetBytes(vIdx);
        byte[] wBytes = BitConverter.GetBytes(wIdx);
        outBarycentrics.Set(BitConverter.ToSingle(uBytes, 0), BitConverter.ToSingle(vBytes, 0), BitConverter.ToSingle(wBytes, 0));
    }

    public void IntersectionPosition(int targetIntersection, ref Vector3 outPosition)
    {
        int xIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionPositionXIndex];
        int yIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionPositionYIndex];
        int zIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionPositionZIndex];
        byte[] xBytes = BitConverter.GetBytes(xIdx);
        byte[] yBytes = BitConverter.GetBytes(yIdx);
        byte[] zBytes = BitConverter.GetBytes(zIdx);
        outPosition.Set(BitConverter.ToSingle(xBytes, 0), BitConverter.ToSingle(yBytes, 0), BitConverter.ToSingle(zBytes, 0));
    }

    public void IntersectionNormal(int targetIntersection, ref Vector3 outNormal)
    {
        int xIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionNormalXIndex];
        int yIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionNormalYIndex];
        int zIdx = intersectionDataArray[RCUCApi.IntersectionDataSize * targetIntersection + RCUCApi.IntersectionNormalZIndex];
        byte[] xBytes = BitConverter.GetBytes(xIdx);
        byte[] yBytes = BitConverter.GetBytes(yIdx);
        byte[] zBytes = BitConverter.GetBytes(zIdx);
        outNormal.Set(BitConverter.ToSingle(xBytes, 0), BitConverter.ToSingle(yBytes, 0), BitConverter.ToSingle(zBytes, 0));
    }
}