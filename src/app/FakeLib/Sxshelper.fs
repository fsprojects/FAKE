/// Module that enables creating and embedding Side-by-Side interop
/// manifests for registration free deployment of Com-.net interop projects
module Fake.SxsHelper

open Fake
open System
open System.IO
open System.Linq
open System.Xml.Linq

/// Represents a `.NET` assembly that may be used in COM interop projects
type InteropAssemblyData = 
    {
        /// Assembly name
        Name:string
        
        /// Path to the assembly file
        Path:string 
        
        /// Assembly version
        Version:string 
        
        /// Guid from the `System.Runtime.Interopservice.GuidAttribute` of the assembly
        Guid:System.Guid
    }

/// Represents an executable to create an _application manifest_ for
type InteropApplicationData = 
    {
        /// Path of the executable binary file
        ExecutablePath:string
        
        /// Version of the executable
        Version:String

        /// Dependent `.NET` assemblies of the executable
        Dependencies:InteropAssemblyData seq 
    }

/// Represents status of attempted parsing
/// of IL file created from executing `ildasm.exe`
/// on a binary
type private ILparsingResult =
    /// Found all required data
    /// Includes structured assembly data
    | Success of InteropAssemblyData
    /// Failed to find all reguired data
    /// Includes an error message
    | Failed of string

/// Path to `mt.exe`
/// ref: https://msdn.microsoft.com/en-us/library/aa375649(v=vs.85).aspx
let private  mtToolPath = !! (sdkBasePath + "/**/mt.exe") -- (sdkBasePath + "/**/x64/*.*") 
                          |> getNewestTool

/// Path to `ildasm.exe
/// .net fx dissasembly tool
/// ref: https://msdn.microsoft.com/en-us/library/f7dy01k1(v=vs.110).aspx
let private ildasmPath = !! (sdkBasePath + "/**/ildasm.exe") -- (sdkBasePath + "/**/x64/*.*")
                         |> getNewestTool

/// XLM namespace of manifest files
let private manifestNamespace = "urn:schemas-microsoft-com:asm.v1"

/// create XName from string with manifest namepace
let private nsXn s = XName.Get(s, manifestNamespace)
/// create XName from string __without__ manifest namespace
let private xn s = XName.Get(s)

let private setAssemblyIdAttributeValue attributeName attributeValue (manifest:XContainer) = 
     manifest.Descendants(nsXn "assemblyIdentity")
         .Single()
         .Attribute(xn attributeName)
         .SetValue(attributeValue)

let private getAssemblyIdAttributeValue attributeName (manifest:XContainer) = 
     manifest.Descendants(nsXn "assemblyIdentity")
         .Single()
         .Attribute(xn attributeName)
         .Value

let private setAssemblyName manifest name =
     manifest |> setAssemblyIdAttributeValue "name" name

let private setAssemblyVersion manifest version =
    manifest |> setAssemblyIdAttributeValue "version" version

let private copyAssemblyIdAttributeValue attributeName toManifest fromManifest = 
    toManifest 
    |> setAssemblyIdAttributeValue attributeName 
        (fromManifest |> getAssemblyIdAttributeValue attributeName)

let private copyAssemblyIdName   = 
    copyAssemblyIdAttributeValue "name" 

let private copyAssemblyIdVersion = 
    copyAssemblyIdAttributeValue "version" 

let private copyElements ((toManifest:XContainer), toElement) ((fromManifest:XContainer), elementName) = 
    toManifest.Element(nsXn toElement).Add(fromManifest.Descendants(nsXn elementName))

let private copyClrClasses toManifest fromManifest = 
    (fromManifest, "clrClass") |> copyElements (toManifest, "assembly")

/// Embeds a manifest file in a binary using `mt.exe`
let private embedManiFestAsync workingDir (asyncData: Async<string*string>) =
    async {
        let! (manifestPath, binaryPath) = asyncData
        let! embedManifestResult = asyncShellExec {defaultParams with 
                                                    Program = mtToolPath
                                                    WorkingDirectory = workingDir
                                                    CommandLine = (sprintf "-manifest \"%s\" -outputResource:\"%s\" -nologo -verbose" manifestPath binaryPath)}
        if embedManifestResult <> 0 then failwith (sprintf "Embedding SxS manifest from %s into %s failed" manifestPath binaryPath)
        return ()
    }
        
/// Created and embeds assembly Side-by-side interop manifests for provided assemblies
/// 
/// ## Parameters
///  - `workingDir` - somewhere to put any temp files created
///  - `assemblies` - .net assemblies to create manifests for
///
/// ## Process
///
/// This function will use `mt.exe` (ref: https://msdn.microsoft.com/en-us/library/aa375649(v=vs.85).aspx)
/// to create a manifest for each assembly. This created manifest is unfortunately _not_ a valid 
/// interop Side-by-Side manifest, but it has the important `clrClass` elements, + `version` and `name`info that would be the most
/// difficult to create through other means.
/// The important info is then put into a valid base manifest and embedded into the assembly as a resource.
let AddEmbeddedAssemblyManifest workingDir (assemblies: string seq) =
     traceStartTask "AddEmbeddedAssemblyManifest" (sprintf "Adding assembly manifests to %i assemlbies" (assemblies |> Seq.length)) 
     let createManifestPath assembly =
            workingDir @@ ((Path.GetFileNameWithoutExtension assembly) + ".manifest")

     let assemblyManifestBase = 
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
                <assemblyIdentity name="assemblyName" version="1.0.0.0" type="win32"/>
            </assembly>
            """.Trim()

     let createManiFestBaseAsync assembly =
        async {
            let manifestPath = createManifestPath assembly
            let! createBaseManifestResult = asyncShellExec {defaultParams with
                                                                    Program = mtToolPath
                                                                    WorkingDirectory = workingDir
                                                                    CommandLine = (sprintf "-managedassemblyname:\"%s\" -out:\"%s\" -nodependency -nologo -verbose" assembly manifestPath)
                                                            }
            if createBaseManifestResult <> 0 then failwith (sprintf "Failed generating base manifest for %s" assembly)
            return (assembly, manifestPath)
        }
      
     let createManifestAsync (asyncData: Async<string * string>) =
        async {
            let! (assembly, manifestPath) = asyncData
            let createdManifest = manifestPath |> XDocument.Load
            let assemblyManifest = assemblyManifestBase |> XDocument.Parse 
            createdManifest |> copyAssemblyIdName assemblyManifest
            createdManifest |> copyAssemblyIdVersion assemblyManifest
            createdManifest |> copyClrClasses assemblyManifest
            assemblyManifest.Save manifestPath
            return (manifestPath, assembly)
        }

     assemblies 
     |> Seq.map (createManiFestBaseAsync >> createManifestAsync >> embedManiFestAsync workingDir )
     |> Async.Parallel
     |> Async.RunSynchronously
     |> ignore
     traceEndTask "AddEmbeddedAssemblyManifest" (sprintf "Adding assembly manifests to %i assemlbies" (assemblies |> Seq.length)) 

/// Gets `name`, `path', `version` and interop `Guid` for those of the provided assemblies that have 
/// all of the required information.
///
/// ## Parameters
///  - `workingDir` - Somewhere to put temporary files
///  - `assemblies` - assemblies to get data from
///
/// ## Purpose
/// 
/// In order to create _application_ interop side-by-side manifests we need to know some metadata
/// about the assemblies that may be referenced from COM executables.
/// For the manifest we need the _assembly version_ and _ assembly name_. And in addition to that
/// the interop _guid_ is collected so we can determine if the assembly is referenced by _vb6 projects_
///
/// ## Process
///
/// This function is a _hack_. To avoid using reflection and loading all potential assemblies into the
/// appdomain (with all the possible problems that may cause). I wanted to get this metadata by other means.
/// I ended up using the windows sdk dissasembler `ildasm.exe` (ref: https://msdn.microsoft.com/en-us/library/f7dy01k1(v=vs.110).aspx)
/// to create the smallest dissasembly I could (Really only need the manifest part), and the parse the IL file to get the metadata
/// (If anyone knows a cleaner / better way, pls improve on the code)
let GetInteropAssemblyData workingDir assemblies = 
    let toChars (s:string) = s.ToCharArray () |> Seq.ofArray
    let replace (oldVal:Char) (newVal:Char) (s:string) = (s.Replace(oldVal, newVal))
    let getValueBetween startChar endChar (line:string) = 
        line
        |> toChars
        |> Seq.skipWhile (fun c -> c <> startChar)
        |> Seq.skip 1
        |> Seq.takeWhile (fun c -> c <> endChar)
        |> String.Concat

    let getGuid assembly (customDataLines: string[])= 
        match customDataLines |> Array.tryFind (fun l -> l.Contains("GuidAttribute")) with
        | None      -> None
        | Some data ->  try 
                            match data |> getValueBetween '\'' '\'' |> Guid.TryParse with
                            | (true, guid) -> Some(guid)
                            | (false, _ )  -> None
                        with
                        | :? System.ArgumentException as ex -> None 

    let tryGetInteropInfo (assembly, (lines: string seq)) =
        let assemblyData = 
            lines 
            |> Seq.skipWhile (fun l -> not (l.Contains(".assembly") && not (l.Contains("extern"))))
            |> Seq.takeWhile (fun l -> l <> "}")
        if assemblyData.Count() = 0 then 
              Failed (sprintf "Did not find assemblydata section for %s" assembly)
        else
            let customData = (assemblyData |> Seq.filter (fun l -> let trimmed = l.Trim()
                                                                   trimmed.StartsWith(".custom") || 
                                                                   trimmed.StartsWith("="))
                                           |> String.Concat
                              ).Split([|".custom"|], StringSplitOptions.RemoveEmptyEntries)
                                           
            let versionLine = assemblyData |> Seq.tryFind(fun l -> l.Trim().StartsWith(".ver"))
            let assyName = (assemblyData |> Seq.head).Replace(".assembly","").Trim()
            
            
            match (versionLine, getGuid assembly customData, String.IsNullOrWhiteSpace assyName) with
            | _, _, true -> Failed(sprintf "No assembly name found for %s" assembly)
            | None, _, _ -> Failed(sprintf "No version info found for %s" assembly)
            | _, None, _ -> Failed(sprintf "No guid attribute found for %s" assembly)
            | Some version, Some guid, false -> Success({
                                                                Name = assyName
                                                                Path = assembly
                                                                Guid = guid
                                                                Version = version.Replace(".ver","").Trim().Replace(":",".")
                                                              })
    let tryGetInteropInfoAsync asyncData =
        async {
            let! (assembly, (lines: string[])) = asyncData
            return tryGetInteropInfo (assembly, lines)
        }

    let getRawAssemblyDataAsync assembly = 
        async {
            let ilName = workingDir @@ ((Path.GetFileNameWithoutExtension assembly) + ".il")
            let! dissasembleResult = 
                asyncShellExec {defaultParams with 
                                    Program = ildasmPath
                                    WorkingDirectory = workingDir
                                    CommandLine = (sprintf "\"%s\" /output:\"%s\" /pubonly /caverbal /item:non_items_please /nobar /utf8" assembly ilName)}
            if dissasembleResult <> 0 then failwith (sprintf "Failed using ildasm to get metadata for %s" assembly)
            let! lines = async {return File.ReadAllLines ilName}
            return (assembly, lines)
        }
        
     

    assemblies 
    // To Avoid rerunning the complete chain for every operation
    // a list is better.
    |> List.ofSeq
    |> List.map ((getRawAssemblyDataAsync >> tryGetInteropInfoAsync))
    |> Async.Parallel
    |> Async.RunSynchronously
    |> List.ofArray
    |> List.filter (fun l -> match l with
                             | Failed error -> traceImportant error
                                               false
                             | Success data -> true)
    |> List.map (fun l -> match l with 
                          | Failed _ -> failwith "This should not be happening"
                          | Success data -> data)

/// Creates and adds _application interop side-by-side manifests_ to provided executables
///
/// ## Parameters
///  - `workingdir` - somewhere to put any temporary files
///  - `applications` - Metadata about executables to create manifests for.
let public AddEmbeddedApplicationManifest workingDir (applications: InteropApplicationData seq) = 
    traceStartTask "AddEmbeddedApplicationManifest" (sprintf "Adding embedded application manifest to %i applications" (applications |> Seq.length))
    let applicationManifestBase = 
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
            <assemblyIdentity name="application.exe" version="1.0.0.0" type="win32" processorArchitecture="x86"/>
        </assembly>
        """.Trim() 

    let dependencyBase =
        ("""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
            <dependency>
                <dependentAssembly>
                    <assemblyIdentity name="" version="1.0.0.0" type="win32"/>
                </dependentAssembly>
            </dependency>
        </assembly>
        """.Trim() |> XDocument.Parse).Descendants(nsXn "dependency").Single()
    

    let createDependencyElements (dependencies:InteropAssemblyData seq) =
        let createDependencyElement (dependency: InteropAssemblyData) =
            let dependencyElement = (new XElement(dependencyBase))
            dependency.Name |> setAssemblyName dependencyElement
            dependency.Version |> setAssemblyVersion dependencyElement
            dependencyElement

        dependencies |> Seq.map createDependencyElement

    let createManifest (application: InteropApplicationData) =
        let appManifest = applicationManifestBase |> XDocument.Parse
        application.ExecutablePath |> Path.GetFileName |> setAssemblyName appManifest
        application.Version |> setAssemblyVersion appManifest
        appManifest.Element(nsXn "assembly").Add(application.Dependencies |> createDependencyElements)
        let appManifestPath = workingDir @@ ((Path.GetFileName application.ExecutablePath) + ".manifest")
        appManifest.Save(appManifestPath)
        (appManifestPath, application.ExecutablePath)
    
    let createManifestAsync (application: InteropApplicationData) =
        async {
            return createManifest application
        }

    applications 
    |> Seq.map (fun a -> 
        tracefn "Creating manifest for %s" (Path.GetFileNameWithoutExtension a.ExecutablePath)
        a |> (createManifestAsync >> embedManiFestAsync workingDir) )
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
    traceEndTask "AddEmbeddedApplicationManifest" (sprintf "Adding embedded application manifest to %i applications" (applications |> Seq.length))