#include <cstdlib>
#include <iostream>
#include <Communicator.h>

using namespace std;

int main(int argc, char ** argv)
{
    if (argc < 2)
    {
        cerr << "ERROR: Please provide at least one name as argument." << endl;
        return EXIT_FAILURE;
    }
    for (int i = 1; i < argc; i++)
    {
        Communicator communicator(argv[i]);
        cout << communicator.hello() << endl;
    }
    return EXIT_SUCCESS;
}
