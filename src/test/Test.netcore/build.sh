#!/bin/bash

if [ ! -f .fake/Runtime/Fake.netcore.exe ]; then
  # Bootstrapping?
  curl https://raw.github.com/matthid/FAKE/core_clr/script/bootstrap_fake.sh -o .fake/bootstap_fake.sh
  chmod +x .fake/bootstrap_fake.sh
  ./.fake/bootstrap_fake.sh
fi

.fake/Runtime/Fake.netcore.exe run $@ 