namespace Fake.Core

/// <summary>
/// Some extensions for the <c>CreateProcess</c> module, opened automatically (use add <c>open Fake.Core</c>)
/// </summary>
[<AutoOpen>]
module CreateProcessExt =
    /// <summary>
    /// Extensions to <a href="/reference/fake-core-createprocess.html"><c>CreateProcess</c></a>.
    /// </summary>
    module CreateProcess =
        
        /// <summary>
        /// Ensures the executable is run with the full framework. On non-windows platforms that means running the tool
        /// by invoking <c>mono</c>.
        /// </summary>
        /// 
        /// <example>
        /// <code lang="fsharp">
        /// Command.RawCommand("file.exe", Arguments.OfArgs ["arg1"; "arg2"])
        ///     |&gt; CreateProcess.fromCommand
        ///     |&gt; CreateProcess.withFramework // start with mono if needed.
        ///     |&gt; Proc.run
        ///     |&gt; ignore
        /// </code>
        /// </example>   
        let withFramework (c:CreateProcess<_>) =
            match Environment.isWindows, c.Command, Process.monoPath with
            | false, RawCommand(file, args), Some monoPath when file.ToLowerInvariant().EndsWith(".exe") ->
                { c with
                    InternalCommand = RawCommand(monoPath, Arguments.withPrefix ["--debug"; file] args) }
            | false, RawCommand(file, _args), _ when file.ToLowerInvariant().EndsWith(".exe") ->
                failwithf "trying to start a .NET process on a non-windows platform, but mono could not be found. Try to set the MONO environment variable or add mono to the PATH."
            | _ -> c
