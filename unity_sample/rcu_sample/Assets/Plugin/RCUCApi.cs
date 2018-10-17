using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class RCUCApi
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RCURay
    {
        public float org_x;
        public float org_y;
        public float org_z;
        public float dir_x;
        public float dir_y;
        public float dir_z;
        public float tmin;
        public float tmax;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RCUIntersection
    {
        public int validity;
        public float t;
        public uint geoID;
        public uint subMeshID;
        public uint triangleID;
        public float u;
        public float v;
        public float w;
    }

    // Allocator API
    [DllImport ("rcu_dylib")]
	public static extern IntPtr rcu_create_allocator();
	[DllImport ("rcu_dylib")]
	public static extern void rcu_destroy_allocator(IntPtr alloc);

	// Scene API
	[DllImport ("rcu_dylib")]
	public static extern IntPtr rcu_create_scene(IntPtr alloc);
	[DllImport ("rcu_dylib")]
	public static extern void rcu_scene_append_geometry(IntPtr scene, uint geoID, uint submeshID, float[] positionArray, uint numVerts, int[] indexArray, uint numTriangles, char[] debug_name);
	[DllImport ("rcu_dylib")]
	public static extern void rcu_destroy_scene(IntPtr scene);

	// Raycast Manager API
	[DllImport ("rcu_dylib")]
	public static extern IntPtr rcu_create_raycast_manager(IntPtr alloc);
	[DllImport ("rcu_dylib")]
	public static extern void rcu_raycast_manager_setup(IntPtr manager, IntPtr scene);
	[DllImport ("rcu_dylib")]
	public static extern void rcu_raycast_manager_release(IntPtr manager);
	[DllImport ("rcu_dylib")]
	public static extern void rcu_raycast_manager_run(IntPtr manager, RCURay[] rayArray, RCUIntersection[] intersectionArray, uint numRays);
	[DllImport ("rcu_dylib")]
	public static extern void rcu_destroy_raycast_manager(IntPtr manager);
}