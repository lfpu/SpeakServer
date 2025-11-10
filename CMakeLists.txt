cmake_minimum_required(VERSION 3.10)
project(SpeakServer)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_TOOLCHAIN_FILE "C:/Users/lfpu/source/vcpkg/scripts/buildsystems/vcpkg.cmake" CACHE STRING "")


# 强制使用静态运行时（MSVC）
if(MSVC)
    foreach(flag_var
        CMAKE_C_FLAGS
        CMAKE_C_FLAGS_DEBUG
        CMAKE_C_FLAGS_RELEASE
        CMAKE_CXX_FLAGS
        CMAKE_CXX_FLAGS_DEBUG
        CMAKE_CXX_FLAGS_RELEASE)
        string(REPLACE "/MD" "/MT" ${flag_var} "${${flag_var}}")
    endforeach()
endif()




include_directories(
    ${Boost_INCLUDE_DIRS}
    ${json_SOURCE_DIR}
    include
)


message(STATUS "Boost include dirs: ${Boost_INCLUDE_DIRS}")
set(SOURCES
    src/main.cpp
    src/server.cpp
    src/audio_stream.cpp
    src/client_session.cpp
    src/config_loader.cpp
    src/control_message.cpp
    src/audio_File.cpp
    src/utils.cpp
)

add_executable(SpeakServer ${SOURCES})

target_link_libraries(SpeakServer ${Boost_LIBRARIES})

# 添加复制配置文件的命令
add_custom_command(
    TARGET SpeakServer POST_BUILD
    # COMMAND ${CMAKE_COMMAND} -E make_directory "$<TARGET_FILE_DIR:SpeakServer>/config"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${CMAKE_SOURCE_DIR}/config/config.json"
        "$<TARGET_FILE_DIR:SpeakServer>/config.json"
    COMMENT "Copying config.json to executable directory"
)