using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class RCUCApi
{
    // Size of the ray data structure
    public const int RayDataSize = 8;

    // Size of the intersection data structure
    public const int IntersectionDataSize = 8;

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
	public static extern void rcu_raycast_manager_run(IntPtr manager, float[] rayDataArray, int[] intersectionDataArray, uint numRays);
	[DllImport ("rcu_dylib")]
	public static extern void rcu_destroy_raycast_manager(IntPtr manager);
}