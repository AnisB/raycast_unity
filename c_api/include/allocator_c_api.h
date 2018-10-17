#pragma once

#include "types_c_api.h"

extern "C"
{
	// Function to create a new rcu allocator
	RCU_EXPORT RCUAllocatorObject* rcu_create_allocator();

	// Function to destroy a rcu allocator
	RCU_EXPORT void rcu_destroy_allocator(RCUAllocatorObject* allocator);
}