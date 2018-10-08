namespace Fake.Core

module internal CmdLineParsing =
    let escapeCommandLineForShell (cmdLine:string) =
        sprintf "'%s'" (cmdLine.Replace("'", "'\\''"))
    let windowsArgvToCommandLine args =
        let escapeBackslashes (sb:System.Text.StringBuilder) (s:string) (lastSearchIndex:int) =
            // Backslashes must be escaped if and only if they precede a double quote.
            [ lastSearchIndex .. -1 .. 0]
            |> Seq.takeWhile (fun i -> s.[i] = '\\')
            //|> Seq.map (fun c -> )
            //|> fun c -> Seq.replicate c '\\'
            |> Seq.iter (fun c -> sb.Append '\\' |> ignore)
        
        let sb = new System.Text.StringBuilder()
        for (s:string) in args do
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
                if i > arguments.Length || arguments.[i] <> '"' then
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

    let toProcessStartInfo args =
        let cmd = windowsArgvToCommandLine args
        if Environment.isMono && Environment.isLinux then
            // See https://bugzilla.xamarin.com/show_bug.cgi?id=19296
            cmd.Replace("\\$", "\\\\$").Replace("\\`", "\\\\`")
        else cmd

type FilePath = string

/// Helper functions for proper command line parsing
module Args =
    /// Convert the given argument list to a conforming windows command line string, escapes parameter in quotes if needed (currently always but this might change).
    let toWindowsCommandLine args = CmdLineParsing.windowsArgvToCommandLine args
    /// Escape the given argument list according to a unix shell (bash)
    let toLinuxShellCommandLine args =
        System.String.Join(" ", args |> Seq.map CmdLineParsing.escapeCommandLineForShell)
    /// Read a windows command line string into its arguments
    let fromWindowsCommandLine cmd = CmdLineParsing.windowsCommandLineToArgv cmd

/// Represents a list of arguments
type Arguments = 
    internal { Args : string array }
    static member Empty = { Args = [||] }
    /// See https://msdn.microsoft.com/en-us/library/17w5ykft.aspx
    static member OfWindowsCommandLine cmd =
        { Args = Args.fromWindowsCommandLine cmd }

    /// This is the reverse of https://msdn.microsoft.com/en-us/library/17w5ykft.aspx
    member x.ToWindowsCommandLine = Args.toWindowsCommandLine x.Args
    member x.ToLinuxShellCommandLine = Args.toLinuxShellCommandLine x.Args

    /// Create a new arguments object from the given list of arguments
    static member OfArgs (args:string seq) = { Args = args |> Seq.toArray }
    /// Create a new arguments object from a given startinfo-conforming-escaped command line string.
    static member OfStartInfo cmd = Arguments.OfWindowsCommandLine cmd
    /// Create a new command line string which can be used in a ProcessStartInfo object.
    member x.ToStartInfo = CmdLineParsing.toProcessStartInfo x.Args

module Arguments =
    let withPrefix s (a:Arguments) =
        Arguments.OfArgs(Seq.append s a.Args)
    let append s (a:Arguments) =
        Arguments.OfArgs(Seq.append a.Args s)