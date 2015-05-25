#include "Communicator.h"

using namespace std;

Communicator::Communicator(const string & interlocutorName) :
  _interlocutorName(interlocutorName)
{
}

string Communicator::hello() const
{
  return "Hello " + _interlocutorName + "!";
}
