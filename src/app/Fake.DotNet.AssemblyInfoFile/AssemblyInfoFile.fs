namespace Fake.DotNet

open System
open System.IO
open System.Text.RegularExpressions

module internal Helper =
    open Fake.Core
    let internal assemblyVersionRegex = String.getRegEx @"([0-9]+\.)+[0-9]+"

    // matches [assembly: name(value)] and captures "name" and "value" as named captures. Variations for C#, F#, C++ and VB
    let regexAttrNameValueCs =
        @"^\s*\[\s*assembly:\s*(?<name>\w+?)\s*\((?<value>.*)\)\s*\]\s*$"

    let regexAttrNameValueFs =
        @"^\s*\[\<\s*assembly:\s*(?<name>\w+?)\s*\((?<value>.*)\)\s*\>\]\s*$"

    let regexAttrNameValueCpp =
        @"^\s*\[\s*assembly:\s*(?<name>\w+?)\s*\((?<value>.*)\)\s*\]\s*;\s*$"

    let regexAttrNameValueVb =
        @"^\s*\<\s*assembly:\s*(?<name>\w+?)\s*\((?<value>.*)\)\s*\>\s*$"

    // matches [assembly: name(value)] but only captures the value. Variations for C#, F#, C++ and VB
    let regexAttrValueCs name =
        @"(?<=^\s*\[\s*assembly:\s*"
        + name
        + @"(?:Attribute)?\s*\()" // look-behind for "[assembly: name[Attribute]("
        + @"(.*)" // value
        + @"(?=\)\s*\]\s*$)" // look-ahead for ")]"

    let regexAttrValueFs name =
        @"(?<=^\s*\[\<\s*assembly:\s*"
        + name
        + @"(?:Attribute)?\s*\()" // look-behind for "[<assembly: name[Attribute]("
        + @"(.*)" // value
        + @"(?=\)\s*\>\]\s*$)" // look-ahead for ")>]"

    let regexAttrValueCpp name =
        @"(?<=^\s*\[\s*assembly:\s*"
        + name
        + @"(?:Attribute)?\s*\()" // look-behind for "[assembly: name[Attribute]("
        + @"(.*)" // value
        + @"(?=\)\s*\]\s*;\s*$)" // look-ahead for ")];"

    let regexAttrValueVb name =
        @"(?<=^\s*\<\s*assembly:\s*"
        + name
        + @"(?:Attribute)?\s*\()" // look-behind for "<assembly: name[Attribute]("
        + @"(.*)" // value
        + @"(?=\)\s*\>\s*$)" // look-ahead for ")>"

    let NormalizeVersion version =
        let m = assemblyVersionRegex.Match(version)

        if m.Captures.Count > 0 then
            m.Captures[0].Value
        else
            version

/// <summary>
/// Represents options for configuring the emission of AssemblyInfo
/// </summary>
type AssemblyInfoFileConfig(generateClass: bool, ?emitResharperSupressions: bool, ?useNamespace: string)
// If set to true then SuppressMessage attributes for the Resharper warnings "RedundantNameQualifier",
// "UnusedMember.Global" and "BuiltInTypeReferenceStyle" will be generated; optional, defaults to false
// The optional namespace into which assembly info will be generated; defaults to "System".
 =
    member _.GenerateClass = generateClass

    member _.UseNamespace =
        match useNamespace with
        | Some n -> n
        | None -> "System"

    member _.EmitResharperSuppressions =
        match emitResharperSupressions with
        | Some n -> generateClass && n
        | None -> false

    static member Default = AssemblyInfoFileConfig(true)

/// <namespacedoc>
/// <summary>
/// DotNet namespace contains tasks to interact with DotNet ecosystem, including DotNet host (cli), F# interactive,
/// testing tools and more.
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains tasks to generate AssemblyInfo files for C# and F#.
/// There is also a tutorial about the <a href="/articles/dotnet-assemblyinfo.html">AssemblyInfo tasks</a> available.
/// </summary>
[<RequireQualifiedAccess>]
module AssemblyInfo =

    /// <summary>
    /// Represents AssemblyInfo attributes
    /// </summary>
    type Attribute(name, value, inNamespace, staticPropName, staticPropType, staticPropValue) =
        /// the name of the attribute
        member _.Name = name

        /// the value for the attribute
        member _.Value = value

        /// the namespace of the attribute
        member _.Namespace = inNamespace

        /// static property name
        member _.StaticPropertyName = staticPropName

        /// static property type
        member _.StaticPropertyType = staticPropType

        /// static property value
        member _.StaticPropertyValue = staticPropValue

        /// Create a new assembly info attribute with given data
        new(name, value, inNamespace, staticPropType) = Attribute(name, value, inNamespace, name, staticPropType, value)

    let private quote (value: string) =
        if value.StartsWith("\"") && value.EndsWith("\"") then
            value
        else
            sprintf "\"%s\"" value

    /// <summary>
    /// Creates a simple attribute with string values. Used as base for other attributes
    /// </summary>
    ///
    /// <param name="name">the attribute name</param>
    /// <param name="value">the attribute value</param>
    /// <param name="inNamespace">the attribute namespace</param>
    /// <param name="staticName">static property name</param>
    /// <param name="staticValue">static property value</param>
    let StringAttributeWithStatic (name, value, inNamespace, staticName, staticValue) =
        let quotedValue = quote value
        let quotedStaticValue = quote staticValue
        Attribute(name, quotedValue, inNamespace, staticName, typeof<string>.FullName, quotedStaticValue)

    /// <summary>
    /// Create a string attribute
    /// </summary>
    ///
    /// <param name="name">the attribute name</param>
    /// <param name="value">the attribute value</param>
    /// <param name="inNamespace">the attribute namespace</param>
    let StringAttribute (name, value, inNamespace) =
        let quotedValue = quote value
        Attribute(name, quotedValue, inNamespace, name, typeof<string>.FullName, quotedValue)

    /// <summary>
    /// Creates a simple attribute with boolean values. Used as base for other attributes
    /// </summary>
    ///
    /// <param name="name">the attribute name</param>
    /// <param name="value">the attribute value</param>
    /// <param name="inNamespace">the attribute namespace</param>
    let BoolAttribute (name, value, inNamespace) =
        Attribute(name, sprintf "%b" value, inNamespace, typeof<bool>.FullName)

    /// <summary>
    /// Creates an attribute which holds the company information
    /// </summary>
    ///
    /// <param name="value">the company attribute value</param>
    let Company value =
        StringAttribute("AssemblyCompany", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the product name
    /// </summary>
    ///
    /// <param name="value">the product attribute value</param>
    let Product value =
        StringAttribute("AssemblyProduct", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the copyright information
    /// </summary>
    ///
    /// <param name="value">the copyright attribute value</param>
    let Copyright value =
        StringAttribute("AssemblyCopyright", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the product title
    /// </summary>
    ///
    /// <param name="value">the title attribute value</param>
    let Title value =
        StringAttribute("AssemblyTitle", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the product description
    /// </summary>
    ///
    /// <param name="value">the description attribute value</param>
    let Description value =
        StringAttribute("AssemblyDescription", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the assembly culture information
    /// </summary>
    ///
    /// <param name="value">the culture attribute value</param>
    let Culture value =
        StringAttribute("AssemblyCulture", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the assembly configuration
    /// </summary>
    ///
    /// <param name="value">the configuration attribute value</param>
    let Configuration value =
        StringAttribute("AssemblyConfiguration", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the trademark
    /// </summary>
    ///
    /// <param name="value">the trademark attribute value</param>
    let Trademark value =
        StringAttribute("AssemblyTrademark", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the assembly version
    /// </summary>
    ///
    /// <param name="value">the version attribute value</param>
    let Version value =
        StringAttribute("AssemblyVersion", Helper.NormalizeVersion value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the assembly key file
    /// </summary>
    ///
    /// <param name="value">the key file attribute value</param>
    let KeyFile value =
        StringAttribute("AssemblyKeyFile", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the assembly key name
    /// </summary>
    ///
    /// <param name="value">the key name attribute value</param>
    let KeyName value =
        StringAttribute("AssemblyKeyName", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the "InternalVisibleTo" data
    /// </summary>
    ///
    /// <param name="value">the internals visible to attribute value</param>
    let InternalsVisibleTo value =
        StringAttribute("InternalsVisibleTo", value, "System.Runtime.CompilerServices")

    /// <summary>
    /// Creates an attribute which holds the assembly file version
    /// </summary>
    ///
    /// <param name="value">the file version attribute value</param>
    let FileVersion value =
        StringAttribute("AssemblyFileVersion", Helper.NormalizeVersion value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds an assembly information version
    /// </summary>
    ///
    /// <param name="value">the informational version attribute value</param>
    let InformationalVersion value =
        StringAttribute("AssemblyInformationalVersion", value, "System.Reflection")

    /// <summary>
    /// Creates an attribute which holds the Guid
    /// </summary>
    ///
    /// <param name="value">the guid attribute value</param>
    let Guid value =
        StringAttribute("Guid", value, "System.Runtime.InteropServices")

    /// <summary>
    /// Creates an attribute which specifies if the assembly is visible via COM
    /// </summary>
    ///
    /// <param name="value">the COM attribute value</param>
    let ComVisible value =
        BoolAttribute("ComVisible", value, "System.Runtime.InteropServices")

    /// <summary>
    /// Creates an attribute which specifies if the assembly is CLS compliant
    /// </summary>
    ///
    /// <param name="value">the CLS Compliant attribute value</param>
    let CLSCompliant value =
        BoolAttribute("CLSCompliant", value, "System")

    /// <summary>
    /// Creates an attribute which specifies if the assembly uses delayed signing
    /// </summary>
    ///
    /// <param name="value">the delay sign attribute value</param>
    let DelaySign value =
        BoolAttribute("AssemblyDelaySign", value, "System.Reflection")

    /// <summary>
    /// Create an attribute which specifies metadata about the assembly
    /// </summary>
    ///
    /// <param name="name">the attribute name</param>
    /// <param name="value">the attribute value</param>
    let Metadata (name, value) =
        let nameValue = sprintf "\"%s\",\"%s\"" name value

        StringAttributeWithStatic(
            "AssemblyMetadata",
            nameValue,
            "System.Reflection",
            sprintf "AssemblyMetadata_%s" (name.Replace(" ", "_")),
            value
        )

/// <summary>
/// Contains tasks to generate AssemblyInfo files for C# and F#.
/// </summary>
[<RequireQualifiedAccess>]
module AssemblyInfoFile =
    open Helper
    open Fake.Core
    open Fake.IO

    type Attribute = AssemblyInfo.Attribute

    let private writeToFile outputFileName (lines: seq<string>) =
        let fi = FileInfo.ofPath outputFileName

        if fi.Exists then
            fi.Delete()

        let dirName = Path.GetDirectoryName(outputFileName)

        if not (String.isNullOrEmpty dirName) then
            Directory.CreateDirectory(dirName) |> ignore

        use f = fi.Open(FileMode.Create)
        use writer = new StreamWriter(f, System.Text.Encoding.UTF8)
        lines |> Seq.iter writer.WriteLine

    let private getDependencies attributes =
        attributes
        |> Seq.map (fun (attr: Attribute) -> attr.Namespace)
        |> Seq.filter (fun x -> not (String.IsNullOrWhiteSpace x))
        |> Set.ofSeq
        |> Seq.toList

    let private getAssemblyVersionInfo attributes =
        match
            attributes
            |> Seq.tryFind (fun (attr: Attribute) -> attr.Name = "AssemblyVersion")
        with
        | Some attr -> attr.Value
        | None _ -> "\"" + BuildServer.buildVersion + "\""

    let private getAssemblyInformationalVersion attributes =
        match
            attributes
            |> Seq.tryFind (fun (attr: Attribute) -> attr.Name = "AssemblyInformationalVersion")
        with
        | Some attr -> attr.Value
        | None _ -> getAssemblyVersionInfo attributes

    let private getSortedAndNumberedAttributes (attrs: seq<Attribute>) =
        attrs
        |> Seq.mapi (fun index a -> index, a)
        |> Seq.groupBy (fun (_, attr) -> attr.StaticPropertyName)
        |> Seq.collect (fun (_, group) -> group |> Seq.mapi (fun i a -> i, a))
        |> Seq.sortBy (fun (_, (index, _)) -> index)
        |> Seq.map (fun (id, (_, attr)) ->
            let name =
                if id = 0 then
                    attr.StaticPropertyName
                else
                    sprintf "%s_%d" attr.StaticPropertyName id

            (name, attr.StaticPropertyType, attr.StaticPropertyValue))

    /// <summary>
    /// Creates a C# AssemblyInfo file with the given attributes and configuration.
    /// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve
    /// the current version no. from inside of an assembly.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    /// <param name="config">The config file</param>
    let createCSharpWithConfig outputFileName attributes (config: AssemblyInfoFileConfig) =
        let generateClass, useNamespace, emitResharperSupressions =
            config.GenerateClass, config.UseNamespace, config.EmitResharperSuppressions

        let dependencies =
            if emitResharperSupressions then
                [ "System.Diagnostics.CodeAnalysis" ] @ (getDependencies attributes)
                |> Set.ofSeq
                |> Seq.toList
            else
                getDependencies attributes

        let attributeLines =
            [ "// Auto-Generated by FAKE; do not edit"; "// <auto-generated/>" ]
            @ (dependencies |> List.map (sprintf "using %s;"))
              @ [ "" ]
                @ (attributes
                   |> Seq.toList
                   |> List.map (fun (attr: Attribute) -> sprintf "[assembly: %s(%s)]" attr.Name attr.Value))

        let sourceLines =
            if generateClass then
                let consts =
                    attributes
                    |> getSortedAndNumberedAttributes
                    |> Seq.map (fun (name, attrtype, value) ->
                        sprintf "        internal const %s %s = %s;" attrtype name value)
                    |> Seq.toList

                let resharperSuppressions =
                    if emitResharperSupressions then
                        [ "RedundantNameQualifier"; "UnusedMember.Global"; "BuiltInTypeReferenceStyle" ]
                        |> List.map (fun line -> sprintf "    [SuppressMessage(\"ReSharper\", \"%s\")]" line)
                    else
                        []

                (sprintf "namespace %s {" useNamespace) :: resharperSuppressions
                @ [ "    internal static class AssemblyVersionInformation {" ]
                  @ consts @ [ "    }"; "}" ]
            else
                []

        attributeLines @ sourceLines |> writeToFile outputFileName

    /// <summary>
    /// Creates a F# AssemblyInfo file with the given attributes and configuration.
    /// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve
    /// the current version no. from inside of an assembly.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    /// <param name="config">The config file</param>
    let createFSharpWithConfig outputFileName attributes (config: AssemblyInfoFileConfig) =
        let generateClass, useNamespace = config.GenerateClass, config.UseNamespace

        let sourceLines =
            [ yield "// Auto-Generated by FAKE; do not edit"
              yield sprintf "namespace %s" useNamespace
              yield! getDependencies attributes |> Seq.map (sprintf "open %s")
              yield ""
              yield!
                  attributes
                  |> Seq.map (fun (attr: Attribute) -> sprintf "[<assembly: %sAttribute(%s)>]" attr.Name attr.Value)

              yield "do ()"
              yield ""

              if generateClass then
                  yield "module internal AssemblyVersionInformation ="

                  yield!
                      // it might be that a specific assembly has multiple attributes of the same name
                      // if more than one occurences appear, append numerical suffixes to avoid compile errors
                      attributes
                      |> getSortedAndNumberedAttributes
                      |> Seq.map (fun (name, _, value) -> sprintf "    let [<Literal>] %s = %s" name value) ]

        sourceLines |> writeToFile outputFileName

    /// <summary>
    /// Creates a VB AssemblyInfo file with the given attributes and configuration.
    /// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve
    /// the current version no. from inside of an assembly.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    /// <param name="config">The config file</param>
    let createVisualBasicWithConfig outputFileName attributes (config: AssemblyInfoFileConfig) =
        let generateClass, _ = config.GenerateClass, config.UseNamespace

        let attributeLines =
            [ "'//' Auto-Generated by FAKE; do not edit"; "'//' <auto-generated/>" ]
            @ (getDependencies attributes |> List.map (sprintf "Imports %s"))
              @ [ "" ]
                @ (attributes
                   |> Seq.toList
                   |> List.map (fun (attr: Attribute) -> sprintf "<assembly: %s(%s)>" attr.Name attr.Value))

        let sourceLines =
            if generateClass then
                let consts =
                    attributes
                    |> getSortedAndNumberedAttributes
                    |> Seq.map (fun (name, attrtype, value) ->
                        sprintf "    Friend Const %s As %s = %s" name attrtype value)
                    |> Seq.toList

                "Friend NotInheritable Class AssemblyVersionInformation" :: consts
                @ [ "End Class" ]
            else
                []

        attributeLines @ sourceLines |> writeToFile outputFileName

    /// <summary>
    /// Creates a C++/CLI AssemblyInfo file with the given attributes and configuration.
    /// Does not generate an AssemblyVersionInformation class.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    /// <param name="config">The config file</param>
    let createCppCliWithConfig outputFileName attributes (config: AssemblyInfoFileConfig) =
        let _, _ = config.GenerateClass, config.UseNamespace
        //C++/CLI namespaces cannot be fully qualified; you must
        // namespace Namespace1 {  namespace Namespace2 { }} //etc

        let attributeLines =
            [ "'//' Auto-Generated by FAKE; do not edit"; "'//' <auto-generated/>" ]
            @ (getDependencies attributes
               |> List.map (
                   String.collect (fun c -> if c = '.' then "::" else c.ToString())
                   >> sprintf "using namespace %s;"
               ))
              @ [ "" ]
                @ (attributes
                   |> Seq.toList
                   |> List.map (fun (attr: Attribute) -> sprintf "[assembly:%sAttribute(%s)];" attr.Name attr.Value))

        attributeLines |> writeToFile outputFileName

    /// <summary>
    /// Creates a C# AssemblyInfo file with the given attributes.
    /// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve the
    /// current version no. from inside of an assembly.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    let createCSharp outputFileName attributes =
        createCSharpWithConfig outputFileName attributes AssemblyInfoFileConfig.Default

    /// <summary>
    /// Creates a F# AssemblyInfo file with the given attributes.
    /// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve
    /// the current version no. from inside of an assembly.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    let createFSharp outputFileName attributes =
        createFSharpWithConfig outputFileName attributes AssemblyInfoFileConfig.Default

    /// <summary>
    /// Creates a VB AssemblyInfo file with the given attributes.
    /// The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be used to retrieve
    /// the current version no. from inside of an assembly.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    let createVisualBasic outputFileName attributes =
        createVisualBasicWithConfig outputFileName attributes AssemblyInfoFileConfig.Default

    /// <summary>
    /// Creates a C++/CLI AssemblyInfo file with the given attributes.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    let createCppCli outputFileName attributes =
        createCppCliWithConfig outputFileName attributes AssemblyInfoFileConfig.Default

    let private (|Suffix|_|) (p: string) (s: string) = if s.EndsWith p then Some(s) else None

    let private removeAtEnd (textToRemove: string) (text: string) =
        if text.EndsWith(textToRemove) then
            text.Substring(0, text.Length - textToRemove.Length)
        else
            text

    /// <summary>
    /// Creates an AssemblyInfo file based in the correct language based on the file name with the given attributes
    /// and configuration. The generated AssemblyInfo file contains an AssemblyVersionInformation class which can be
    /// used to retrieve the current version no. from inside of an assembly.
    /// </summary>
    ///
    /// <param name="outputFileName">The output file name</param>
    /// <param name="attributes">Sequence of attributes</param>
    /// <param name="config">The config file</param>
    let create (outputFileName: string) (attributes: seq<Attribute>) (config: AssemblyInfoFileConfig option) =
        match outputFileName.ToLower(), config with
        | Suffix ".cs" _, Some x -> createCSharpWithConfig outputFileName attributes x
        | Suffix ".cs" _, None -> createCSharp outputFileName attributes
        | Suffix ".fs" _, Some x -> createFSharpWithConfig outputFileName attributes x
        | Suffix ".fs" _, None -> createFSharp outputFileName attributes
        | Suffix ".vb" _, Some x -> createVisualBasicWithConfig outputFileName attributes x
        | Suffix ".vb" _, None -> createVisualBasic outputFileName attributes
        | Suffix ".cpp" _, Some x -> createCppCliWithConfig outputFileName attributes x
        | Suffix ".cpp" _, None -> createCppCli outputFileName attributes
        | _ -> failwithf "Assembly info file type not supported: %s" outputFileName

    /// <summary>
    /// Read attributes from an AssemblyInfo file and return as a sequence of Attribute.
    /// </summary>
    ///
    /// <param name="assemblyInfoFile">The file to read attributes from. Language C#, F#, VB or C++ is
    /// determined from the extension.</param>
    let getAttributes assemblyInfoFile =
        let text = File.ReadAllText assemblyInfoFile

        // VB.NET is case-insensitive. Handle assembly attributes accordingly
        let regex, additionalRegexOptions =
            match assemblyInfoFile.ToLower() with
            | Suffix ".cs" _ -> (regexAttrNameValueCs, RegexOptions.Multiline ||| RegexOptions.None)
            | Suffix ".fs" _ -> (regexAttrNameValueFs, RegexOptions.Multiline ||| RegexOptions.None)
            | Suffix ".vb" _ -> (regexAttrNameValueVb, RegexOptions.Multiline ||| RegexOptions.IgnoreCase)
            | Suffix ".cpp" _ -> (regexAttrNameValueCpp, RegexOptions.Multiline ||| RegexOptions.None)
            | _ -> failwithf "Assembly info file type not supported: %s" assemblyInfoFile

        let assemblyMap (m: Match) =
            let v = m.Groups[ "value" ].Value.Trim([| '"' |])
            let n = m.Groups["name"].Value |> removeAtEnd "Attribute"

            let t =
                if v = "true" || v = "false" then
                    typeof<bool>.FullName
                else
                    typeof<string>.FullName

            match n.ToLower() with
            | "assemblycompany" -> AssemblyInfo.Company(v)
            | "assemblyproduct" -> AssemblyInfo.Product(v)
            | "assemblycopyright" -> AssemblyInfo.Copyright(v)
            | "assemblytitle" -> AssemblyInfo.Title(v)
            | "assemblydescription" -> AssemblyInfo.Description(v)
            | "assemblyculture" -> AssemblyInfo.Culture(v)
            | "assemblyconfiguration" -> AssemblyInfo.Configuration(v)
            | "assemblytrademark" -> AssemblyInfo.Trademark(v)
            | "assemblyversion" -> AssemblyInfo.Version(v)
            | "assemblyfileversion" -> AssemblyInfo.FileVersion(v)
            | "assemblykeyfile" -> AssemblyInfo.KeyFile(v)
            | "assemblykeyname" -> AssemblyInfo.KeyName(v)
            | "assemblyinformationalversion" -> AssemblyInfo.InformationalVersion(v)
            | "assemblydelaysign" -> AssemblyInfo.DelaySign(v |> bool.Parse)
            | "internalsvisibleto" -> AssemblyInfo.InternalsVisibleTo(v)
            | "guid" -> AssemblyInfo.Guid(v)
            | "comvisible" -> AssemblyInfo.ComVisible(v |> bool.Parse)
            | "clscompliant" -> AssemblyInfo.CLSCompliant(v |> bool.Parse)
            | _ -> Attribute(n, v, "", t)

        Regex.Matches(text, regex, additionalRegexOptions)
        |> Seq.cast<Match>
        |> Seq.map assemblyMap

    /// <summary>
    /// Read a single attribute from an AssemblyInfo file.
    /// </summary>
    ///
    /// <param name="attrName">Name of the attribute without "Attribute" at the end.</param>
    /// <param name="assemblyInfoFile">The file to read from. Language C#, F#, VB or C++ is determined from
    /// the extension.</param>
    let getAttribute attrName assemblyInfoFile =
        assemblyInfoFile |> getAttributes |> Seq.tryFind (fun a -> a.Name = attrName)

    /// <summary>
    /// Read the value of a single attribute from an AssemblyInfo file. Note that string values are returned
    /// with surrounding "".
    /// </summary>
    ///
    /// <param name="attrName">Name of the attribute without "Attribute" at the end.</param>
    /// <param name="assemblyInfoFile">The file to read from. Language C#, F#, VB or C++ is determined from
    /// the extension.</param>
    let getAttributeValue attrName assemblyInfoFile =
        match getAttribute attrName assemblyInfoFile with
        | Some attr -> Some attr.Value
        | None -> None

    let private updateAttr regexFactory regexOptions text (attribute: Attribute) =
        let regex = regexFactory attribute.Name

        let m = Regex.Match(text, regex, regexOptions)

        // Replace if found with different value
        if m.Success && m.Value <> attribute.Value then
            Trace.tracefn "Attribute '%s' updated: %s" attribute.Name attribute.Value
            Regex.Replace(text, regex, attribute.Value, regexOptions)

        // Do nothing if found with the same value
        elif m.Success then
            Trace.tracefn "Attribute '%s' is already correct: %s" attribute.Name attribute.Value
            text

        // Fail if not found
        else
            failwithf "Attribute '%s' not found" attribute.Name

    /// <summary>
    /// Update a set of attributes in an AssemblyInfo file. Fails if any attribute is not found.
    /// </summary>
    ///
    /// <param name="assemblyInfoFile">The file to update. Language C#, F#, VB or C++ is determined from the
    /// extension.</param>
    /// <param name="attributes">The Attributes that should be updated matched on Name (Namespace is not used).</param>
    let updateAttributes assemblyInfoFile (attributes: seq<Attribute>) =
        Trace.tracefn "Updating attributes in: %s" assemblyInfoFile

        // VB.NET is case-insensitive. Handle assembly attributes accordingly
        let regexFactory, regexOptions =
            match assemblyInfoFile.ToLower() with
            | Suffix ".cs" _ -> (regexAttrValueCs, RegexOptions.Multiline ||| RegexOptions.None)
            | Suffix ".fs" _ -> (regexAttrValueFs, RegexOptions.Multiline ||| RegexOptions.None)
            | Suffix ".vb" _ -> (regexAttrValueVb, RegexOptions.Multiline ||| RegexOptions.IgnoreCase)
            | Suffix ".cpp" _ -> (regexAttrValueCpp, RegexOptions.Multiline ||| RegexOptions.None)
            | _ -> failwithf "Assembly info file type not supported: %s" assemblyInfoFile

        let text = File.ReadAllText assemblyInfoFile
        let newText = attributes |> Seq.fold (updateAttr regexFactory regexOptions) text

        File.WriteAllText(assemblyInfoFile, newText)
