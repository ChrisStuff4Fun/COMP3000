cmake_minimum_required(VERSION 3.15)

project(seal_wrapper LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ─────────────────────────────
# OPTION A: bundled SEAL
# ─────────────────────────────
add_subdirectory(external/SEAL)

add_library(seal_wrapper SHARED
    cpp/seal_wrapper.cpp
    cpp/geofence.cpp
)

target_include_directories(seal_wrapper PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_link_libraries(seal_wrapper PRIVATE SEAL::seal)

if (WIN32)
    set_target_properties(seal_wrapper PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
endif()