_fake_index_of_cmdline ()
{
	local word item index=1
	while [ $index -lt $cword ]; do
		word="${words[index]}"
		for item in $1; do
			if [ "$item" = "$word" ]; then
				echo "$index"
				return
			fi
		done
		((index++))
	done
}

_fake_complete_fake() {
    local subcommands=(
		build
		run
	)

    local arguments=(
        --help
        -h
        --version
        --verbose
        -v
        -vv
        --silent
        -s
    )

	case "$cur" in
		-*)
			COMPREPLY=( $( compgen -W "${arguments[*]}" -- "$cur" ) )
			;;
		*)
			COMPREPLY=( $( compgen -W "${subcommands[*]}" -- "$cur" ) )
			;;
	esac
}


_fake_get_targets() {
		local start="The following targets are available:"
		local end="Performance:"
		fake 2>/dev/null "$@" | sed -n "/$start/,/$end/{//b;p}"
}

_fake_complete_build() {
    local subcommands=(
		target
	)
      
    local arguments=(
        --help
        -h
        --debug
        -d
        --nocache
        -n
        --partial-restore
        -p
        --fsiargs
        --target
		-t
        --list
		--script
		-f
        --single-target
        -s
        --parallel
        -p
        --environment-variable
        -e
    )

    case "$prev" in
		--script|-f)
			# default bash filename completion
			return
			;;
		target|--target|-t)

			local index="$(_fake_index_of_cmdline "--script -f")"
			if [ -n "$index" ]; then
					local fileName="${words[(($index+1))]}"
					case $fileName in
						*.fsx)
							local targets=$(_fake_get_targets build --script "$fileName" --list)
							COMPREPLY=( $( compgen -W "${targets}" -- "$cur"  ) )
							;;							
					esac	
					return
			fi
			local targets=$(_fake_get_targets build --list)
			COMPREPLY=( $( compgen -W "${targets}" -- "$cur"  ) )
			return
			;;
	esac	
	# target was specified but not as $prev, so we do not complete anything
	if [ -n "$(_fake_index_of_cmdline "target")" ]; then
		return
	fi
	case "$cur" in
		-*)
			COMPREPLY=( $( compgen -W "${arguments[*]}" -- "$cur" ) )
			;;
		*)
			COMPREPLY=( $( compgen -W "${subcommands[*]}" -- "$cur" ) )
			;;
	esac
}



_fake_complete_run() {
    local subcommands=(
		target
	)
      
    local arguments=(
        --help
        -h
        --debug
        -d
        --nocache
        -n
        --partial-restore
        -p
        --fsiargs
        --target
		-t
        --list
        --single-target
        -s
        --parallel
        -p
        --environment-variable
        -e
    )
	
    case "$prev" in
		run)
			# default bash filename completion
			return
			;;

		target|--target|-t)
			local index="$(_fake_index_of_cmdline "run")"
			if [ -n "$index" ]; then
					local fileName="${words[(($index+1))]}"
					case $fileName in
						*.fsx)
							local targets=$(_fake_get_targets run "$fileName" --list)
							COMPREPLY=( $( compgen -W "${targets}" -- "$cur"  ) )
							;;							
					esac	
			fi
			return
			;;

	esac	
	# target was specified but not as $prev, so we do not complete anything
	if [ -n "$(_fake_index_of_cmdline "target")" ]; then
		return
	fi
	case "$cur" in
		-*)
			COMPREPLY=( $( compgen -W "${arguments[*]}" -- "$cur" ) )
			;;
		*)
			COMPREPLY=( $( compgen -W "${subcommands[*]}" -- "$cur" ) )
			;;
	esac
}






_complete_fake() {
	

	# COMPREPLY=()
	local cur prev words cword
	_get_comp_words_by_ref -n : cur prev words cword

	local command='fake'
	local index=1

	while [ $index -lt $cword ]; do
		case "${words[$index]}" in
			-*)
				;;
			*)
				command="${words[$index]}"
				break
				;;
		esac
		(( index++ ))
	done

	local completions_func=_fake_complete_${command}
	declare -F $completions_func >/dev/null && $completions_func

	return 0
}


complete -o bashdefault -o default -F _complete_fake fake fake.exe
