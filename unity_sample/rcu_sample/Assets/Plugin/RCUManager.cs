using System;
using System.Diagnostics;
using UnityEngine;

public class RCUManager
{
    // Allocator used for the RCU SDK allocation
    private IntPtr rcuAllocator = IntPtr.Zero;
    private IntPtr rcuScene = IntPtr.Zero;
    private IntPtr rcuRaycastManager = IntPtr.Zero;

    // Stopwatch used for the performance measures
    Stopwatch sw = new Stopwatch();

    public void SetupRaycastEnvironment(MeshRenderer[] meshRendererArray)
	{
        // Initialize the stopwatch
        sw.Restart();

        // Create all the native pointers
        rcuAllocator = RCUCApi.rcu_create_allocator();
        rcuScene = RCUCApi.rcu_create_scene(rcuAllocator);
        rcuRaycastManager = RCUCApi.rcu_create_raycast_manager(rcuAllocator);

        // Array that holds the matrix
        float[] transformMatrix = new float[16];

        int maxVertCount = 0;
        int numGameObjects = meshRendererArray.Length;
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
            }
        }

        // Allocate the array for the marshaling
        float[] vertArray = new float[3 * maxVertCount];

        // Let's now push all the geometry to the plugin
        for(int geoIdx = 0; geoIdx < numGameObjects; ++geoIdx)
        {
            // Grab the next mesh renderer
            MeshRenderer meshRenderer = meshRendererArray[geoIdx];

            // Grab the next game object
            GameObject gameObject = meshRenderer.gameObject;

            // Grab the id of the object
            uint gameObjectID = (uint)gameObject.GetInstanceID();

            // Grab the mesh filter
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if(meshFilter != null && meshFilter.sharedMesh)
            {
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

                    // Flatten the index array
                    int[] subMeshIndices = currentMesh.GetIndices((int)subMeshIdx);
                    uint numTriangles = (uint)(subMeshIndices.Length / 3);

                    // Push the geometry to the scene
                    RCUCApi.rcu_scene_append_geometry(rcuScene, gameObjectID, subMeshIdx, vertArray, numVerts, subMeshIndices, numTriangles, transformMatrix);
                }
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

    public void Run(float[] rayDataArray, int[] intersectionDataArray, int numRays)
	{
        sw.Restart();
        RCUCApi.rcu_raycast_manager_run(rcuRaycastManager, rayDataArray, intersectionDataArray, (uint)numRays);
        sw.Stop();
        UnityEngine.Debug.Log("RCU: Throwing the rays took " + sw.Elapsed.ToString());
    }

    public void ReleaseRaycastEnvironment()
	{
        RCUCApi.rcu_destroy_raycast_manager(rcuRaycastManager);
        RCUCApi.rcu_destroy_scene(rcuScene);
        RCUCApi.rcu_destroy_allocator(rcuAllocator);
    }
}