# Command-line completion

FAKE provides command-line completion for bash and PowerShell.

## Available completions

The following completions are supported:

* available FAKE commands
* options supported by those commands
* available targets in a fake-script

## Installation for bash

### Prerequisites

* bash completion is installed
* FAKE is installed as a global tool

### OSX

 1. Install bash completion if it is not installed `brew install bash-completion`
 2. Add the following into `~/.bash_profile` (create the file if it doesn't already exist)
     ```shell
    if [ -f $(brew --prefix)/etc/bash_completion ]; then
    . $(brew --prefix)/etc/bash_completion
    fi
    ```
 3. Download the completion file https://github.com/fsharp/FAKE/tree/master/completion/bash/fake-completion.bash and place it in `/usr/local/etc/bash_completion.d/`
    ```shell
    sudo curl -L https://raw.githubusercontent.com/fsharp/FAKE/master/completion/bash/fake-completion.bash -o /usr/local/etc/bash_completion.d/fake-completion
    ```


### Linux
 1. Bash completion should be installed on a current Linux OS. Otherwise you have to install it.
 2. Download the completion file https://github.com/fsharp/FAKE/tree/master/completion/bash/fake-completion.bash and place it in `/etc/bash_completion.d/`
    ```shell
    sudo curl -L https://raw.githubusercontent.com/fsharp/FAKE/master/completion/bash/fake-completion.bash -o /etc/bash_completion.d/fake-completion
    ```

### Windows with git-bash
 1. Download the completion file https://github.com/fsharp/FAKE/tree/master/completion/bash/fake-completion.bash and place it in your home directory (`~/`)
    ```shell
    curl -L https://raw.githubusercontent.com/fsharp/FAKE/master/completion/bash/fake-completion.bash -o ~/fake-completion.bash
    ```
 2. Add the following into `~/.bashrc` (create the file if it doesn't already exist)
    ```shell
    source ~/fake-completion.bash
    ```

## Installation for PowerShell

### Prerequisites

* PowerShell >= 5.0
* FAKE is installed as a global tool

### Install completion for PowerShell

Download the `posh-fake` module from [*`posh-fake.psm1`*](https://github.com/fsharp/FAKE/tree/master/completion/powershell/posh-fake.psm1). 
Copy the module into a folder called `posh-fake`, inside one of your PowerShell module folders. You can find those in `$env:PSModulePath`.
For example `C:\Users\UserName\Documents\WindowsPowerShell\Modules\posh-fake`.
You can download the file with:
```shell
# create directory
New-Item -ItemType Directory -Path \$HOME\Documents\WindowsPowerShell\Modules\posh-fake
# download
Invoke-WebRequest -Uri https://raw.githubusercontent.com/fsharp/FAKE/master/completion/powershell/posh-fake.psm1 -OutFile \$HOME\Documents\WindowsPowerShell\Modules\posh-fake\posh-fake.psm1
```

To import the module, run
```shell
Import-Module posh-fake
```

If you do not want to manually import the module everytime you start PowerShell, you can add it to your PowerShell profile.
You can find your profile in `$PROFILE`.
Open the profile script (create the file if it doesn't already exist) and add the command `Import-Module posh-fake`.
