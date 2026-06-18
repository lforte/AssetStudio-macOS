cmake_minimum_required(VERSION 3.16)
project(AssetStudioFBXNative CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

if(NOT FBXSDK_ROOT)
    set(FBXSDK_ROOT "/Applications/Autodesk/FBX SDK/2020.3.9")
endif()

set(FBXSDK_LIB_DIR "${FBXSDK_ROOT}/lib/clang/release")
if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    set(FBXSDK_LIB_DIR "${FBXSDK_ROOT}/lib/clang/debug")
endif()

add_library(AssetStudioFBXNative SHARED
    api.cpp
    asfbx_anim_context.cpp
    asfbx_context.cpp
    asfbx_morph_context.cpp
    asfbx_skin_context.cpp
    utils.cpp
)

target_compile_definitions(AssetStudioFBXNative PRIVATE _AS_DLL)
target_include_directories(AssetStudioFBXNative PRIVATE ${CMAKE_CURRENT_SOURCE_DIR} "${FBXSDK_ROOT}/include")
target_link_libraries(AssetStudioFBXNative PRIVATE "${FBXSDK_LIB_DIR}/libfbxsdk.a" "-framework CoreFoundation" "-framework Foundation" "-liconv" "-lz" "-lxml2")

set_target_properties(AssetStudioFBXNative PROPERTIES
    OUTPUT_NAME "AssetStudioFBXNative"
    PREFIX "lib"
)
