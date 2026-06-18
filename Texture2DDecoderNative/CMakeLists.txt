cmake_minimum_required(VERSION 3.16)
project(Texture2DDecoderNative CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

add_library(Texture2DDecoderNative SHARED
    astc.cpp
    atc.cpp
    bcn.cpp
    crunch.cpp
    dllmain.cpp
    etc.cpp
    pvrtc.cpp
    unitycrunch.cpp
)

target_include_directories(Texture2DDecoderNative PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

if(CMAKE_CXX_COMPILER_ID MATCHES "Clang|AppleClang|GNU")
    target_compile_options(Texture2DDecoderNative PRIVATE -Wno-c++11-narrowing)
endif()

set_target_properties(Texture2DDecoderNative PROPERTIES
    OUTPUT_NAME "Texture2DDecoderNative"
    PREFIX "lib"
)
