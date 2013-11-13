/// Contains tasks to generate AssemblyInfo files for C# and F#.
/// There is also a tutorial about the [AssemblyInfo tasks](../assemblyinfo.html) available.
module Fake.AssemblyInfoFile

let internal assemblyVersionRegex = getRegEx @"([0-9]+.)+[0-9]+"

let private NormalizeVersion version = assemblyVersionRegex.Match(version).Captures.[0].Value

/// Represents AssemblyInfo attributes
type Attribute(name,value,inNamespace) =
   member this.Name = name
   member this.Value = value
   member this.Namespace = inNamespace

   /// Creates a simple attribute with string values. Used as base for other attributes
   static member StringAttribute(name,value,inNamespace) = Attribute(name,sprintf "\"%s\"" value,inNamespace)
   /// Creates a simple attribute with boolean values. Used as base for other attributes
   static member BoolAttribute(name,value,inNamespace) = Attribute(name,sprintf "%b" value,inNamespace)

   /// Creates an attribute which holds the company information
   static member Company(value) = Attribute.StringAttribute("AssemblyCompany",value,"System.Reflection")
   /// Creates an attribute which holds the product name
   static member Product(value) = Attribute.StringAttribute("AssemblyProduct",value,"System.Reflection")
   /// Creates an attribute which holds the copyright information
   static member Copyright(value) = Attribute.StringAttribute("AssemblyCopyright",value,"System.Reflection")
   /// Creates an attribute which holds the product title
   static member Title(value) = Attribute.StringAttribute("AssemblyTitle",value,"System.Reflection")
   /// Creates an attribute which holds the product description
   static member Description(value) = Attribute.StringAttribute("AssemblyDescription",value,"System.Reflection")
   /// Creates an attribute which holds the assembly culture information
   static member Culture(value) = Attribute.StringAttribute("AssemblyCulture",value,"System.Reflection")
   /// Creates an attribute which holds the assembly configuration
   static member Configuration(value) = Attribute.StringAttribute("AssemblyConfiguration",value,"System.Reflection")
   /// Creates an attribute which holds the trademark
   static member Trademark(value) = Attribute.StringAttribute("AssemblyTrademark",value,"System.Reflection")
   /// Creates an attribute which holds the assembly version
   static member Version(value) = Attribute.StringAttribute("AssemblyVersion",NormalizeVersion value,"System.Reflection")
   /// Creates an attribute which holds the assembly key file
   static member KeyFile(value) = Attribute.StringAttribute("AssemblyKeyFile",value,"System.Reflection")
   /// Creates an attribute which holds the assembly key name
   static member KeyName(value) = Attribute.StringAttribute("AssemblyKeyName",value,"System.Reflection")
   /// Creates an attribute which holds the "InternalVisibleTo" data
   static member InternalsVisibleTo(value) = Attribute.StringAttribute("InternalsVisibleTo",value,"System.Runtime.CompilerServices")
   /// Creates an attribute which holds the assembly file version
   static member FileVersion(value) = Attribute.StringAttribute("AssemblyFileVersion",NormalizeVersion value,"System.Reflection")
   /// Creates an attribute which holds an assembly information version
   static member InformationalVersion(value) = Attribute.StringAttribute("AssemblyInformationalVersion",value,"System.Reflection")
   /// Creates an attribute which holds the Guid
   static member Guid(value) = Attribute.StringAttribute("Guid",value,"System.Runtime.InteropServices")
   /// Creates an attribute which specifies if the assembly is visible via COM
   static member ComVisible(?value) = Attribute.BoolAttribute("ComVisible",defaultArg value false,"System.Runtime.InteropServices")
   /// Creates an attribute which specifies if the assembly is CLS compliant
   static member CLSCompliant(?value) = Attribute.BoolAttribute("CLSCompliant",defaultArg value false,"System")
   /// Creates an attribute which specifies if the assembly uses delayed signing
   static member DelaySign(value) = Attribute.BoolAttribute("AssemblyDelaySign",defaultArg value false,"System.Reflection")

let private writeToFile outputFileName (lines: seq<string>) =    
    let fi = fileInfo outputFileName
    fi.Delete()

    use writer = new System.IO.StreamWriter(outputFileName,false,System.Text.Encoding.UTF8) 
    lines |> Seq.iter writer.WriteLine
    tracefn "Created AssemblyInfo file \"%s\"." outputFileName

let private getDependencies attributes =
    attributes
    |> Seq.map (fun (attr:Attribute) -> attr.Namespace)
    |> Set.ofSeq
    |> Seq.toList

let private getAssemblyVersionInfo attributes =
    match attributes |> Seq.tryFind (fun (attr:Attribute) -> attr.Name = "AssemblyVersion") with
    | Some attr -> attr.Value
    | None _ -> "\"" + buildVersion + "\""

/// Creates a C# AssemblyInfo file with the given attributes.
/// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the current version no. from inside of an assembly.
let CreateCSharpAssemblyInfo outputFileName attributes =
    traceStartTask "AssemblyInfo" outputFileName

    "// <auto-generated/>" ::
    (getDependencies attributes |> List.map (sprintf "using %s;")) @ [""] @
    (attributes |> Seq.toList |> List.map (fun (attr:Attribute) -> sprintf "[assembly: %sAttribute(%s)]" attr.Name attr.Value)) @
    ["namespace System {"
     "    internal static class AssemblyVersionInformation {"
     sprintf "        internal const string Version = %s;" (getAssemblyVersionInfo attributes)
     "    }"
     "}"]
    |> writeToFile outputFileName
    
    traceEndTask "AssemblyInfo" outputFileName

/// Creates a F# AssemblyInfo file with the given attributes.
/// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the current version no. from inside of an assembly.
let CreateFSharpAssemblyInfo outputFileName attributes =
    traceStartTask "AssemblyInfo" outputFileName

    ["namespace System"] @
    (getDependencies attributes |> List.map (sprintf "open %s")) @ [""] @
    (attributes |> Seq.toList |> List.map (fun (attr:Attribute) -> sprintf "[<assembly: %sAttribute(%s)>]" attr.Name attr.Value)) @
    ["()"
     ""
     "module internal AssemblyVersionInformation ="
     sprintf "    let [<Literal>] Version = %s" (getAssemblyVersionInfo attributes)]
    |> writeToFile outputFileName

    traceEndTask "AssemblyInfo" outputFileName
