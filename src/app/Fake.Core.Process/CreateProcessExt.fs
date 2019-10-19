namespace Fake.Core

/// Some extensions for the `CreateProcess` module, opened automatically (use add `open Fake.Core`)
[<AutoOpen>]
module CreateProcessExt =
    /// Extensions to [`CreateProcess`](apidocs/v5/fake-core-createprocess.html).
    module CreateProcess =
        /// Ensures the executable is run with the full framework. On non-windows platforms that means running the tool by invoking 'mono'.
        /// 
        /// ### Example
        /// 
        ///     Command.RawCommand("file.exe", Arguments.OfArgs ["arg1"; "arg2"])
        ///     |> CreateProcess.fromCommand
        ///     |> CreateProcess.withFramework // start with mono if needed.
        ///     |> Proc.run
        ///     |> ignore
        let withFramework (c:CreateProcess<_>) =
            match Environment.isWindows, c.Command, Process.monoPath with
            | false, RawCommand(file, args), Some monoPath when file.ToLowerInvariant().EndsWith(".exe") ->
                { c with
                    InternalCommand = RawCommand(monoPath, Arguments.withPrefix ["--debug"; file] args) }
            | false, RawCommand(file, args), _ when file.ToLowerInvariant().EndsWith(".exe") ->
                failwithf "trying to start a .NET process on a non-windows platform, but mono could not be found. Try to set the MONO environment variable or add mono to the PATH."
            | _ -> c