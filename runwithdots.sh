#!/bin/bash

dotnet fake build --parallel 3 > output.txt &
#echo "wtf123" > output.txt &

pid=$!
while kill -0 $pid; do
    printf '.' > /dev/tty
    sleep 2
done

exitCode=`wait $pid`

tail -n 1000 output.txt
printf "\nExitted with ${pid}" > /dev/tty

exit ${pid}

