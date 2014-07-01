/// Contains tasks to generate AssemblyInfo files for C# and F#.
/// There is also a tutorial about the [AssemblyInfo tasks](../assemblyinfo.html) available.
module Fake.AssemblyInfoFile

let internal assemblyVersionRegex = getRegEx @"([0-9]+.)+[0-9]+"

let private NormalizeVersion version = 
    let m = assemblyVersionRegex.Match(version)
    if m.Captures.Count > 0 then m.Captures.[0].Value
    else version

/// Represents options for configuring the emission of AssemblyInfo
type AssemblyInfoFileConfig
    ( // If true, a module (for F#) or static class (for C#), which contains the assembly version, will be generated
      generateClass : bool,
      // The optional namespace into which assembly info will be generated; defaults to "System".
      ?useNamespace : string ) =
        member x.GenerateClass = generateClass
        member x.UseNamespace =
            match useNamespace with
            | Some n -> n
            | None -> "System"
        static member Default = AssemblyInfoFileConfig(true)

/// Represents AssemblyInfo attributes
type Attribute(name, value, inNamespace) = 
    member this.Name = name
    member this.Value = value
    member this.Namespace = inNamespace
    
    /// Creates a simple attribute with string values. Used as base for other attributes
    static member StringAttribute(name, value, inNamespace) = Attribute(name, sprintf "\"%s\"" value, inNamespace)
    
    /// Creates a simple attribute with boolean values. Used as base for other attributes
    static member BoolAttribute(name, value, inNamespace) = Attribute(name, sprintf "%b" value, inNamespace)
    
    /// Creates an attribute which holds the company information
    static member Company(value) = Attribute.StringAttribute("AssemblyCompany", value, "System.Reflection")
    
    /// Creates an attribute which holds the product name
    static member Product(value) = Attribute.StringAttribute("AssemblyProduct", value, "System.Reflection")
    
    /// Creates an attribute which holds the copyright information
    static member Copyright(value) = Attribute.StringAttribute("AssemblyCopyright", value, "System.Reflection")
    
    /// Creates an attribute which holds the product title
    static member Title(value) = Attribute.StringAttribute("AssemblyTitle", value, "System.Reflection")
    
    /// Creates an attribute which holds the product description
    static member Description(value) = Attribute.StringAttribute("AssemblyDescription", value, "System.Reflection")
    
    /// Creates an attribute which holds the assembly culture information
    static member Culture(value) = Attribute.StringAttribute("AssemblyCulture", value, "System.Reflection")
    
    /// Creates an attribute which holds the assembly configuration
    static member Configuration(value) = Attribute.StringAttribute("AssemblyConfiguration", value, "System.Reflection")
    
    /// Creates an attribute which holds the trademark
    static member Trademark(value) = Attribute.StringAttribute("AssemblyTrademark", value, "System.Reflection")
    
    /// Creates an attribute which holds the assembly version
    static member Version(value) = 
        Attribute.StringAttribute("AssemblyVersion", NormalizeVersion value, "System.Reflection")
    
    /// Creates an attribute which holds the assembly key file
    static member KeyFile(value) = Attribute.StringAttribute("AssemblyKeyFile", value, "System.Reflection")
    
    /// Creates an attribute which holds the assembly key name
    static member KeyName(value) = Attribute.StringAttribute("AssemblyKeyName", value, "System.Reflection")
    
    /// Creates an attribute which holds the "InternalVisibleTo" data
    static member InternalsVisibleTo(value) = 
        Attribute.StringAttribute("InternalsVisibleTo", value, "System.Runtime.CompilerServices")
    
    /// Creates an attribute which holds the assembly file version
    static member FileVersion(value) = 
        Attribute.StringAttribute("AssemblyFileVersion", NormalizeVersion value, "System.Reflection")
    
    /// Creates an attribute which holds an assembly information version
    static member InformationalVersion(value) = 
        Attribute.StringAttribute("AssemblyInformationalVersion", value, "System.Reflection")
    
    /// Creates an attribute which holds the Guid
    static member Guid(value) = Attribute.StringAttribute("Guid", value, "System.Runtime.InteropServices")
    
    /// Creates an attribute which specifies if the assembly is visible via COM
    static member ComVisible(?value) = 
        Attribute.BoolAttribute("ComVisible", defaultArg value false, "System.Runtime.InteropServices")
    
    /// Creates an attribute which specifies if the assembly is CLS compliant
    static member CLSCompliant(?value) = Attribute.BoolAttribute("CLSCompliant", defaultArg value false, "System")
    
    /// Creates an attribute which specifies if the assembly uses delayed signing
    static member DelaySign(value) = 
        Attribute.BoolAttribute("AssemblyDelaySign", defaultArg value false, "System.Reflection")

    /// Create an attribute which specifies metadata about the assembly
    static member Metadata(name,value) = 
        Attribute.StringAttribute("AssemblyMetadata", sprintf "%s\",\"%s" name value, "System.Reflection")

let private writeToFile outputFileName (lines : seq<string>) = 
    let fi = fileInfo outputFileName
    fi.Delete()
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputFileName)) |> ignore
    use writer = new System.IO.StreamWriter(outputFileName, false, System.Text.Encoding.UTF8)
    lines |> Seq.iter writer.WriteLine
    tracefn "Created AssemblyInfo file \"%s\"." outputFileName

let private getDependencies attributes = 
    attributes
    |> Seq.map (fun (attr : Attribute) -> attr.Namespace)
    |> Set.ofSeq
    |> Seq.toList

let private getAssemblyVersionInfo attributes = 
    match attributes |> Seq.tryFind (fun (attr : Attribute) -> attr.Name = "AssemblyVersion") with
    | Some attr -> attr.Value
    | None _ -> "\"" + buildVersion + "\""

/// Creates a C# AssemblyInfo file with the given attributes and configuration.
/// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the current version no. from inside of an assembly.
let CreateCSharpAssemblyInfoWithConfig (outputFileName, attributes, config : AssemblyInfoFileConfig) =
    traceStartTask "AssemblyInfo" outputFileName
    let generateClass, useNamespace = config.GenerateClass, config.UseNamespace

    let attributeLines = 
        "// <auto-generated/>" :: (getDependencies attributes |> List.map (sprintf "using %s;")) 
        @ [ "" ] 
          @ (attributes
             |> Seq.toList
             |> List.map (fun (attr : Attribute) -> sprintf "[assembly: %sAttribute(%s)]" attr.Name attr.Value)) 

    let sourceLines =
        if generateClass then
            [ sprintf "namespace %s {" useNamespace
              "    internal static class AssemblyVersionInformation {"
              sprintf "        internal const string Version = %s;" (getAssemblyVersionInfo attributes)
              "    }"
              "}" ]
        else []
    
    attributeLines @ sourceLines
    |> writeToFile outputFileName
    traceEndTask "AssemblyInfo" outputFileName

/// Creates a F# AssemblyInfo file with the given attributes and configuration.
/// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the current version no. from inside of an assembly.
let CreateFSharpAssemblyInfoWithConfig (outputFileName, attributes, config : AssemblyInfoFileConfig) =
    traceStartTask "AssemblyInfo" outputFileName
    let generateClass, useNamespace = config.GenerateClass, config.UseNamespace
    
    let sourceLines = 
        let required = 
            [ sprintf "namespace %s" useNamespace ] 
            @ (getDependencies attributes |> List.map (sprintf "open %s")) 
              @ [ "" ] 
                @ (attributes
                   |> Seq.toList
                   |> List.map (fun (attr : Attribute) -> sprintf "[<assembly: %sAttribute(%s)>]" attr.Name attr.Value)) 
                  @ [ "do ()"; "" ]
        
        let optional = 
            [ "module internal AssemblyVersionInformation ="
              sprintf "    let [<Literal>] Version = %s" (getAssemblyVersionInfo attributes) ]
        
        if generateClass then required @ optional
        else required
    sourceLines |> writeToFile outputFileName
    traceEndTask "AssemblyInfo" outputFileName

/// Creates a VB AssemblyInfo file with the given attributes and configuration.
/// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the current version no. from inside of an assembly.
let CreateVisualBasicAssemblyInfoWithConfig (outputFileName, attributes, config : AssemblyInfoFileConfig) =
    traceStartTask "AssemblyInfo" outputFileName
    let generateClass, useNamespace = config.GenerateClass, config.UseNamespace

    let attributeLines =
        "' <auto-generated/>" :: (getDependencies attributes |> List.map (sprintf "Imports %s"))
        @ [ "" ]
          @ (attributes
             |> Seq.toList
             |> List.map (fun (attr : Attribute) -> sprintf "<assembly: %sAttribute(%s)>" attr.Name attr.Value))

    let sourceLines =
        if generateClass then
            [ sprintf "Namespace %s" useNamespace
              "    Friend NotInheritable Class"
              sprintf "        Friend Const Version As String = %s" (getAssemblyVersionInfo attributes)
              "    End Class"
              "End Namespace" ]
        else []

    attributeLines @ sourceLines
    |> writeToFile outputFileName
    traceEndTask "AssemblyInfo" outputFileName

/// Creates a C# AssemblyInfo file with the given attributes.
/// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the current version no. from inside of an assembly.
let CreateCSharpAssemblyInfo outputFileName attributes = 
    CreateCSharpAssemblyInfoWithConfig (outputFileName, attributes, AssemblyInfoFileConfig.Default)

/// Creates a F# AssemblyInfo file with the given attributes.
/// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the current version no. from inside of an assembly.
let CreateFSharpAssemblyInfo outputFileName attributes = 
    CreateFSharpAssemblyInfoWithConfig (outputFileName, attributes, AssemblyInfoFileConfig.Default)

/// Creates a VB AssemblyInfo file with the given attributes.
/// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the current version no. from inside of an assembly.
let CreateVisualBasicAssemblyInfo outputFileName attributes =
    CreateVisualBasicAssemblyInfoWithConfig (outputFileName, attributes, AssemblyInfoFileConfig.Default)
