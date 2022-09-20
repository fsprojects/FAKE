# How to use Vagrant and quickly setup a reproducible development environment for FAKE

Since [#711](https://github.com/fsharp/FAKE/pull/711), it is possible to deploy an fresh Ubuntu
Linux virtual machine with one simple command thanks to [Vagrant](https://www.vagrantup.com/). Here
is how to proceed...

# Install the system-wide prerequisites

## VirtualBox and its Extension Pack

> VirtualBox is a powerful x86 and AMD64/Intel64 virtualization product for enterprise as well as
> home use. Not only is VirtualBox an extremely feature rich, high performance product for
> enterprise customers, it is also the only professional solution that is freely available as Open
> Source Software under the terms of the GNU General Public License (GPL) version 2.

You can install VirtualBox and the VirtualBox Extension Pack from [www.virtualbox.org/wiki/Downloads](https://www.virtualbox.org/wiki/Downloads)
or with your package manager.

## Vagrant

> Vagrant is a tool for building complete development environments. With an easy-to-use workflow
> and focus on automation, Vagrant lowers development environment setup time, increases
> development/production parity, and makes the "works on my machine" excuse a relic of the past.

You can install Vagrant from [www.vagrantup.com/downloads.html](https://www.vagrantup.com/downloads.html)
or with your package manager.

# Start a new VM

Simply `cd` to the root directory of the FAKE source code and run `vagrant up`.

The VM will be automatically downloaded, imported into VirtualBox, configured and started. The FAKE
source directory is automatically shared with the VM under the `/vagrant` directory.

Run `vagrant ssh` to directly connect via SSH to the VM and start working.

Alternately, you can directly execute any command with `vagrant ssh -c "<command>"`.
As example, you can directly invoke the `build.sh` script with the
`vagrant ssh -c "cd /vagrant && bash ./build.sh"` command.

Once you have finished, you can either:

* Run `vagrant suspend` to pause the VM.
* Run `vagrant halt` to shutdown the VM.
* Run `vagrant destroy` to delete the imported VM.
* Run `vagrant box remove ubuntu/trusty64` to remove the base box from your computer.
* Re-run `vagrant up` to reload the VM.

The full command line documentation is available at [docs.vagrantup.com](https://docs.vagrantup.com/).

# Further information

* [Vagrant Documentation](https://docs.vagrantup.com/v2/)
* [Vagrant on GitHub](https://github.com/mitchellh/vagrant)
* [What is Vagrant and why should I care](http://24ways.org/2014/what-is-vagrant-and-why-should-i-care/)
* [Packer](https://www.packer.io/) (used to create base boxes).
