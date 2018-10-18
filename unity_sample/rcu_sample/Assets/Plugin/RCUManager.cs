using System;
using UnityEngine;

public class RCUManager
{
    // Allocator used for the RCU SDK allocation
    private IntPtr rcuAllocator = IntPtr.Zero;
    private IntPtr rcuScene = IntPtr.Zero;
    private IntPtr rcuRaycastManager = IntPtr.Zero;

    public void SetupRaycastEnvironment(MeshRenderer[] meshRendererArray)
	{
        // Create all the native pointers
        rcuAllocator = RCUCApi.rcu_create_allocator();
        rcuScene = RCUCApi.rcu_create_scene(rcuAllocator);
        rcuRaycastManager = RCUCApi.rcu_create_raycast_manager(rcuAllocator);

        // Let's now push all the geometry to the plugin
        int numGameObjects = meshRendererArray.Length;
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
                    // Flatten the position array
                    uint numVerts = (uint)currentMesh.vertices.Length;
                    float[] posArray = new float[3 * numVerts];
                    for (int vIdx = 0; vIdx < numVerts; ++vIdx)
                    {
                        Vector3 currentVertex = gameObject.transform.TransformPoint(currentMesh.vertices[vIdx]);
                        posArray[3 * vIdx] = currentVertex.x;
                        posArray[3 * vIdx + 1] = currentVertex.y;
                        posArray[3 * vIdx + 2] = currentVertex.z;
                    }

                    // Flatten the index array
                    int[] subMeshIndices = currentMesh.GetIndices((int)subMeshIdx);
                    uint numTriangles = (uint)(subMeshIndices.Length / 3);
                    int[] faceArray = new int[3 * numTriangles];
                    for (int vIdx = 0; vIdx < numTriangles; ++vIdx)
                    {
                        faceArray[3 * vIdx] = subMeshIndices[3 * vIdx];
                        faceArray[3 * vIdx + 1] = subMeshIndices[3 * vIdx + 1];
                        faceArray[3 * vIdx + 2] = subMeshIndices[3 * vIdx + 2];
                    }

                    // Generate the submesh's name
                    string submeshName = gameObject.name + subMeshIdx.ToString();

                    // Push the geometry to the scene
                    RCUCApi.rcu_scene_append_geometry(rcuScene, gameObjectID, subMeshIdx, posArray, numVerts, faceArray, numTriangles, submeshName.ToCharArray());
                }
            }
        }
        
        // Init the raycast manager
        RCUCApi.rcu_raycast_manager_setup(rcuRaycastManager, rcuScene);
    }

    public void Run(float[] rayDataArray, int[] intersectionDataArray, int numRays)
	{
        RCUCApi.rcu_raycast_manager_run(rcuRaycastManager, rayDataArray, intersectionDataArray, (uint)numRays);
    }

	public void ReleaseRaycastEnvironment()
	{
        RCUCApi.rcu_destroy_raycast_manager(rcuRaycastManager);
        RCUCApi.rcu_destroy_scene(rcuScene);
        RCUCApi.rcu_destroy_allocator(rcuAllocator);
    }
}