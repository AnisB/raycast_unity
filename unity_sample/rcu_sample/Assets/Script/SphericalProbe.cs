using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SphericalProbe : MonoBehaviour
{
    [Range(16, 512)]
    public int rayResolution = 512;

    public bool hitPoints = true;

    public bool missPoints = true;

    [Range(1.0f, 50.0f)]
    public float range = 10.0f;

    RCUManager rcuManager = null;
    RCUCApi.RCURay[] rayArray = null;
    RCUCApi.RCUIntersection[] intersectionArray = null;

    Dictionary<int, GameObject> objectMap = new Dictionary<int, GameObject>();

    [ContextMenu("Rebuild")]
    public void RebuildRayTracingManager()
    {
        if(rcuManager != null)
        {
            objectMap.Clear();
            rcuManager.ReleaseRaycastEnvironment();
            rcuManager = null;
        }

        rcuManager = new RCUManager();
        MeshRenderer[] meshRendererArray = FindObjectsOfType<MeshRenderer>();
        for (int meshIdx = 0; meshIdx < meshRendererArray.Length; ++meshIdx)
        {
            GameObject gameObj = meshRendererArray[meshIdx].gameObject;
            objectMap[gameObj.GetInstanceID()] = gameObj;
        }
        rcuManager.SetupRaycastEnvironment(meshRendererArray);
        rayArray = new RCUCApi.RCURay[rayResolution * rayResolution];
        intersectionArray = new RCUCApi.RCUIntersection[rayResolution * rayResolution];
    }


    public void Start()
    {
        rcuManager = new RCUManager();
        MeshRenderer[] meshRendererArray = FindObjectsOfType<MeshRenderer>();
        for(int meshIdx = 0; meshIdx < meshRendererArray.Length; ++meshIdx)
        {
            GameObject gameObj = meshRendererArray[meshIdx].gameObject;
            objectMap[gameObj.GetInstanceID()] = gameObj;
        }
        rcuManager.SetupRaycastEnvironment(meshRendererArray);
        rayArray = new RCUCApi.RCURay[rayResolution * rayResolution];
        intersectionArray = new RCUCApi.RCUIntersection[rayResolution * rayResolution];
    }

    public void Update()
    {
        if(rcuManager == null)
        {
            rcuManager = new RCUManager();
            MeshRenderer[] meshRendererArray = FindObjectsOfType<MeshRenderer>();
            for (int meshIdx = 0; meshIdx < meshRendererArray.Length; ++meshIdx)
            {
                GameObject gameObj = meshRendererArray[meshIdx].gameObject;
                objectMap[gameObj.GetInstanceID()] = gameObj;
            }
            rcuManager.SetupRaycastEnvironment(meshRendererArray);
            rayArray = new RCUCApi.RCURay[rayResolution * rayResolution];
            intersectionArray = new RCUCApi.RCUIntersection[rayResolution * rayResolution];
        }

        for(int thetaIdx = 0; thetaIdx < rayResolution; ++thetaIdx)
        {
            float theta = thetaIdx / (2.0f * Mathf.PI);
            for (int phiIdx = 0; phiIdx < rayResolution; ++phiIdx)
            {
                float phi = phiIdx / (Mathf.PI);

                int currentRayIndex = thetaIdx * rayResolution + phiIdx;

                rayArray[currentRayIndex].org_x = gameObject.transform.position.x;
                rayArray[currentRayIndex].org_y = gameObject.transform.position.y;
                rayArray[currentRayIndex].org_z = gameObject.transform.position.z;

                rayArray[currentRayIndex].dir_x = Mathf.Sin(theta) * Mathf.Sin(phi);
                rayArray[currentRayIndex].dir_y = Mathf.Cos(theta);
                rayArray[currentRayIndex].dir_z = Mathf.Sin(theta) * Mathf.Cos(phi);

                rayArray[currentRayIndex].tmin = 0.0001f;
                rayArray[currentRayIndex].tmax = range;
            }
        }

        // Proceed with the raycast
        rcuManager.Run(rayArray, intersectionArray);

        for (int rayIdx = 0; rayIdx < rayResolution * rayResolution; ++rayIdx)
        {
            Vector3 rayOrigin = new Vector3(rayArray[rayIdx].org_x, rayArray[rayIdx].org_y, rayArray[rayIdx].org_z);
            Vector3 rayDirection = new Vector3(rayArray[rayIdx].dir_x, rayArray[rayIdx].dir_y, rayArray[rayIdx].dir_z);

            if ((int)intersectionArray[rayIdx].validity == 1)
            {
                if(hitPoints)
                {
                    Color color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    Debug.DrawLine(rayOrigin + rayDirection * (intersectionArray[rayIdx].t - 0.01f), rayOrigin + rayDirection * intersectionArray[rayIdx].t, color);
                }
            }
            else
            {
                if (missPoints)
                {
                    Color color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    Debug.DrawLine(rayOrigin + rayDirection * (range - 0.01f), rayOrigin + rayDirection * range, color);
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
