namespace Fake.DotNet

// Here are the different ways to execute a .NET applications:
// Assume we have a tool called "mytool" we want to call it with arguments "--myargument"
// Full framework on windows: "mytool.exe --myargument" <- Windows handles finding the correct framework
// Full framework on unixes : "mono mytool.exe --myargument" <- mono needs to be installed to run a full framework executable
// .NET Core application (netcoreapp): "dotnet mytool.dll --myargument" <- This is for Framework Dependent Deployments (FDD)
// .NET Core SCD windows (netcoreapp): "mytool.exe --myargument" <- This is for self contained deployments
// .NET Core SCD unixes  (netcoreapp): "mytool --myargument" <- This is for self contained deployments
// .NET global tools (installed via "dotnet tool install -g "MyToolNuGetPackageName"): "<ToolCommandName>.exe --myargument" (or without ".exe" on non-windows)
// .NET global tools (installed via "dotnet tool install --tool-path .tools "MyToolNuGetPackageName"): like -g global tools (but with '.tools/<ToolCommandName>.exe')
// .NET local tools (installed via "dotnet new tool-manifest && dotnet tool install "MyToolNuGetPackageName"): "dotnet <ToolCommandName> --myargument"

// For SCD and global tools we can just use "ToolPath"
// for mono prefix `CreateProcess.withFramework` is already available
// So the extensions in this file adds similar extensions for local tools and FDDs
// And some extensions to combine some of the above.
type DotNetExecution =
    | FrameworkDependentDeployment
    | SelfContainedDeployment
    | FullFramework
    | GlobalTool
    | LocalTool


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

    /// Ensures the command  is run with dotnet or with framework/mono as appropriate.
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
