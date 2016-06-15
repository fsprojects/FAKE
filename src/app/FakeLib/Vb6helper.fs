/// Enables building of Visual Basic 6 projects
/// Also includes a do-it-all function that will embed interop
/// side-by-side manifest to executables from Vb6 using
/// functions from the Side-by-side helper module
module Fake.Vb6Helper

open Fake
open Fake.SxsHelper
open System
open System.IO

/// Parameters for running a VB6 build
type Vb6BuildParams = 
    { 
        /// Path to the VB6 executable
        Toolpath:string; 
        
        /// Directory to put generated binaries 
        Outdir:string; 

        /// Directory to put logs and other temporary files
        /// created during the build process
        Logdir:string; 
      
        /// Maximum amount of time the entire build is allowed to take
        Timeout:System.TimeSpan }

type private Vb6BuildJob = 
    { Path:string; 
      Name:string; 
      Started:System.DateTime; 
      Finished:System.DateTime;
      IsStarted:bool; 
      StartSucceeded:bool
      IsFinished:bool;
      LogFile:string; 
      IsSuccessful:bool; 
      ErrorMessage:string }

type private Vb6BuildResult =
    | Success
    | Pending
    | Failed of string

/// Represents the version of a VB6 project
/// `ToString ()` will return a Maj.Min.Rev.Patch version string
type Vb6Version = {MajorVer:int; MinorVer:int; RevisionVer:int; AutoIncrementVer:int;}
                    override x.ToString () = sprintf "%i.%i.%i.%i" x.MajorVer x.MinorVer x.RevisionVer x.AutoIncrementVer

/// Represents the version of a VB6 reference
/// References from VB6 projects only care about Major.Minor versions
type Vb6ReferenceVersion = 
    {
        Major: int
        Minor: int
    }

/// Represents a VB6 Reference
type Vb6Reference =
    {
        Guid : Guid
        Version: Vb6ReferenceVersion
    }

/// Represents a VB6 project
type Vb6Project = 
    {
        /// Path to the Propject file representing 
        /// Representing this project
        ProjectFile:string
        /// Name of binary that will
        /// be generated from this project
        BinaryName:string 
        
        /// Version of the project
        /// in Major.Minor.Revision.Patch format
        Version: string 
        
        /// All references and components used
        /// in this VBV6 project
        References: Vb6Reference seq
    }

let private defaultVb6BuildParams = {
        Toolpath = ProgramFilesX86 + @"\Microsoft Visual Studio\VB98\VB6.exe"
        Outdir = "bin"
        Logdir = "temp"
        Timeout = System.TimeSpan.FromMinutes 10.0
     } 

/// Helper methods for working with Vb6 project files 
let private readProjectFileLines p = File.ReadAllLines(p, System.Text.Encoding.GetEncoding("ISO-8859-1"))
let private writeProjectFileLines p (l:string seq) = File.WriteAllLines(p,l,System.Text.Encoding.GetEncoding("ISO-8859-1"))
let private toChars (s:string) = s.ToCharArray () |> Seq.ofArray
let private getValueBetween startChar endChar (line:string) = 
        line
        |> toChars
        |> Seq.skipWhile (fun c -> c <> startChar)
        |> Seq.skip 1
        |> Seq.takeWhile (fun c -> c <> endChar)
        |> String.Concat
let private getReferenceLineParts (line:string) = 
            line.Split([|"#"|], StringSplitOptions.RemoveEmptyEntries)
let private createReferenceLine (lineParts:string[]) =
            String.Join("#", lineParts)
let private getReferenceLineGuid (lineParts:string[]) = 
            lineParts.[0] |> getValueBetween '{' '}' |> Guid.Parse
let private referenceLineFilter (l:string) = l.StartsWith("Reference") || l.StartsWith("Object")


/// Executes a VB6 command line make on all provided VB6 projects
///
/// Builds will be executed in paralell
///
/// ## Parameters
///  - `getConfig` - function to modify the build params record from default values
///  - `vb6Projects`- `Seq` of paths to `.vbp` files to build
let public Vb6Make (getConfig: Vb6BuildParams->Vb6BuildParams) (vb6Projects: string seq) =
     let config = defaultVb6BuildParams |> getConfig
     traceStartTask "Vb6Make" (sprintf "Building %i projects" (vb6Projects |> Seq.length))
     let jobs = vb6Projects 
                |> List.ofSeq 
                |>  List.map (fun p -> 
                                let name = System.IO.Path.GetFileNameWithoutExtension p
                                {
                                  Path = p
                                  Name = name
                                  Started = System.DateTime.Now
                                  Finished = System.DateTime.Now
                                  IsFinished = false
                                  IsStarted = false
                                  StartSucceeded = false
                                  IsSuccessful = false
                                  ErrorMessage = ""
                                  LogFile = config.Logdir @@ (name + ".log")
                                 })

     let startBuildAsync j =
        async {
            let! startResult = asyncShellExec {defaultParams  with 
                                                Program = config.Toolpath
                                                WorkingDirectory = config.Logdir
                                                CommandLine = (sprintf "/m \"%s\" /out \"%s\" /outdir \"%s\"" j.Path j.LogFile config.Outdir)}

            if startResult <> 0 then 
                return {j with IsStarted = true; Started = System.DateTime.Now; ErrorMessage = "StartupFailed";}
            else
                return {j with IsStarted = true; Started = System.DateTime.Now; StartSucceeded = true}
        }

     let getLogfileStatusAsync j =
        async {
            let! exists = async {return System.IO.File.Exists(j.LogFile)}
            match exists with
            | false -> return Pending
            | true -> let! content = async { return System.IO.File.ReadAllText j.LogFile }
                      match content with
                      | x when x.ToLower().Contains("succeeded") -> return Success
                      | x when x.ToLower().Contains("failed")    -> return Failed(x)
                      | _                                        -> return Pending 
        }

     let rec waitForFinishAsync asyncJ =
        async {
            let! j = asyncJ
            let! logFileStatus = getLogfileStatusAsync j
            let hasTimedOut = (DateTime.Now - j.Started) > config.Timeout
            match (logFileStatus, j.StartSucceeded, hasTimedOut) with
            | Success, _, _       -> tracefn "%s finished successfully after %A" j.Name (System.DateTime.Now - j.Started)
                                     return {j with IsFinished = true; IsSuccessful = true; Finished = System.DateTime.Now} 
            | Failed error, _, _  -> traceError (sprintf "%s failed after %A due to %s" j.Name (System.DateTime.Now - j.Started) error)  
                                     return {j with IsFinished = true; ErrorMessage = error;  Finished = System.DateTime.Now}
            | Pending, false, _   -> traceError (sprintf "%s failed after %A due to failed startup" j.Name (System.DateTime.Now - j.Started))  
                                     return {j with IsFinished = true; ErrorMessage = "Startup failed";  Finished = System.DateTime.Now}
            | Pending, _, true    -> traceError (sprintf "%s has timed out after %A" j.Name (System.DateTime.Now - j.Started))
                                     return {j with IsFinished = true; IsSuccessful = false; ErrorMessage = "Timed out"}
            | Pending, _,_        -> do! Async.Sleep 500
                                     return! waitForFinishAsync asyncJ
        }

     let startTime = System.DateTime.Now
     
     let completedWork = 
        jobs 
        |> List.map (startBuildAsync >> waitForFinishAsync) 
        |> Async.Parallel
        |> Async.RunSynchronously
        |> List.ofArray

     let failedJobs = completedWork |> List.filter (fun j -> not j.IsSuccessful)
     match failedJobs with
     | [] -> traceEndTask "Vb6Make" (sprintf "Building %i projects" (vb6Projects |> Seq.length))
     | _  -> failwithf "Vb6 build failed after %A" (System.DateTime.Now - startTime)

/// Returns application details for provided `.vbp` files.
/// 
/// ## Information returned
///  - Name of created binary file
///  - Version as saved in `.vbp`file
///  - GUIDs of all referenced libraries and components
///
/// ## Usage
/// 
/// This is used for creating Side-By-Side interop manifests.
let public GetVb6ApplicationProjDetails (projects: string seq) =
    let defaultVb6Version = {MajorVer = 1; MinorVer = 0; RevisionVer = 0; AutoIncrementVer = 0}
    
    let getVersionValue l = 
        l 
        |> toChars 
        |> Seq.skipWhile (fun c -> c <> '=')
        |> Seq.skip 1
        |> String.Concat
        |> Int32.Parse

    let getExename project (projectlines: string seq) =
        let defaultName = (Path.GetFileNameWithoutExtension project) + ".exe"
        match projectlines |> List.ofSeq |> List.filter (fun l -> l.StartsWith("ExeName32")) with
        | [unique] -> match unique |> getValueBetween '"' '"' with
                      | name when not (String.IsNullOrWhiteSpace name) -> name
                      | _                                              -> defaultName
        | _        -> defaultName

    let getVersion (projectlines: string seq) = 
        let getVersionLines = Seq.filter (fun (l:string) ->
            l.StartsWith("MajorVer") ||
            l.StartsWith("MinorVer") ||
            l.StartsWith("RevisionVer") ||
            l.StartsWith("AutoIncrementVer")
         )

        let toVersion = Seq.fold (fun ver (line:string) ->
            match line with    
            | x when x.StartsWith("MajorVer")         -> {ver with MajorVer = x |> getVersionValue }
            | x when x.StartsWith("MinorVer")         -> {ver with MinorVer = x |> getVersionValue }
            | x when x.StartsWith("RevisionVer")      -> {ver with RevisionVer = x |> getVersionValue}
            | x when x.StartsWith("AutoIncrementVer") -> {ver with AutoIncrementVer = x |> getVersionValue}
            | _                                     -> ver) defaultVb6Version
        
        projectlines |> getVersionLines |> toVersion
    
    let getReferencesAndObjectGuids (projectLines: string seq) =
        let getVersion (lineParts:string[]) = 
            let versionParts = lineParts.[1].Split([|"."|], StringSplitOptions.RemoveEmptyEntries)
            {Vb6ReferenceVersion.Major = Int32.Parse(versionParts.[0], Globalization.NumberStyles.HexNumber);
                                 Minor = Int32.Parse(versionParts.[1], Globalization.NumberStyles.HexNumber)}

        let createReference (lineParts:string[]) = 
            {Vb6Reference.Guid = lineParts |> getReferenceLineGuid;
                          Version = lineParts |> getVersion}
        projectLines
        |> Seq.filter referenceLineFilter
        |> Seq.map (fun l -> l |> getReferenceLineParts |> createReference)

    projects 
    |> Seq.map (fun p -> async {return (p, readProjectFileLines p)})
    |> Seq.map (fun asyncData -> async {
        let! (p, lines) = asyncData
        return { ProjectFile = p
                 BinaryName = getExename p lines
                 Version = (lines |> getVersion).ToString()
                 References = lines |> getReferencesAndObjectGuids 
        }})
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Seq.ofArray

/// Determines which of the provided assemblies are referenced by the
/// provided VB6 projects, and registers them so the VB6 ide can
/// find them.
///
/// ## Paramteters
///  - `getConfig`- function to alter default VB6 build parameters
///  - `vb6Projects` - Paths to all `.vbp` files to build
///  - `possibleAssemblies` - Paths to assemblies that may be referenced by the VB6 projects
let public RegisterDependenciesForDevelopment (getConfig: Vb6BuildParams->Vb6BuildParams) (vb6Projects: string seq) (possibleAssemblies: string seq) =
    traceStartTask "RegisterDependenciesForDevelopment" (sprintf "Registering dependenices for %i projects" (vb6Projects |> Seq.length))
    let config = defaultVb6BuildParams |> getConfig 
    let details = vb6Projects |> GetVb6ApplicationProjDetails
    let interopReferences = possibleAssemblies |> GetInteropAssemblyData config.Logdir
    let applications = details |> Seq.map (fun a -> 
        { ExecutablePath = config.Outdir @@ a.BinaryName
          Version = a.Version
          Dependencies = a.References 
                         |> Seq.filter (fun r -> interopReferences |> Seq.exists (fun a -> a.Guid = r.Guid))
                         |> Seq.map (fun r -> interopReferences |> Seq.find (fun a -> a.Guid = r.Guid))
        })
    let dependenciesToRegister = applications |> Seq.collect (fun a -> a.Dependencies) |> Seq.distinct |> Seq.map (fun d -> d.Path)
    dependenciesToRegister |> RegisterAssembliesWithCodebase config.Logdir 
    traceEndTask "RegisterDependenciesForDevelopment" (sprintf "Registering dependenices for %i projects" (vb6Projects |> Seq.length))

/// Determins which of the provided assemblies are referenced by the 
/// provided VB6 projects, and __un-registers__ them
///
/// ## Paramteters
///  - `getConfig`- function to alter default VB6 build parameters
///  - `vb6Projects` - Paths to all `.vbp` files to build
///  - `possibleAssemblies` - Paths to assemblies that may be referenced by the VB6 projects
let public UnRegisterDependenciesForDevelopment (getConfig: Vb6BuildParams->Vb6BuildParams) (vb6Projects: string seq) (possibleAssemblies: string seq) =
    traceStartTask "UnRegisterDependenciesForDevelopment" (sprintf "Un-registering dependenices for %i projects" (vb6Projects |> Seq.length))
    let config = defaultVb6BuildParams |> getConfig 
    let details = vb6Projects |> GetVb6ApplicationProjDetails
    let interopReferences = possibleAssemblies |> GetInteropAssemblyData config.Logdir
    let applications = details |> Seq.map (fun a -> 
        { ExecutablePath = config.Outdir @@ a.BinaryName
          Version = a.Version
          Dependencies = a.References 
                         |> Seq.filter (fun r -> interopReferences |> Seq.exists (fun a -> a.Guid = r.Guid))
                         |> Seq.map (fun r -> interopReferences |> Seq.find (fun a -> a.Guid = r.Guid))
        })
    let dependenciesToRegister = applications |> Seq.collect (fun a -> a.Dependencies) |> Seq.distinct |> Seq.map (fun d -> d.Path)
    dependenciesToRegister |> UnregisterAssemblies config.Logdir
    traceEndTask "UnRegisterDependenciesForDevelopment" (sprintf "Un-registering dependenices for %i projects" (vb6Projects |> Seq.length))


/// All-In-one build and manifest function for VB6 __applications__ referencing .net __libraries__
///
/// ## Paramteters
///  - `getConfig`- function to alter default VB6 build parameters
///  - `vb6Projects` - Paths to all `.vbp` files to build
///  - `possibleAssemblies` - Paths to assemblies that may be referenced by the VB6 projects
///
/// ## Process
///
/// This function will:
///
/// 1. Determine which of the `possibleAssemnblies` are referenced by any of the provided `.vbp` files
/// 2. Temporarily register any referenced assemblies using `RegAsm /codebase /tlb`
/// 3. Run VB6 command line make on all provided `.vbp` projects
/// 4. Unregister all registered assemblies
/// 5. Generate and embed Side-By-Side interop appplication manifests in all generated VB6 executables
/// 6. Generate and embed Side-By-Side interop assembly manifest in all referenced assemblies 
let public BuildAndEmbedInteropManifests (getConfig: Vb6BuildParams->Vb6BuildParams) (vb6Projects: string seq) (possibleAssemblies: string seq) =
    traceStartTask "BuildAndEmbedInteropManifests" (sprintf "Building and embedding for %i projects" (vb6Projects |> Seq.length))
    let config = defaultVb6BuildParams |> getConfig 
    let details = vb6Projects |> GetVb6ApplicationProjDetails
    let interopReferences = possibleAssemblies |> GetInteropAssemblyData config.Logdir
    let applications = details |> Seq.map (fun a -> 
        { ExecutablePath = config.Outdir @@ a.BinaryName
          Version = a.Version
          Dependencies = a.References 
                         |> Seq.filter (fun r -> interopReferences |> Seq.exists (fun a -> a.Guid = r.Guid))
                         |> Seq.map (fun r ->  interopReferences |> Seq.find (fun a -> a.Guid = r.Guid))
        })
    let dependenciesToRegister = applications |> Seq.collect (fun a -> a.Dependencies) |> Seq.distinct |> Seq.map (fun d -> d.Path)
    dependenciesToRegister |> RegisterAssembliesWithCodebase config.Logdir 
    vb6Projects |> Vb6Make getConfig
    dependenciesToRegister |> UnregisterAssemblies config.Logdir
    applications |> AddEmbeddedApplicationManifest config.Logdir
    dependenciesToRegister |> AddEmbeddedAssemblyManifest config.Logdir
    traceEndTask "BuildAndEmbedInteropManifests" (sprintf "Building and embedding for %i projects" (vb6Projects |> Seq.length))

/// Fixes dependency versions in VB6 project files 
///
/// ## Paramteters
///  - `getConfig`- function to alter default VB6 build parameters
///  - `vb6Projects` - Paths to all `.vbp` to update references in
///  - `possibleAssemblies` - Paths to assemblies that may be referenced by the VB6 projects
///
/// Running this task will:
///
/// 1. In all VB6 projects provided: Get all references that intersects with the provided assemblies arg
/// 2. Check if there is a version difference
/// 3. Update the VB6 project file to reflect the actual version used.
///
/// Note: Vb6 Reference versions are __hex numbers__ not decimals like .net verions. This task handles
///       this difference automatically.
let public UpdateDependencyVersions (getConfig: Vb6BuildParams->Vb6BuildParams) (vb6Projects: string seq) (possibleAssemblies: string seq) =
    traceStartTask "UpdateDependencyVersion" (sprintf "Updating dependency versions for %i projects" (vb6Projects |> Seq.length))
    let config = defaultVb6BuildParams |> getConfig 
    let details = vb6Projects |> GetVb6ApplicationProjDetails
    let interopReferences = possibleAssemblies |> GetInteropAssemblyData config.Logdir

    //Filter to select only projects and references that needs updating
    let projectNeedsUpdating (p:Vb6Project) = 
        let transformInteropVersion (a:InteropAssemblyData) = 
            let versionParts = a.Version.Split([|'.'|], StringSplitOptions.RemoveEmptyEntries) 
                               |> Array.map (Int32.Parse)
            {Vb6ReferenceVersion.Major = versionParts.[0];
                                 Minor = versionParts.[1]}
        let referencesToUpdate = 
            p.References 
            |> Seq.choose (fun r -> match interopReferences |> Seq.tryFind (fun a -> a.Guid = r.Guid) with
                                    | Some(interop) -> Some(r, interop, interop |> transformInteropVersion)
                                    | None -> None)
            |> Seq.choose (fun (r,a,v) -> match r.Version <> v with
                                          | true  -> Some(r,a,v)
                                          | false -> None) 
        if referencesToUpdate |> Seq.isEmpty then None else Some(p, referencesToUpdate)
    
    let updateProjectReferences (p:Vb6Project * (Vb6Reference * InteropAssemblyData * Vb6ReferenceVersion) seq) = 
        let versionToString (v:Vb6ReferenceVersion) = 
            sprintf "%X.%X" v.Major v.Minor   
        let (project, references) = p
        readProjectFileLines project.ProjectFile
        |> Seq.map (fun l ->
            let lineparts = getReferenceLineParts l
            match referenceLineFilter l with
            | true  -> 
               let guid = getReferenceLineGuid lineparts
               match references |> Seq.tryFind (fun (r,a,v) -> r.Guid = guid) with
               | Some(r,a,v) -> 
                   lineparts.[1] <- (versionToString v)
                   tracefn "Updated %s to %i.%i for %s" a.Name v.Major v.Minor project.BinaryName 
                   lineparts |> createReferenceLine 
               | None -> l
            | false -> l)
        |> writeProjectFileLines project.ProjectFile

    details |> Seq.choose projectNeedsUpdating |> Seq.iter updateProjectReferences
    traceEndTask "UpdateDependencyVersion" (sprintf "Updating dependency versions for %i projects" (vb6Projects |> Seq.length))
