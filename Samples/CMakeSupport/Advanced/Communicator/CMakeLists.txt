cmake_minimum_required (VERSION 2.6)

project (Communicator CXX)

include_directories ("${PROJECT_SOURCE_DIR}")
add_library (Communicator Communicator.cxx)

install (TARGETS Communicator
    ARCHIVE DESTINATION lib
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION bin
    COMPONENT library
)
install (FILES Communicator.h DESTINATION include)
