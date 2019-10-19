namespace Fake.DotNet

/// Information about a dotnet tool
type DotNetTool =
  { /// Parameters as for DotNet.exec
    Options: DotNet.Options -> DotNet.Options
    /// If Some, override tool name in case CLI tool and local tool have different names, or substitute a reimplementation
    Tool: string option }
  static member Create() =
    { Options = id
      Tool = None }
type SimpleTool =
  { /// If Some, override tool name to substitute a reimplementation (Global tool) or default path (Framework tool)
    Tool: string option }
  static member Create() = { Tool = None }
/// Select which style of tool implementation
type ToolType =
  | DotNet of DotNetTool
  | Global of SimpleTool
  | Framework of SimpleTool
  member this.withWorkingDirectory dir =
    match this with
    | DotNet t ->
      DotNet
        { t with Options = t.Options >> (fun o -> { o with WorkingDirectory = dir }) }
    | _ -> this
  //member this.withTool tool =
  //                    match this with
  //                    | DotNet t -> DotNet { t with Tool = Some tool }
  //                    | _ -> this
  member this.Command (exePath: string) (toolName: string) =
    let defaultIfBlank x value =
      if x
         |> Option.isNone
         || x
            |> Option.get
            |> System.String.IsNullOrWhiteSpace
      then value
      else x |> Option.get
    match this with
    | Framework x ->
      match x.Tool with
      | None -> exePath
      | Some p -> p
    | Global x ->
      match x.Tool with
      | None -> toolName
      | Some p -> p
    | DotNet tool -> defaultIfBlank tool.Tool toolName

namespace Fake.Core

open Fake.DotNet

/// Some extensions for the `CreateProcess` module, opened automatically (use add `open Fake.Core`)
[<AutoOpen>]
module CreateProcessDotNetExt =
  /// Extensions to [`CreateProcess`](apidocs/v5/fake-core-createprocess.html).
  module CreateProcess =

    /// Ensures the command  is run with dotnet.
    ///
    /// ### Example
    ///
    ///     Command.RawCommand("localtool", Arguments.OfArgs ["arg1"; "arg2"])
    ///     |> CreateProcess.fromCommand
    ///     |> CreateProcess.withDotNet buildOptions // prepare to execute dotnet localtool.
    ///     |> Proc.runWithDotNet buildOptions
    ///     |> ignore
    let withDotNetTool (buildOptions: DotNet.Options -> DotNet.Options)
        (c: CreateProcess<_>) =
      c
      |> DotNet.prefixProcess buildOptions [c.Command.Executable]

    /// Ensures the command  is run with dotnet or with frameowrk/mono as appropriate.
    ///
    /// ### Example
    ///
    ///     Command.RawCommand("tool", Arguments.OfArgs ["arg1"; "arg2"])
    ///     |> CreateProcess.fromCommand
    ///     |> CreateProcess.withFrameworkOrDotNetTool toolType buildOptions // prepare to execute tool, mono tool, or dotnet localtool.
    ///     |> Proc.runWithDotNetOrFramework toolType buildOptions
    ///     |> ignore
    let withFrameworkOrDotNetTool (toolType: ToolType) (c: CreateProcess<_>) =
      c
      |> match toolType with
         | ToolType.Framework _ -> CreateProcess.withFramework
         | ToolType.DotNet t -> withDotNetTool t.Options
         | _ -> id
