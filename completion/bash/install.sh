#! /bin/bash
if [ -f $(brew --prefix)/etc/bash_completion ]; then
    COMPLETIONS_FOLDER=$(brew --prefix)/etc/bash_completion.d
else
    COMPLETIONS_FOLDER=$(pkg-config --variable=completionsdir bash-completion)
fi
echo "Copying FAKE completions to $COMPLETIONS_FOLDER"
THISDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
sudo cp $THISDIR/fake $COMPLETIONS_FOLDER
source $COMPLETIONS_FOLDER/fake
echo "Done!"
