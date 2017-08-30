#!/usr/bin/env bash
# Partly from https://github.com/dotnet/cli/blob/rel/1.0.0/scripts/obtain/dotnet-install.sh, but rewritten
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Use in the the functions: eval $invocation
invocation='say_verbose "Calling: ${yellow:-}${FUNCNAME[0]} ${green:-}$*${normal:-}"'

# standard output may be used as a return value in the functions
# we need a way to write text on the screen in the functions so that
# it won't interfere with the return value.
# Exposing stream 3 as a pipe to standard output of the script itself
exec 3>&1

# Setup some colors to use. These need to work in fairly limited shells, like the Ubuntu Docker container where there are only 8 colors.
# See if stdout is a terminal
if [ -t 1 ]; then
    # see if it supports colors
    ncolors=$(tput colors)
    if [ -n "$ncolors" ] && [ $ncolors -ge 8 ]; then
        bold="$(tput bold       || echo)"
        normal="$(tput sgr0     || echo)"
        black="$(tput setaf 0   || echo)"
        red="$(tput setaf 1     || echo)"
        green="$(tput setaf 2   || echo)"
        yellow="$(tput setaf 3  || echo)"
        blue="$(tput setaf 4    || echo)"
        magenta="$(tput setaf 5 || echo)"
        cyan="$(tput setaf 6    || echo)"
        white="$(tput setaf 7   || echo)"
    fi
fi

say_err() {
    printf "%b\n" "${red:-}fake-boot: Error: $1${normal:-}" >&2
}

say() {
    # using stream 3 (defined in the beginning) to not interfere with stdout of functions
    # which may be used as return value
    printf "%b\n" "${cyan:-}fake-boot:${normal:-} $1" >&3
}

say_verbose() {
    if [ "$verbose" = true ]; then
        say "$1"
    fi
}

get_current_os_name() {
    eval $invocation

    local uname=$(uname)
    if test "$OS" = "Windows_NT"; then
        echo "win7"
        return 0
    elif [ "$uname" = "Darwin" ]; then
        echo "osx.10.11"
        return 0
    else
        # Detect Distro
        if [ "$(cat /etc/*-release | grep -cim1 ubuntu)" -eq 1 ]; then
            
            if [ "$(cat /etc/*-release | grep -cim1 16.04)" -eq 1 ]; then
                echo "ubuntu.16.04"
                return 0
            fi
            echo "ubuntu.14.04"
            #echo "ubuntu"
            return 0
        elif [ "$(cat /etc/*-release | grep -cim1 centos)" -eq 1 ]; then
            echo "centos.7"
            return 0
        elif [ "$(cat /etc/*-release | grep -cim1 rhel)" -eq 1 ]; then
            echo "rhel.7.0"
            return 0
        elif [ "$(cat /etc/*-release | grep -cim1 debian)" -eq 1 ]; then
            echo "debian.8"
            return 0
        elif [ "$(cat /etc/*-release | grep -cim1 fedora)" -eq 1 ]; then
            if [ "$(cat /etc/*-release | grep -cim1 23)" -eq 1 ]; then
                echo "fedora.23"
                return 0
            fi
        elif [ "$(cat /etc/*-release | grep -cim1 opensuse)" -eq 1 ]; then
            if [ "$(cat /etc/*-release | grep -cim1 13.2)" -eq 1 ]; then
                echo "opensuse.13.2"
                return 0
            fi
        fi
    fi
    
    say_err "OS name could not be detected"
    return 1
}

machine_has() {
    eval $invocation
    
    which "$1" > /dev/null 2>&1
    return $?
}

check_min_reqs() {
    if ! machine_has "curl"; then
        say_err "curl is required to download dotnet. Install curl to proceed."
        return 1
    fi
    
    return 0
}

check_pre_reqs() {
    eval $invocation
    
    local failing=false;

    if [ "${DOTNET_INSTALL_SKIP_PREREQS:-}" = "1" ]; then
        return 0
    fi

    if [ "$(uname)" = "Linux" ]; then
        if ! [ -x "$(command -v ldconfig)" ]; then
            echo "ldconfig is not in PATH, trying /sbin/ldconfig."
            LDCONFIG_COMMAND="/sbin/ldconfig"
        else
            LDCONFIG_COMMAND="ldconfig"
        fi

        [ -z "$($LDCONFIG_COMMAND -p | grep libunwind)" ] && say_err "Unable to locate libunwind. Install libunwind to continue" && failing=true
        [ -z "$($LDCONFIG_COMMAND -p | grep libssl)" ] && say_err "Unable to locate libssl. Install libssl to continue" && failing=true
        [ -z "$($LDCONFIG_COMMAND -p | grep libcurl)" ] && say_err "Unable to locate libcurl. Install libcurl to continue" && failing=true
        [ -z "$($LDCONFIG_COMMAND -p | grep libicu)" ] && say_err "Unable to locate libicu. Install libicu to continue" && failing=true
    fi

    if [ "$failing" = true ]; then
       return 1
    fi
    
    return 0
}

# args:
# input - $1
to_lowercase() {
    #eval $invocation
    
    echo "$1" | tr '[:upper:]' '[:lower:]'
    return 0
}

# args:
# input - $1
remove_trailing_slash() {
    #eval $invocation
    
    local input=${1:-}
    echo "${input%/}"
    return 0
}

# args:
# input - $1
remove_beginning_slash() {
    #eval $invocation
    
    local input=${1:-}
    echo "${input#/}"
    return 0
}

# args:
# root_path - $1
# child_path - $2 - this parameter can be empty
combine_paths() {
    eval $invocation
    
    # TODO: Consider making it work with any number of paths. For now:
    if [ ! -z "${3:-}" ]; then
        say_err "combine_paths: Function takes two parameters."
        return 1
    fi
    
    local root_path=$(remove_trailing_slash $1)
    local child_path=$(remove_beginning_slash ${2:-})
    say_verbose "combine_paths: root_path=$root_path"
    say_verbose "combine_paths: child_path=$child_path"
    echo "$root_path/$child_path"
    return 0
}

get_machine_architecture() {
    eval $invocation
    if [ $(uname -m) == 'x86_64' ]; then
        echo "x64"
        return 0
    elif test "$OS" = "Windows_NT"; then
        echo "x86"
        return 0
    else
        # Currently the only one supported
        echo "x64"
        return 0
    fi
}


# version_info is a conceptual two line string representing commit hash and 4-part version
# format:
# Line 1: # commit_hash
# Line 2: # 4-part version

# args:
# version_text - stdin
get_version_from_version_info() {
    eval $invocation
    
    cat | tail -n 1
    return 0
}

# args:
# version_text - stdin
get_commit_hash_from_version_info() {
    eval $invocation
    
    cat | head -n 1
    return 0
}

# args:
# install_root - $1
# specific_version - $2
is_fake_package_installed() {
    eval $invocation
    
    local install_root=$1
    local specific_version=${2:-}
    
    local fake_package_path=$(combine_paths $(combine_paths $install_root $specific_version) $osname-$architecture)
    say_verbose "is_fake_package_installed: fake_package_path=$fake_package_path"
    
    if [ -d "$fake_package_path" ]; then
        return 0
    else
        return 1
    fi
}

get_latest_version() {
    local expectedFile="fake-dotnetcore-$osname-$architecture.zip"
    local my_specific_version=$(curl -s "https://api.github.com/repos/$github_repo/releases" \
        | grep browser_download_url \
        | cut -d '"' -f 4 \
        | grep "$expectedFile" \
        | head -n 1 \
        | cut -d '/' -f 8)
    if [ -z "$my_specific_version" ]; then
        say_err "Could not find a version for $expectedFile, please open an issue on https://github.com/fsharp/FAKE/ so that we can add support for it!"
        return 1
    fi
    echo "$my_specific_version"
    return 0
}

# args:
# specific_version - $1
construct_download_link() {
    eval $invocation
    
    local expectedFile="fake-dotnetcore-$osname-$architecture.zip"
    
    local specific_version=${1:-}
    
    if [ ! -z "$specific_version" ]; then
        echo "https://github.com/$github_repo/releases/download/$specific_version/$expectedFile"
        return 0
    fi
    
    local version_file_url=$(curl -s "https://api.github.com/repos/$github_repo/releases" \
        | grep browser_download_url \
        | cut -d '"' -f 4 \
        | grep "$expectedFile" \
        | head -n 1)
    
    echo "$version_file_url"
    return 0
}

# args:
# specific_version - $1
construct_packages_download_link() {
    eval $invocation
    
    local expectedFile="fake-dotnetcore-packages.zip"
    
    local specific_version=${1:-}
    
    if [ ! -z "$specific_version" ]; then
        echo "https://github.com/$github_repo/releases/download/$specific_version/$expectedFile"
        return 0
    fi
    
    local version_file_url=$(curl -s "https://api.github.com/repos/$github_repo/releases" \
        | grep browser_download_url \
        | cut -d '"' -f 4 \
        | grep "$expectedFile" \
        | head -n 1)
    
    echo "$version_file_url"
    return 0
}


# args:
# install_root - $1
get_installed_version_info() {
    eval $invocation
    
    local install_root=$1
    local version_file=$(combine_paths "$install_root" "$local_version_file_relative_path")
    say_verbose "Local version file: $version_file"
    if [ ! -z "$version_file" ] | [ -r "$version_file" ]; then
        local version_info="$(cat $version_file)"
        echo "$version_info"
        return 0
    fi
    
    say_verbose "Local version file not found."
    return 0
}

# args:
# relative_or_absolute_path - $1
get_absolute_path() {
    eval $invocation
    
    local relative_or_absolute_path=$1
    echo $(cd $(dirname "$1") && pwd -P)/$(basename "$1")
    return 0
}

# args:
# zip_path - $1
# out_path - $2
extract_fake_package() {
    eval $invocation
    
    local zip_path=$1
    local out_path=$2
    local optionalPackPath=${3:-}
    
    local temp_out_path=$(mktemp -d $temporary_file_template)
    
    local failed=false
    #tar -xzf "$zip_path" -C "$temp_out_path" > /dev/null || failed=true
    unzip "$zip_path" -d "$temp_out_path" > /dev/null || failed=true
    
    local sourceDir="$temp_out_path"
    if [ ! -z "$optionalPackPath" ]; then
        if [ -d "$temp_out_path/$optionalPackPath" ]; then
            sourceDir="$temp_out_path/$optionalPackPath"
        fi
    fi
    
    cp -R "$sourceDir" "$out_path.temp" || failed=true
    mv "$out_path.temp" "$out_path"
    
    rm -rf $temp_out_path
    
    if [ "$failed" = true ]; then
        say_err "Extraction failed"
        return 1
    fi
}

# args:
# remote_path - $1
# [out_path] - $2 - stdout if not provided
download() {
    eval $invocation
    
    local remote_path=$1
    local out_path=${2:-}

    local failed=false
    if [ -z "$out_path" ]; then
        curl --fail -L -s $remote_path || failed=true
    else
        curl --fail -L -s -o $out_path $remote_path || failed=true
    fi
    
    if [ "$failed" = true ]; then
        say_err "Download failed"
        return 1
    fi
}

calculate_vars() {
    eval $invocation
    
    architecture=$(get_machine_architecture)
    osname=$(get_current_os_name)
    if [ -z "$specific_version" ]; then
        specific_version=$(get_latest_version)
        say_verbose "specific_version=$specific_version"
    fi
    download_link=$(construct_download_link $specific_version)
    say_verbose "download_link=$download_link"
    
    packages_download_link=$(construct_packages_download_link  $specific_version)
    say_verbose "packages_download_link=$packages_download_link"
    
    install_root=".fake/bin"
    say_verbose "install_root=$install_root"
    
    local postfix=""
    if beginswith win "$osname"; then
        postfix=".exe"
    fi
    local fake_package_path="$install_root/$specific_version/$osname-$architecture"
    fake_executable="$fake_package_path/Fake.netcore$postfix"
    
}

install_fake_raw() {
    eval $invocation
    
    if is_fake_package_installed $install_root $specific_version; then
        say "FAKE version $specific_version is already installed."
        return 0
    fi
    
    mkdir -p $install_root
    zip_path=$(mktemp $temporary_file_template)
    say_verbose "Zip path: $zip_path"
    
    say "Downloading $download_link"
    download "$download_link" $zip_path
    say_verbose "Downloaded file exists and readable? $(if [ -r $zip_path ]; then echo "yes"; else echo "no"; fi)"
    
    say "Extracting zip"
    mkdir -p "$install_root/$specific_version"
    rm -rf "$install_root/$specific_version/$osname-$architecture"
    extract_fake_package $zip_path "$install_root/$specific_version/$osname-$architecture" "$osname-$architecture"
    
    chmod +x "$fake_executable"
    
    return 0
}

install_fake_packages() {
    eval $invocation
    
    check_min_reqs
    calculate_vars
    
    check_pre_reqs
    
    packagesPath="$install_root/$specific_version/packages"
    if [ -d "$packagesPath" ]; then
        say "FAKE packages for version $specific_version already installed."
        return 0
    fi
    
    mkdir -p "$install_root/$specific_version"
    zip_path=$(mktemp $temporary_file_template)
    say_verbose "Zip path: $zip_path"
    
    say "Downloading $packages_download_link"
    download "$packages_download_link" $zip_path
    say_verbose "Downloaded file exists and readable? $(if [ -r $zip_path ]; then echo "yes"; else echo "no"; fi)"
    
    say "Extracting zip"
    mkdir -p "$install_root/$specific_version"
    extract_fake_package $zip_path "$install_root/$specific_version/packages"
    
    return 0
}

local_version_file_relative_path="/.version"
bin_folder_relative_path=""
temporary_file_template="${TMPDIR:-/tmp}/fake-dnc.XXXXXXXXX"

github_repo="${github_repo:-matthid/FAKE}"
verbose=${VERBOSE:-false}
specific_version=${FAKE_VERSION:-}


install_fake() {
    check_min_reqs
    calculate_vars
    
    check_pre_reqs
    install_fake_raw
}

beginswith() { case $2 in "$1"*) true;; *) false;; esac; }

exec_fake () {

    install_fake
    local failed=false
    local postfix=""
    if beginswith win "$osname"; then
        postfix=".exe"
    fi
    "$fake_executable" $* || failed=true

    if [ "$failed" = true ]; then
        say_err "Fake returned nonzero exit code"
        return 1
    fi
    return 0
}