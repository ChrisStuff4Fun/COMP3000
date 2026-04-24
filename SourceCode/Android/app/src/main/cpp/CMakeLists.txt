cmake_minimum_required(VERSION 3.10)
project(seal_android)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Imported prebuilt SEAL
add_library(seal STATIC IMPORTED)
set_target_properties(seal PROPERTIES
        IMPORTED_LOCATION
        ${CMAKE_SOURCE_DIR}/lib/arm64-v8a/libseal-4.1.a
)

# Imported zlib
add_library(zlib STATIC IMPORTED)
set_target_properties(zlib PROPERTIES
        IMPORTED_LOCATION
        ${CMAKE_SOURCE_DIR}/lib/arm64-v8a/libz.a
)

# JNI library 
add_library(seal_jni SHARED seal_jni.cpp)

target_include_directories(seal_jni PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/gsl
)

target_link_libraries(seal_jni
        seal
        zlib
        log
)