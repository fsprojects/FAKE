#include <cstdlib>
#include <iostream>

int main(int argc, char ** argv)
{
    if (argc < 2)
    {
        std::cerr << "ERROR: Please provide at least one name as argument." << std::endl;
        return EXIT_FAILURE;
    }
    for (int i = 1; i < argc; i++)
        std::cout << "Hello " << argv[i] << "!" << std::endl;
    return EXIT_SUCCESS;
}
