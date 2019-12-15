#!/bin/bash
set -x
COREHOST_TRACE=1 dotnet fake build --parallel 3 > output.txt 2> error.txt &
#echo "wtf123" > output.txt &

pid=$!

val=1
hasExited=0

# wait max 40 minutes
while [ $val -lt 40 ]; do
    val=$(($val + 1))
    sleep 60
    if kill -0 $pid; then
        printf '.' > /dev/tty
    else
        hasExited=1
        break
    fi
done

# exit if not 
if $hasExited = 0; then
    printf 'sending ctrl+C as the build already rund 40 minutes' > /dev/tty
    kill -SIGINT $pid
fi

( sleep 60 ; echo 'timeout and kill'; kill -9 $pid ) &
exitCode=`wait $pid`

tail -n 10000 output.txt
tail -n 10000 error.txt
printf "\nExitted with ${exitCode}" > /dev/tty

exit ${exitCode}

