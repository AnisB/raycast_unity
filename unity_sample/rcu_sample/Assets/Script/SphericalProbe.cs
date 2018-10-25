using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SphericalProbe : MonoBehaviour
{
    const int maxResolution = 1024;
    [Range(1, maxResolution)]
    public int rayResolution = 512;

    public bool hitPoints = false;
    public bool missPoints = false;

    [Range(0.01f, 50.0f)]
    public float range = 10.0f;

    RCUManager rcuManager = null;
    float[] rayDataArray = null;
    int[] intersectionDataArray = null;
    Dictionary<int, GameObject> objectMap = new Dictionary<int, GameObject>();

    void InitializeRaycastData()
    {
        rcuManager = new RCUManager();
        MeshRenderer[] meshRendererArray = FindObjectsOfType<MeshRenderer>();
        for (int meshIdx = 0; meshIdx < meshRendererArray.Length; ++meshIdx)
        {
            GameObject gameObj = meshRendererArray[meshIdx].gameObject;
            objectMap[gameObj.GetInstanceID()] = gameObj;
        }
        int maxNumRays = maxResolution * maxResolution;
        rcuManager.SetupRaycastEnvironment(meshRendererArray);
        rayDataArray = new float[maxNumRays * RCUCApi.RayDataSize];
        intersectionDataArray = new int[maxNumRays * RCUCApi.IntersectionDataSize];
    }

    [ContextMenu("Rebuild")]
    public void RebuildRayTracingManager()
    {
        if(rcuManager != null)
        {
            objectMap.Clear();
            rcuManager.ReleaseRaycastEnvironment();
            rcuManager = null;
        }

        InitializeRaycastData();
    }

    [ContextMenu("Run")]
    public void RunSIMD()
    {
        RunRays();
    }

    public void Start()
    {
        InitializeRaycastData();
    }

    void RunRays()
    {
        if (rcuManager == null)
        {
            InitializeRaycastData();
        }

        for (int thetaIdx = 0; thetaIdx < rayResolution; ++thetaIdx)
        {
            float theta = thetaIdx / (rayResolution - 1.0f) * (2.0f * Mathf.PI);
            for (int phiIdx = 0; phiIdx < rayResolution; ++phiIdx)
            {
                float phi = phiIdx / (rayResolution - 1.0f) *  (Mathf.PI);

                int currentRayIndex = thetaIdx * rayResolution + phiIdx;

                rayDataArray[8 * currentRayIndex] = gameObject.transform.position.x;
                rayDataArray[8 * currentRayIndex + 1] = gameObject.transform.position.y;
                rayDataArray[8 * currentRayIndex + 2] = gameObject.transform.position.z;

                rayDataArray[8 * currentRayIndex + 3] = Mathf.Sin(theta) * Mathf.Sin(phi);
                rayDataArray[8 * currentRayIndex + 4] = Mathf.Cos(theta);
                rayDataArray[8 * currentRayIndex + 5] = Mathf.Sin(theta) * Mathf.Cos(phi);

                rayDataArray[8 * currentRayIndex + 6] = 0.0001f;
                rayDataArray[8 * currentRayIndex + 7] = range;
            }
        }

        // Proceed with the raycast
        rcuManager.Run(rayDataArray, intersectionDataArray, rayResolution * rayResolution);
    }

    public void Update()
    {
        for (int rayIdx = 0; rayIdx < rayResolution * rayResolution; ++rayIdx)
        {
            Vector3 rayOrigin = new Vector3(rayDataArray[8 * rayIdx], rayDataArray[8 * rayIdx + 1], rayDataArray[8 * rayIdx + 2]);
            Vector3 rayDirection = new Vector3(rayDataArray[8 * rayIdx + 3], rayDataArray[8 * rayIdx + 4], rayDataArray[8 * rayIdx + 5]);

            if ((int)intersectionDataArray[8 * rayIdx] == 1)
            {
                if (hitPoints)
                {
                    byte[] tBytes = BitConverter.GetBytes(intersectionDataArray[8 * rayIdx + 1]);
                    float t = BitConverter.ToSingle(tBytes, 0);
                    Color color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    Debug.DrawLine(rayOrigin + rayDirection * (t - 0.01f), rayOrigin + rayDirection * t, color);
                }
            }
            else
            {
                if (missPoints)
                {
                    Color color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    Debug.DrawLine(rayOrigin, rayOrigin + rayDirection * 0.5f, color);
                }
            }
        }
    }

    public void OnDestroy()
    {
        if(rcuManager != null)
        {
            objectMap.Clear();
            rcuManager.ReleaseRaycastEnvironment();
            rcuManager = null;
        }
    }
}
