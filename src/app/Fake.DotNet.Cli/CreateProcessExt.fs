namespace Fake.Core

open Fake.DotNet

/// Some extensions for the `CreateProcess` module, opened automatically (use add `open Fake.Core`)
[<AutoOpen>]
module CreateProcessExt =
    /// Extensions to [`CreateProcess`](apidocs/v5/fake-core-createprocess.html).
    module CreateProcess =
        /// Information about a dotnet tool
        type DotNetTool =
            {
            Options : DotNet.Options -> DotNet.Options
            Tool : string option
            }
        with static member Create() = {Options = id; Tool = None}

        /// Select which style of tool implementation
        type ToolType =
            | DotNet of DotNetTool
            | Global
            | Framework of string option

        /// Ensures the command  is run with dotnet.
        ///
        /// ### Example
        ///
        ///     Command.RawCommand("localtool", Arguments.OfArgs ["arg1"; "arg2"])
        ///     |> CreateProcess.fromCommand
        ///     |> CreateProcess.withDotNet buildOptions // execute dotnet localtool.
        ///     |> Proc.run
        ///     |> ignore
        let withDotNet (buildOptions: DotNet.Options -> DotNet.Options) (c:CreateProcess<_>) =
            c // TODO
            //match Environment.isWindows, c.Command, Process.monoPath with
            //| false, RawCommand(file, args), Some monoPath when file.ToLowerInvariant().EndsWith(".exe") ->
            //    { c with
            //        Command = RawCommand(monoPath, Arguments.withPrefix ["--debug"; file] args) }
            //| false, RawCommand(file, args), _ when file.ToLowerInvariant().EndsWith(".exe") ->
            //    failwithf "trying to start a .NET process on a non-windows platform, but mono could not be found. Try to set the MONO environment variable or add mono to the PATH."
            //| _ -> c
