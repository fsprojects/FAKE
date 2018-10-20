# Command-line completion

Fake provides command-line completion for bash and PowerShell.

## Available completions

For PowerShell, the following completions are supported:

* available FAKE commands
* options supported by those commands
* available targets in a fake-script

For bash, only the available targets are currently supported.

## Installation for bash

### Prerequisites

* Tab completion for bash:

If you have tab completion for other commands (such as git parameters) then you can skip to [Install](#Install-completion-for-bash).
On OSX, you might have to do the following:

<pre><code class="lang-bash">
brew install bash-completion
</code></pre>
Paste the following into `~/.bash_profile` (create the file if it doesn't already exist)
<pre><code class="lang-bash">
if [ -f $(brew --prefix)/etc/bash_completion ]; then
    . $(brew --prefix)/etc/bash_completion
fi
</code></pre>

### Install completion for bash
Download the contents of https://github.com/fsharp/FAKE/tree/master/completion/bash and run

<pre><code class="lang-bash">
cd completion/bash
./install.sh
</code></pre>

## Installation for PowerShell

### Prerequisites

* PowerShell >= 5.0
* FAKE installed as a global tool

### Install completion for PowerShell

Download the `posh-fake` module from <https://github.com/fsharp/FAKE/tree/master/completion/powershell/posh-fake.psm1>. Copy the module into a folder called `posh-fake`, inside one of your PowerShell module folders. You can find those in `$env:PSModulePath`.
For example C:\Users\UserName\Documents\WindowsPowerShell\Modules\posh-fake.

To import the module, run
<pre><code class="lang-bash">
Import-Module posh-fake
</code></pre>

If you do not want to manually import the module everytime you start PowerShell, you can add it to your PowerShell profile.
You can find your profile in `$PROFILE`.
Open the profile script (create the file if it doesn't already exist) and add the command `Import-Module posh-fake`.
