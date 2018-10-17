cmake_minimum_required(VERSION 3.2)

# The defines we need for the library
set(RCU_SDK_INCLUDE ${RCU_SDK_ROOT}/include)
set(RCU_CAPI_INCLUDE ${RCU_CAPI_ROOT}/include)
set(RCU_CAPI_SRC ${RCU_CAPI_ROOT}/src)

bento_headers(header_files "${RCU_CAPI_INCLUDE}" "c_api")
bento_sources(source_files "${RCU_CAPI_SRC}" "c_api")

# Generate the static library
bento_dynamic_lib("rcu_dylib" "c_api" "${header_files};${source_files};" "${RCU_SDK_INCLUDE};${RCU_3RD_LIBRARIES};${RCU_CAPI_INCLUDE}")
target_link_libraries("rcu_dylib" "rcu_sdk" "bento_sdk" "${RCU_3RD_LIBRARIES}/embree/lib/embree3.lib")