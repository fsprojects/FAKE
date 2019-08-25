namespace Fake.Core

module internal CmdLineParsing =
    let escapeCommandLineForShell (cmdLine:string) =
        sprintf "'%s'" (cmdLine.Replace("'", "'\\''"))
    let windowsArgvToCommandLine shorten args =
        if isNull args then
            invalidArg "args" "'args' cannot be null"
        
        let escapeBackslashes (sb:System.Text.StringBuilder) (s:string) (lastSearchIndex:int) =
            // Backslashes must be escaped if and only if they precede a double quote.
            [ lastSearchIndex .. -1 .. 0]
            |> Seq.takeWhile (fun i -> s.[i] = '\\')
            //|> Seq.map (fun c -> )
            //|> fun c -> Seq.replicate c '\\'
            |> Seq.iter (fun c -> sb.Append '\\' |> ignore)
        
        let sb = new System.Text.StringBuilder()
        for (s:string) in args do
            if isNull s then
                invalidArg "args" "'args' cannot contain null"
            if shorten && s.Length > 0 && s.IndexOfAny([|' '; '\"'; '\\'; '\t'|]) < 0 then
                sb.Append s |> ignore
                sb.Append " " |> ignore
            else
                sb.Append('"') |> ignore
                // Escape double quotes (") and backslashes (\).
                let mutable searchIndex = 0
                
                // Put this test first to support zero length strings.
                let mutable quoteIndex = 0
                while searchIndex < s.Length && quoteIndex >= 0 do

                    quoteIndex <- s.IndexOf('"', searchIndex)
                    if quoteIndex >= 0 then
                        sb.Append(s, searchIndex, quoteIndex - searchIndex) |> ignore
                        escapeBackslashes sb s (quoteIndex - 1)
                        sb.Append('\\') |> ignore
                        sb.Append('"') |> ignore
                        searchIndex <- quoteIndex + 1
                
                sb.Append(s, searchIndex, s.Length - searchIndex) |> ignore
                escapeBackslashes sb s (s.Length - 1)
                sb.Append(@""" ") |> ignore
        
        sb.ToString(0, System.Math.Max(0, sb.Length - 1))

    let windowsCommandLineToArgv (arguments:string) =
        if arguments.Contains "\"\"\"" then
            invalidArg "arguments" (sprintf "tripple quotes are not allowed in the command line ('%s') as they behave different across programs, see https://github.com/vbfox/FoxSharp/issues/1 to escape a quote use backslash and the rules from https://docs.microsoft.com/en-US/cpp/cpp/parsing-cpp-command-line-arguments?view=vs-2017." arguments)

        // https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L443-L522
        let currentArgument = new System.Text.StringBuilder()
        let mutable inQuotes = false
        let mutable atLeastEmpty = false
        let results = System.Collections.Generic.List<_>()

        // Iterate through all of the characters in the argument string.
        let mutable i = 0
        while i < arguments.Length do
            // From the current position, iterate through contiguous backslashes.
            let mutable backslashCount = 0
            while i < arguments.Length && arguments.[i] = '\\' do
                i <- i + 1
                backslashCount <- backslashCount + 1
            if backslashCount > 0 then
                if i >= arguments.Length || arguments.[i] <> '"' then
                    // Backslashes not followed by a double quote:
                    // they should all be treated as literal backslashes.
                    currentArgument.Append('\\', backslashCount) |> ignore
                    i <- i - 1
                else
                    // Backslashes followed by a double quote:
                    // - Output a literal slash for each complete pair of slashes
                    // - If one remains, use it to make the subsequent quote a literal.
                    currentArgument.Append('\\', backslashCount / 2) |> ignore
                    if backslashCount % 2 = 0 then
                        i <- i - 1
                    else
                        currentArgument.Append('"') |> ignore
            else
                let c = arguments.[i]
                
                match c with
                // If this is a double quote, track whether we're inside of quotes or not.
                // Anything within quotes will be treated as a single argument, even if
                // it contains spaces.
                | '"' ->
                    atLeastEmpty <- true
                    inQuotes <- not inQuotes
                // If this is a space/tab and we're not in quotes, we're done with the current
                // argument, and if we've built up any characters in the current argument,
                // it should be added to the results and then reset for the next one.
                | ' ' | '\t' when not inQuotes ->
                    if currentArgument.Length > 0 || atLeastEmpty then
                        atLeastEmpty <- false
                        results.Add(currentArgument.ToString())
                        currentArgument.Clear() |> ignore
                // Nothing special; add the character to the current argument.
                | _ ->
                    currentArgument.Append(c) |> ignore
            i <- i + 1

        // If we reach the end of the string and we still have anything in our current
        // argument buffer, treat it as an argument to be added to the results.
        if currentArgument.Length > 0 || atLeastEmpty then
            results.Add(currentArgument.ToString())

        results.ToArray()

#if !FAKE_RUNTIME
    let toProcessStartInfo args =
        let cmd = windowsArgvToCommandLine true args
        if Environment.isMono && Environment.isLinux then
            // See https://bugzilla.xamarin.com/show_bug.cgi?id=19296
            cmd.Replace("\\$", "\\\\$").Replace("\\`", "\\\\`")
        else cmd

type FilePath = string

/// Helper functions for proper command line parsing
module Args =
    /// Convert the given argument list to a conforming windows command line string, escapes parameter in quotes if needed (currently always but this might change).
    let toWindowsCommandLine args = CmdLineParsing.windowsArgvToCommandLine true args
    /// Escape the given argument list according to a unix shell (bash)
    let toLinuxShellCommandLine args =
        System.String.Join(" ", args |> Seq.map CmdLineParsing.escapeCommandLineForShell)
    /// Read a windows command line string into its arguments
    let fromWindowsCommandLine cmd = CmdLineParsing.windowsCommandLineToArgv cmd

/// Represents a list of arguments
type Arguments = 
    internal { Args : string array; Original : string option }
    static member Empty = { Args = [||]; Original = None }
    /// See https://msdn.microsoft.com/en-us/library/17w5ykft.aspx
    static member OfWindowsCommandLine cmd =
        { Args = Args.fromWindowsCommandLine cmd; Original = Some cmd }

    /// This is the reverse of https://msdn.microsoft.com/en-us/library/17w5ykft.aspx
    member x.ToWindowsCommandLine = Args.toWindowsCommandLine x.Args// |> CmdLine.toString
    /// Escape the given argument list according to a unix shell (bash)
    member x.ToLinuxShellCommandLine = Args.toLinuxShellCommandLine x.Args// |> CmdLine.toList |> Args.toLinuxShellCommandLine

    /// Create a new arguments object from the given list of arguments
    static member OfArgs (args:string seq) = { Args = args |> Seq.toArray; Original = None }

    /// Create a new arguments object from a given startinfo-conforming-escaped command line string.
    /// Same as `OfWindowsCommandLine`.
    static member OfStartInfo cmd = Arguments.OfWindowsCommandLine cmd

    /// Create a new command line string which can be used in a ProcessStartInfo object.
    /// If given, returns the exact input of `OfWindowsCommandLine` otherwise `ToWindowsCommandLine` (with some special code for `mono`) is used.
    member x.ToStartInfo =
        match x.Original with
        | Some orig -> orig
        | None ->
            CmdLineParsing.toProcessStartInfo x.Args

/// Module for working with an `Arguments` instance
module Arguments =
    /// This is the reverse of https://msdn.microsoft.com/en-us/library/17w5ykft.aspx
    let toWindowsCommandLine (a:Arguments) = a.ToWindowsCommandLine
    /// Escape the given argument list according to a unix shell (bash)
    let toLinuxShellCommandLine (a:Arguments) = a.ToLinuxShellCommandLine
    /// Create a new command line string which can be used in a ProcessStartInfo object.
    /// If given, returns the exact input of `OfWindowsCommandLine` otherwise `ToWindowsCommandLine` (with some special code for `mono`) is used.
    let toStartInfo (a:Arguments) = a.ToStartInfo

    /// Append the given arguments before all current arguments 
    let withPrefix (s:string seq) (a:Arguments) =
        Arguments.OfArgs(Seq.append s a.Args)
    /// Append all arguments after the current arguments    
    let append s (a:Arguments) =
        Arguments.OfArgs(Seq.append a.Args s)

    /// Appends the given raw argument to the command line, you can not use other methods for this to work
    /// This method is only required if you NEED quotes WITHIN your argument (some old Microsoft Tools).
    /// "raw" methods are not compatible with non-raw methods.
    let appendRaw s (a:Arguments) =
        let cmd = a.ToStartInfo
        let newCmd = if cmd.Length = 0 then s else cmd + " " + s
        { Args = Array.append a.Args [|s|]; Original = Some newCmd }

    /// Appends the given raw argument to the command line, you can not use other methods for this to work
    /// This allows unusal quoting with the given prefix, like /k:"myarg" ("/k:" would be the argPrefix)
    /// This method is only required if you NEED quotes WITHIN your argument (some old Microsoft Tools).
    /// "raw" methods are not compatible with non-raw methods.
    let appendRawEscaped (argPrefix:string) paramValue (a:Arguments) =
        if argPrefix.IndexOfAny([|' '; '\"'; '\\'; '\t'|]) >= 0 then
            invalidArg "argPrefix" "Argument prefix cannot contain special characters"
        a |> appendRaw (sprintf "%s%s" argPrefix (CmdLineParsing.windowsArgvToCommandLine false [paramValue]))
        
    /// Append an argument prefixed by another if the value is Some.
    /// This method is only required if you NEED quotes WITHIN your argument (some old Microsoft Tools).
    /// "raw" methods are not compatible with non-raw methods.
    let appendRawEscapedIf b (argPrefix:string) (paramValue:string) (a:Arguments) =
        if b then
             a |> appendRawEscaped argPrefix paramValue
        else a
    /// Append an argument prefixed by another if the value is Some.
    /// This method is only required if you NEED quotes WITHIN your argument (some old Microsoft Tools).
    /// "raw" methods are not compatible with non-raw methods.
    let appendRawEscapedOption (argPrefix:string) (paramValue:string option) (a:Arguments) =
        match paramValue with
        | Some x -> a |> appendRawEscaped argPrefix x
        | None -> a
    /// Append an argument prefixed by another if the value is Some.
    /// This method is only required if you NEED quotes WITHIN your argument (some old Microsoft Tools).
    /// "raw" methods are not compatible with non-raw methods.
    let appendRawEscapedNotEmpty (argPrefix:string) (paramValue:string) (a:Arguments) =
        appendRawEscapedIf (String.isNullOrEmpty paramValue |> not) argPrefix paramValue a

    /// Append an argument prefixed by another if the value is Some.
    let appendOption (paramName:string) (paramValue:string option) (a:Arguments) =
        match paramValue with
        | Some x -> a |> append [ paramName; x ]
        | None -> a

    /// Append an argument to a command line if a condition is true.
    let appendIf value paramName (a:Arguments) =
        if value then a |> append [ paramName ]
        else a

    /// Append an argument prefixed by another if the value is not null or empty
    let appendNotEmpty paramName paramValue (a:Arguments) =
        if String.isNullOrEmpty paramValue then a
        else a |> append [ paramName; paramValue ]

    /// Convert the arguments instance to a string list
    let toList (a:Arguments) =
        a.Args |> Array.toList

    /// Convert the arguments instance to a string array
    let toArray (a:Arguments) =
        a.Args |> Array.toList |> Seq.toArray

    /// Create a arguments instance from a list.
    let ofList (a:string list) =
        { Args = a |> Seq.toArray; Original = None }

#endif   
