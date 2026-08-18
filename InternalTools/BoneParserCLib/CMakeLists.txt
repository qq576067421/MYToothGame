cmake_minimum_required(VERSION 3.20)

project(BoneParserCLib VERSION 0.1.0 LANGUAGES CXX)

option(BONE_PARSER_CLIB_COPY_TO_UNITY "Copy the built native library to the Unity plugin folder for the current target platform." ON)

set(BONE_PARSER_CLIB_UNITY_WINDOWS_X64_PLUGIN_DIR
    "${CMAKE_CURRENT_LIST_DIR}/../../Project/ToothGameProject/Assets/Plugins/x86_64"
    CACHE PATH
    "Unity Windows x64 native plugin output directory.")

set(BONE_PARSER_CLIB_UNITY_ANDROID_LIBS_DIR
    "${CMAKE_CURRENT_LIST_DIR}/../../Project/ToothGameProject/Assets/Plugins/Android/libs"
    CACHE PATH
    "Unity Android native plugin libs output directory.")

add_library(BoneParserCLib SHARED
    src/BoneParserCLib.cpp)

target_include_directories(BoneParserCLib
    PUBLIC
        "${CMAKE_CURRENT_LIST_DIR}/include")

target_compile_features(BoneParserCLib PRIVATE cxx_std_17)

target_compile_definitions(BoneParserCLib
    PRIVATE
        BONE_PARSER_CLIB_BUILD)

set_target_properties(BoneParserCLib PROPERTIES
    OUTPUT_NAME "BoneParserCLib"
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN YES)

if(MSVC)
    target_compile_options(BoneParserCLib PRIVATE /W4 /permissive-)
else()
    target_compile_options(BoneParserCLib PRIVATE -Wall -Wextra -Wpedantic)
endif()

if(ANDROID)
    target_link_options(BoneParserCLib PRIVATE -Wl,--exclude-libs,ALL)
endif()

if(WIN32 AND BONE_PARSER_CLIB_COPY_TO_UNITY)
    add_custom_command(TARGET BoneParserCLib POST_BUILD
        COMMAND "${CMAKE_COMMAND}" -E make_directory "${BONE_PARSER_CLIB_UNITY_WINDOWS_X64_PLUGIN_DIR}"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "$<TARGET_FILE:BoneParserCLib>"
            "${BONE_PARSER_CLIB_UNITY_WINDOWS_X64_PLUGIN_DIR}/$<TARGET_FILE_NAME:BoneParserCLib>"
        VERBATIM)
elseif(ANDROID AND BONE_PARSER_CLIB_COPY_TO_UNITY)
    if(CMAKE_ANDROID_ARCH_ABI)
        set(BONE_PARSER_CLIB_ANDROID_ABI "${CMAKE_ANDROID_ARCH_ABI}")
    elseif(ANDROID_ABI)
        set(BONE_PARSER_CLIB_ANDROID_ABI "${ANDROID_ABI}")
    else()
        message(FATAL_ERROR "Android ABI is required when copying BoneParserCLib to Unity.")
    endif()

    set(BONE_PARSER_CLIB_UNITY_ANDROID_PLUGIN_DIR
        "${BONE_PARSER_CLIB_UNITY_ANDROID_LIBS_DIR}/${BONE_PARSER_CLIB_ANDROID_ABI}")

    add_custom_command(TARGET BoneParserCLib POST_BUILD
        COMMAND "${CMAKE_COMMAND}" -E make_directory "${BONE_PARSER_CLIB_UNITY_ANDROID_PLUGIN_DIR}"
        COMMAND "${CMAKE_COMMAND}" -E copy_if_different
            "$<TARGET_FILE:BoneParserCLib>"
            "${BONE_PARSER_CLIB_UNITY_ANDROID_PLUGIN_DIR}/$<TARGET_FILE_NAME:BoneParserCLib>"
        VERBATIM)
endif()
