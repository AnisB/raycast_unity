cmake_minimum_required(VERSION 3.2)
project(rcu)

# Declare the global variables for the cmake project
set(BENTO_SDK_ROOT ${PROJECT_SOURCE_DIR}/bento)
set(RCU_SDK_ROOT ${PROJECT_SOURCE_DIR}/sdk)
set(RCU_CAPI_ROOT ${PROJECT_SOURCE_DIR}/c_api)
set(RCU_TESTS_ROOT ${PROJECT_SOURCE_DIR}/tests)
set(RCU_APPLICATIONS_ROOT ${PROJECT_SOURCE_DIR}/applications)
set(RCU_3RD_LIBRARIES ${PROJECT_SOURCE_DIR}/3rd)

# Set the cmake path variable
set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${CMAKE_SOURCE_DIR}/cmake")

# This flag must be activated in order to handle properly folder flags
set_property(GLOBAL PROPERTY USE_FOLDERS ON)

# Define the cmake macros
include(CMakePlatforms)
include(CMakeMacros)
include(CMakeBuildSettings)

# Define the build options
define_plaform_settings()

# Print the platform's name
message(STATUS "The build identifier is: ${RCU_PLATFORM_NAME}")

# Create the list of allowed files to be included
set(bento_source_extensions)
list(APPEND bento_source_extensions ".h" ".cpp" ".inl" ".txt")

# Compile the bento SDK
add_subdirectory(${BENTO_SDK_ROOT}/src)

# Generate the RCU SDK
add_subdirectory(${RCU_SDK_ROOT}/src)

# Generate the dynamic library
add_subdirectory(${RCU_CAPI_ROOT}/src)