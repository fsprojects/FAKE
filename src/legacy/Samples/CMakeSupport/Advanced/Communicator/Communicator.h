#ifndef COMMUNICATE_H
#define COMMUNICATE_H

#include <string>

// Provides helpers for communicating with an end-user.
class Communicator
{
  private:
    std::string _interlocutorName;

  public:
    explicit Communicator(const std::string & interlocutorName);
    std::string hello() const;
};

#endif // COMMUNICATE_H
