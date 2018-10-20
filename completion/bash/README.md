# BASH tab completion for possible FAKE tasks

## Prerequisites

If you have tab completion for other commands (such as git parameters) then you can skip to [Install](#install-completion-for-fake)

##### OSX
```bash
brew install bash-completion
```
Paste the following into ~/.bash_profile (create the file if it doesn't already exist)
```bash
if [ -f $(brew --prefix)/etc/bash_completion ]; then
    . $(brew --prefix)/etc/bash_completion
fi
```

## Install completion for FAKE
```bash
cd completion/bash
./install.sh
```
