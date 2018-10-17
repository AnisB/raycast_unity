cmake_minimum_required(VERSION 3.2)

# The defines we need for the library
set(RCU_SDK_INCLUDE ${RCU_SDK_ROOT}/include)
set(RCU_SDK_SOURCE ${RCU_SDK_ROOT}/src)

sub_directory_list(sub_projects_headers "${RCU_SDK_INCLUDE}")
foreach(header_dir ${sub_projects_headers})
	bento_headers(tmp_header_list "${RCU_SDK_INCLUDE}/${header_dir}" "${header_dir}")
	list(APPEND header_files "${tmp_header_list}")
endforeach()

sub_directory_list(sub_projects_sources "${RCU_SDK_SOURCE}")
foreach(source_dir ${sub_projects_sources})
	bento_sources(tmp_source_list "${RCU_SDK_SOURCE}/${source_dir}" "${source_dir}")
	list(APPEND source_files "${tmp_source_list}")
endforeach()

# Generate the static library
bento_static_lib("rcu_sdk" "sdk" "${header_files};${source_files};" "${RCU_SDK_INCLUDE};${RCU_3RD_LIBRARIES};${BENTO_SDK_ROOT}/include")
