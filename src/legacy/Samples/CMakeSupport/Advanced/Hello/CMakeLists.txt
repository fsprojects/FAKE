cmake_minimum_required (VERSION 2.6)

project (Hello CXX)

# Not using FindLibrary to keep the example simple.
if (NOT DEFINED COMMUNICATOR_INCLUDE_DIR OR NOT DEFINED COMMUNICATOR_LIBRARY)
  message (FATAL_ERROR "Please define both COMMUNICATOR_INCLUDE_DIR and COMMUNICATOR_LIBRARY.")
endif ()

include_directories ("${COMMUNICATOR_INCLUDE_DIR}")

add_executable (hello main.cxx)

target_link_libraries (hello "${COMMUNICATOR_LIBRARY}")
