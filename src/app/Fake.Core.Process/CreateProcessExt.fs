namespace Fake.Core

[<AutoOpen>]
module CreateProcessExt =
    module CreateProcess =
        /// Ensures the executable is run with the full framework. On non-windows platforms that means running the tool by invoking 'mono'.
        let withFramework (c:CreateProcess<_>) =
            match Environment.isWindows, c.Command, Process.monoPath with
            | false, RawCommand(file, args), Some monoPath when file.ToLowerInvariant().EndsWith(".exe") ->
                { c with
                    Command = RawCommand(monoPath, Arguments.withPrefix ["--debug"; file] args) }
            | false, RawCommand(file, args), _ when file.ToLowerInvariant().EndsWith(".exe") ->
                failwithf "trying to start a .NET process on a non-windows platform, but mono could not be found. Try to set the MONO environment variable or add mono to the PATH."
            | _ -> c