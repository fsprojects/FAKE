module Fake.AssemblyInfoFile

type Attribute(name,value,inNamespace) =
   member this.Name = name
   member this.Value = value
   member this.Namespace = inNamespace

   static member StringAttribute(name,value,dependencies) = Attribute(name,sprintf "\"%s\"" value,dependencies)
   static member BoolAttribute(name,value,dependencies) = Attribute(name,sprintf "%b" value,dependencies)

   static member Company(value) = Attribute.StringAttribute("AssemblyCompany",value,"System.Reflection")
   static member Product(value) = Attribute.StringAttribute("AssemblyProduct",value,"System.Reflection")
   static member Copyright(value) = Attribute.StringAttribute("AssemblyCopyright",value,"System.Reflection")
   static member Title(value) = Attribute.StringAttribute("AssemblyTitle",value,"System.Reflection")
   static member Description(value) = Attribute.StringAttribute("AssemblyDescription",value,"System.Reflection")
   static member Culture(value) = Attribute.StringAttribute("AssemblyCulture",value,"System.Reflection")
   static member Configuration(value) = Attribute.StringAttribute("AssemblyConfiguration",value,"System.Reflection")
   static member Trademark(value) = Attribute.StringAttribute("AssemblyTrademark",value,"System.Reflection")
   static member Version(value) = Attribute.StringAttribute("AssemblyVersion",value,"System.Reflection")
   static member KeyFile(value) = Attribute.StringAttribute("AssemblyKeyFile",value,"System.Reflection")
   static member KeyName(value) = Attribute.StringAttribute("AssemblyKeyName",value,"System.Reflection")   
   static member FileVersion(value) = Attribute.StringAttribute("AssemblyFileVersion",value,"System.Reflection")
   static member InformationalVersion(value) = Attribute.StringAttribute("AssemblyInformationalVersion",value,"System.Reflection")
   static member Guid(value) = Attribute.StringAttribute("Guid",value,"System.Runtime.InteropServices")
   static member ComVisible(?value) = Attribute.BoolAttribute("ComVisible",defaultArg value false,"System.Runtime.InteropServices")
   static member CLSCompliant(?value) = Attribute.BoolAttribute("CLSCompliant",defaultArg value false,"System")
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
 
/// Creates a C# AssemblyInfo file with the given attributes
let CreateCSharpAssemblyInfo outputFileName attributes =
    traceStartTask "AssemblyInfo" outputFileName

    (getDependencies attributes |> List.map (sprintf "using %s;")) @ [""] @
    (attributes |> Seq.toList |> List.map (fun (attr:Attribute) -> sprintf "[assembly: %sAttribute(%s)]" attr.Name attr.Value))
    |> writeToFile outputFileName
    
    traceEndTask "AssemblyInfo" outputFileName

/// Creates a F# AssemblyInfo file with the given attributes
let CreateFSharpAssemblyInfo outputFileName attributes =
    traceStartTask "AssemblyInfo" outputFileName

    ["module AssemblyInfo"] @
    (getDependencies attributes |> List.map (sprintf "open %s")) @ [""] @
    (attributes |> Seq.toList |> List.map (fun (attr:Attribute) -> sprintf "[<assembly: %sAttribute(%s)>]" attr.Name attr.Value)) @ [""] @
    ["()"]
    |> writeToFile outputFileName

    traceEndTask "AssemblyInfo" outputFileName