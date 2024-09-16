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
// DotNetCliToolReference contains a file "dotnet-<name>.dll" and then can be called via "dotnet <name> --myargument"

// For SCD and global tools we can just use "ToolPath"
// for mono prefix `CreateProcess.withFramework` is already available
// So the extensions in this file adds similar extensions for local tools and FDDs
// And some extensions to combine some of the above.


type DotNetFDDOptions =
    internal
        {
            /// Parameters as for the dotnet call
            Options: DotNet.Options -> DotNet.Options
        }

    static member Create(install) = { Options = install }
    static member Create() = DotNetFDDOptions.Create(id)

/// <summary>
/// Information about a dotnet tool
/// </summary>
type DotNetLocalTool =
    internal
        {
            /// Parameters as for the dotnet call
            Options: DotNet.Options -> DotNet.Options

            /// Currently ignored
            PackageName: string option

            /// The command name of the tool (the first argument of 'dotnet'). For example `"fake"` for `dotnet fake`.
            /// By default we usually fallback to the executable name of ToolPath without file extension.
            ToolCommandName: string option
        }

    static member Create(install) =
        { Options = install
          PackageName = None
          ToolCommandName = None }

    static member Create() = DotNetLocalTool.Create id

    member x.WithDefaultToolCommandName toolCommandName =
        { x with
            ToolCommandName =
                match x.ToolCommandName with
                | None -> Some toolCommandName
                | _ -> x.ToolCommandName }

/// <summary>
/// Describes which kind of application ToolPath references
/// </summary>
[<RequireQualifiedAccess>]
type ToolType =
    internal
    /// The application is a pre .NET 5 full framework application, ToolPath is combined with
    /// <c>CreateProcess.withFramework</c>. Which prefixes the command with <c>mono</c> on non-windows platforms
    | FullFramework
    /// The application is a framework dependent application, prefixes the app with <c>dotnet</c> and allows ToolPath
    /// to be the path to the dll.
    | FrameworkDependentDeployment of dotnetOptions: DotNetFDDOptions // dotnet ToolPath (can be a dll)
    /// The application is a self contained application, does not prefix anything, expects ToolPath to be the
    /// platform dependent path to the application.
    | SelfContainedDeployment // just ToolPath
    /// The application is a global dotnet cli tool, does not prefix anything, expects ToolPath to be the
    /// platform dependent path to the application.
    | GlobalTool // just ToolPath
    /// local dotnet tool, uses <c>dotnet <toolname></c>
    | LocalTool of tool: DotNetLocalTool // ToolPath is ignored or stripped
    /// CLIToolReference, uses <c>dotnet <toolname></c>
    | CLIToolReference of tool: DotNetLocalTool // ToolPath is ignored or stripped

    static member Create() = FullFramework
    static member CreateFullFramework() = FullFramework

    static member CreateFrameworkDependentDeployment() =
        FrameworkDependentDeployment(DotNetFDDOptions.Create())

    static member CreateFrameworkDependentDeployment(install) =
        FrameworkDependentDeployment(DotNetFDDOptions.Create(install))

    static member CreateGlobalTool() = GlobalTool
    static member CreateLocalTool() = LocalTool(DotNetLocalTool.Create())

    static member CreateLocalTool(install) =
        LocalTool(DotNetLocalTool.Create(install))

    static member CreateCLIToolReference() =
        CLIToolReference(DotNetLocalTool.Create())

    static member CreateCLIToolReference(install) =
        CLIToolReference(DotNetLocalTool.Create(install))

    member x.WithDefaultToolCommandName toolCommandName =
        match x with
        | LocalTool tool -> LocalTool(tool.WithDefaultToolCommandName toolCommandName)
        | CLIToolReference tool -> CLIToolReference(tool.WithDefaultToolCommandName toolCommandName)
        | _ -> x

    member x.WithDefaultCliToolReferenceToolName toolCommandName =
        match x with
        | CLIToolReference tool -> CLIToolReference(tool.WithDefaultToolCommandName toolCommandName)
        | _ -> x

    member x.WithDotNetOptions setParams =
        match x with
        | CLIToolReference tool -> CLIToolReference { tool with Options = setParams }
        | LocalTool tool -> LocalTool { tool with Options = setParams }
        | FrameworkDependentDeployment opts -> FrameworkDependentDeployment { opts with Options = setParams }
        | _ -> x


module ToolType =
    let withDefaultToolCommandName t (x: ToolType) = x.WithDefaultToolCommandName t
    let withDefaultCliToolReferenceToolName t (x: ToolType) = x.WithDefaultCliToolReferenceToolName t
    let withDotNetOptions t (x: ToolType) = x.WithDotNetOptions t

namespace Fake.Core

open Fake.DotNet

/// <summary>
/// Some extensions for the <c>CreateProcess</c> module, opened automatically (use add <c>open Fake.Core</c>)
/// </summary>
[<AutoOpen>]
module CreateProcessDotNetExt =
    /// <summary>
    /// Extensions to <a href="/reference/fake-core-createprocess.html"><c>CreateProcess</c></a>.
    /// </summary>
    module CreateProcess =

        /// <summary>
        /// Ensures the command  is run with dotnet or with framework/mono as appropriate.
        /// </summary>
        ///
        /// <example>
        /// <code lang="fsharp">
        /// Command.RawCommand("tool", Arguments.OfArgs ["arg1"; "arg2"])
        ///     |> CreateProcess.fromCommand
        ///     |> CreateProcess.withToolType toolType // prepare to execute tool, mono tool, or dotnet localtool.
        ///     |> Proc.run
        ///     |> ignore
        /// </code>
        /// </example>
        let withToolType (toolType: ToolType) (c: CreateProcess<_>) =
            match toolType with
            | ToolType.FullFramework -> c |> CreateProcess.withFramework
            | ToolType.FrameworkDependentDeployment dotnetOptions -> // dotnet ToolPath (can be a dll)
                c |> DotNet.prefixProcess dotnetOptions.Options [ c.Command.Executable ]
            | ToolType.SelfContainedDeployment // just ToolPath
            | ToolType.GlobalTool -> // just ToolPath
                c
            //local dotnet tool
            | ToolType.LocalTool tool // ToolPath is ignored or stripped
            | ToolType.CLIToolReference tool -> // ToolPath is ignored or stripped
                let toolArgs =
                    match tool.ToolCommandName with
                    | Some t -> [ t ]
                    | None -> [ System.IO.Path.GetFileNameWithoutExtension(c.Command.Executable) ]

                c |> DotNet.prefixProcess tool.Options toolArgs
