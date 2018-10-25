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

    [Range(0.01f, 100.0f)]
    public float range = 10.0f;

    // Manager that holds everything related to the raycast
    RCUManager rcuManager = null;

    // Keep track of the run position for displacing the point cloud
    Vector3 positionAtRun = new Vector3();

    void InitializeRaycastData()
    {
        int maxNumRays = maxResolution * maxResolution;

        rcuManager = new RCUManager();
        MeshRenderer[] meshRendererArray = FindObjectsOfType<MeshRenderer>();
        for (int meshIdx = 0; meshIdx < meshRendererArray.Length; ++meshIdx)
        {
            GameObject gameObj = meshRendererArray[meshIdx].gameObject;
        }
        rcuManager.SetupRaycastEnvironment(meshRendererArray, maxNumRays);

    }

    [ContextMenu("Rebuild")]
    public void RebuildRayTracingManager()
    {
        if(rcuManager != null)
        {
            rcuManager.ReleaseRaycastEnvironment();
            rcuManager = null;
        }

        InitializeRaycastData();
    }

    [ContextMenu("Run")]
    void RunRays()
    {
        if (rcuManager == null)
        {
            InitializeRaycastData();
        }

        Vector3 currentDirection = new Vector3();

        for (int thetaIdx = 0; thetaIdx < rayResolution; ++thetaIdx)
        {
            float theta = thetaIdx / (rayResolution - 1.0f) * (2.0f * Mathf.PI);
            for (int phiIdx = 0; phiIdx < rayResolution; ++phiIdx)
            {
                float phi = phiIdx / (rayResolution - 1.0f) *  (Mathf.PI);

                int currentRayIndex = thetaIdx * rayResolution + phiIdx;

                currentDirection.Set(Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Cos(phi));

                rcuManager.SetRayData(gameObject.transform.position, currentDirection, 0.0001f, range, currentRayIndex);

            }
        }

        // Proceed with the raycast
        rcuManager.Run(rayResolution * rayResolution);

        // Save the position of the object when calling run
        positionAtRun = gameObject.transform.position;
    }

    public void Update()
    {
        if (rcuManager == null)
            return;

        Vector3 rayOrigin = new Vector3();
        Vector3 rayDirection = new Vector3();

        Vector3 positionShift = gameObject.transform.position - positionAtRun;

        for (int rayIdx = 0; rayIdx < rayResolution * rayResolution; ++rayIdx)
        {
            // Set the ray origin and direction
            rcuManager.RayOrigin(rayIdx, ref rayOrigin);
            rcuManager.RayDirection(rayIdx, ref rayDirection);

            // Check the validity flag
            if (rcuManager.IntersectionValidity(rayIdx))
            {
                if (hitPoints)
                {
                    float t = rcuManager.IntersectionDistance(rayIdx);
                    Debug.DrawLine(positionShift + rayOrigin + rayDirection * (t - 0.01f), positionShift + rayOrigin + rayDirection * t, Color.green);
                }
            }
            else
            {
                if (missPoints)
                {
                    Debug.DrawLine(positionShift + rayOrigin, positionShift + rayOrigin + rayDirection * 0.5f, Color.red);
                }
            }
        }
    }

    public void OnDestroy()
    {
        if(rcuManager != null)
        {
            rcuManager.ReleaseRaycastEnvironment();
            rcuManager = null;
        }
    }
}
