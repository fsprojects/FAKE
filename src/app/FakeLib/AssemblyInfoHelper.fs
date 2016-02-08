[<AutoOpen>]
/// Generates an AssemblyInfo file
module Fake.AssemblyInfoHelper

open System
open System.Collections.Generic
open System.Reflection
open System.Text
open System.CodeDom
open System.CodeDom.Compiler
open System.IO

type CodeLanguage = 
    | CSharp
    | FSharp
    | VisualBasic

type AssemblyInfoParams = 
    { OutputFileName : string
      ComVisible : bool option
      CLSCompliant : bool option
      Guid : string
      CodeLanguage : CodeLanguage
      AssemblyTitle : string
      AssemblyDescription : string
      AssemblyConfiguration : string
      AssemblyCompany : string
      AssemblyProduct : string
      AssemblyCopyright : string
      AssemblyTrademark : string
      AssemblyCulture : string
      AssemblyVersion : string
      AssemblyFileVersion : string
      AssemblyInformationalVersion : string
      AssemblyKeyFile : string
      AssemblyKeyName : string
      AssemblyDelaySign : bool option
      GenerateClass : bool }

/// AssemblyInfo default params
let AssemblyInfoDefaults = 
    { OutputFileName = String.Empty
      ComVisible = Some false
      CLSCompliant = Some false
      Guid = Guid.NewGuid().ToString()
      CodeLanguage = CSharp
      AssemblyTitle = String.Empty
      AssemblyDescription = String.Empty
      AssemblyConfiguration = String.Empty
      AssemblyCompany = String.Empty
      AssemblyProduct = String.Empty
      AssemblyCopyright = String.Empty
      AssemblyTrademark = String.Empty
      AssemblyCulture = String.Empty
      AssemblyVersion = "1.0.0.0"
      AssemblyFileVersion = String.Empty
      AssemblyInformationalVersion = String.Empty
      AssemblyKeyFile = String.Empty
      AssemblyKeyName = String.Empty
      AssemblyDelaySign = Some false
      GenerateClass = false }

/// generates the assembly info file
let generateFile param (attributes : Dictionary<string, string>) imports (writer : TextWriter) = 
    let provider, outputFileName = 
        match param.CodeLanguage with
        | CSharp -> 
            let provider = new Microsoft.CSharp.CSharpCodeProvider() :> CodeDomProvider
            provider, Path.ChangeExtension(param.OutputFileName, ".cs")
        | FSharp -> failwith "No CodeDom available."
        | VisualBasic -> 
            let provider = new Microsoft.VisualBasic.VBCodeProvider() :> CodeDomProvider
            provider, Path.ChangeExtension(param.OutputFileName, ".vb")
    
    let codeCompileUnit = new CodeCompileUnit()
    let codeNamespace = new CodeNamespace()
    imports |> Seq.iter (fun import -> new CodeNamespaceImport(import) |> codeNamespace.Imports.Add)
    codeCompileUnit.Namespaces.Add codeNamespace |> ignore
    let addString = 
        match param.CodeLanguage with
        | FSharp -> 
            (attributes
             |> Seq.map (fun attr -> 
                    match bool.TryParse attr.Value with
                    | true, value -> sprintf "[<assembly: %s (%A)>]" attr.Key value
                    | _ -> sprintf "[<assembly: %s (\"%s\")>]" attr.Key attr.Value)
             |> toLines) + "\r\n()"
        | _ -> 
            for attr in attributes do
                // create new assembly-level attribute
                let codeAttributeDeclaration = new CodeAttributeDeclaration(attr.Key)
                let codeAttrArg value = new CodeAttributeArgument(new CodePrimitiveExpression(value))
                match attr.Key with
                | "CLSCompliant" | "AssemblyDelaySign" | "ComVisible" -> 
                    match bool.TryParse attr.Value with
                    | true, value -> 
                        codeAttributeDeclaration.Arguments.Add(codeAttrArg value) |> ignore
                        codeCompileUnit.AssemblyCustomAttributes.Add(codeAttributeDeclaration) |> ignore
                    | _ -> ()
                | _ -> 
                    codeAttributeDeclaration.Arguments.Add(codeAttrArg attr.Value) |> ignore
                    codeCompileUnit.AssemblyCustomAttributes.Add(codeAttributeDeclaration) |> ignore
            String.Empty
    if param.GenerateClass then 
        // Create Class Declaration
        let thisAssemblyType = new CodeTypeDeclaration("ThisAssembly")
        thisAssemblyType.IsClass <- true
        thisAssemblyType.IsPartial <- true
        thisAssemblyType.TypeAttributes <- TypeAttributes.NotPublic ||| TypeAttributes.Sealed
        let privateConstructor = new CodeConstructor()
        privateConstructor.Attributes <- MemberAttributes.Private
        thisAssemblyType.Members.Add privateConstructor |> ignore
        attributes
        |> Seq.filter (fun attr -> 
               match attr.Key with
               | "CLSCompliant" | "AssemblyDelaySign" | "ComVisible" | "AssemblyKeyFile" -> false
               | _ -> true)
        |> Seq.iter (fun attr -> 
               let field = new CodeMemberField(typeof<string>, attr.Key)
               field.Attributes <- MemberAttributes.Assembly ||| MemberAttributes.Const
               field.InitExpression <- new CodePrimitiveExpression(attr.Value)
               thisAssemblyType.Members.Add field |> ignore)
        codeNamespace.Types.Add thisAssemblyType |> ignore
    let options = new CodeGeneratorOptions()
    provider.GenerateCodeFromCompileUnit(codeCompileUnit, writer, options)
    if addString <> String.Empty then writer.WriteLine addString

/// Generates an AssemblyInfo file for projects
/// **Obsolete - Please use the new AssemblyInfoFile tasks**
[<Obsolete("Please use the new AssemblyInfoFile tasks")>]
let AssemblyInfo setParams = 
    let param' = setParams AssemblyInfoDefaults
    traceStartTask "AssemblyInfo" param'.OutputFileName
    let param'' = 
        if param'.AssemblyFileVersion <> String.Empty then param'
        else { param' with AssemblyFileVersion = param'.AssemblyVersion }
    
    let param = 
        if param''.AssemblyProduct <> String.Empty then param''
        else { param'' with AssemblyProduct = param''.AssemblyTitle }
    
    if isNullOrEmpty param.OutputFileName then 
        failwith "You have to specify the OutputFileName for the AssemblyInfo task."
    let attributes = new Dictionary<_, _>()
    
    let writeAttribute name attribute = 
        if attribute <> String.Empty then attributes.Add(name, attribute)
    
    let writeAttributeOption name = 
        function 
        | Some attribute -> 
            try 
                attribute.ToString() |> writeAttribute name
            with exn -> ()
        | _ -> ()
    
    writeAttributeOption "ComVisible" param.ComVisible
    writeAttributeOption "CLSCompliant" param.CLSCompliant
    writeAttribute "Guid" param.Guid
    writeAttribute "AssemblyTitle" param.AssemblyTitle
    writeAttribute "AssemblyDescription" param.AssemblyDescription
    writeAttribute "AssemblyConfiguration" param.AssemblyConfiguration
    writeAttribute "AssemblyCompany" param.AssemblyCompany
    writeAttribute "AssemblyProduct" param.AssemblyProduct
    writeAttribute "AssemblyCopyright" param.AssemblyCopyright
    writeAttribute "AssemblyTrademark" param.AssemblyTrademark
    writeAttribute "AssemblyCulture" param.AssemblyCulture
    writeAttribute "AssemblyVersion" param.AssemblyVersion
    writeAttribute "AssemblyFileVersion" param.AssemblyFileVersion
    writeAttribute "AssemblyInformationalVersion" param.AssemblyInformationalVersion
    writeAttribute "AssemblyKeyFile" param.AssemblyKeyFile
    writeAttribute "AssemblyKeyName" param.AssemblyKeyName
    writeAttributeOption "AssemblyDelaySign" param.AssemblyDelaySign
    let imports = [ "System"; "System.Reflection"; "System.Runtime.CompilerServices"; "System.Runtime.InteropServices" ]
    let fi = fileInfo param.OutputFileName
    use writer = File.CreateText param.OutputFileName
    let write (x : string) = writer.WriteLine x
    match param.CodeLanguage with
    | FSharp -> 
        write (sprintf "module %s.AssemblyInfo" fi.Directory.Name)
        write "#nowarn \"49\" // uppercase argument names"
        write "#nowarn \"67\" // this type test or downcast will always hold"
        write "#nowarn \"66\" // tis upast is unnecessary - the types are identical"
        write "#nowarn \"58\" // possible incorrect indentation.."
        write "#nowarn \"57\" // do not use create_DelegateEvent"
        write "#nowarn \"51\" // address-of operator can occur in the code"
        write "open System"
        write "open System.Reflection"
        write "open System.Runtime.CompilerServices"
        write "open System.Runtime.InteropServices"
        write "exception ReturnException183c26a427ae489c8fd92ec21a0c9a59 of obj"
        write "exception ReturnNoneException183c26a427ae489c8fd92ec21a0c9a59"
        attributes |> Seq.iter (fun attr -> 
                          match bool.TryParse attr.Value with
                          | true, value -> write (sprintf "\n[<assembly: %s (%A)>]" attr.Key value)
                          | false, _ -> write (sprintf "\n[<assembly: %s (\"%s\")>]" attr.Key (attr.Value.ToString())))
        write "\n()"
    | _ -> generateFile param attributes imports writer
    writer.Flush()
    writer.Close()
    tracefn "Created AssemblyInfo file \"%s\"." param.OutputFileName
    traceEndTask "AssemblyInfo" param'.OutputFileName

type AssemblyInfoReplacementParams = 
    { OutputFileName : string
      AssemblyVersion : string
      AssemblyFileVersion : string
      AssemblyInformationalVersion : string
      AssemblyCompany : string
      AssemblyCopyright : string
      AssemblyConfiguration : string
      AssemblyMetadata : (string * string) list }

/// AssemblyInfoReplacement default params
let AssemblyInfoReplacementDefaults = 
    { OutputFileName = null
      AssemblyConfiguration = null
      AssemblyVersion = null
      AssemblyFileVersion = null
      AssemblyInformationalVersion = null
      AssemblyCompany = null
      AssemblyCopyright = null
      AssemblyMetadata = [] }

let ReplaceAssemblyInfoVersions param = 
    let (parameters : AssemblyInfoReplacementParams) = param AssemblyInfoReplacementDefaults
    
    let replaceAttribute attributeName value line = 
        if isNullOrEmpty value then line
        else regex_replace (sprintf "%s\\s*[(][^)]*[)]" attributeName) (sprintf "%s(\"%s\")" attributeName value) line

    let rec replaceMetadataAttributes metadata line =
        let replaceSingleMetadataAttribute key value line =
            if isNullOrEmpty key then line
            else
                regex_replace
                    (sprintf "AssemblyMetadata\\s*\\(\\s*\"%s\"\\s*,[^)]*\\)" key)
                    (sprintf "AssemblyMetadata(\"%s\", \"%s\")" key value)
                    line
        match metadata with
        | (key, value) :: rest ->
            line
            |> replaceSingleMetadataAttribute key value
            |> replaceMetadataAttributes rest
        | _ -> line

    let replaceLine line = 
        line
        |> replaceAttribute "AssemblyVersion" parameters.AssemblyVersion
        |> replaceAttribute "AssemblyConfiguration" parameters.AssemblyConfiguration
        |> replaceAttribute "AssemblyFileVersion" parameters.AssemblyFileVersion
        |> replaceAttribute "AssemblyInformationalVersion" parameters.AssemblyInformationalVersion
        |> replaceAttribute "AssemblyCompany" parameters.AssemblyCompany
        |> replaceAttribute "AssemblyCopyright" parameters.AssemblyCopyright
        |> replaceMetadataAttributes parameters.AssemblyMetadata
    
    let encoding = Text.Encoding.GetEncoding "UTF-8"

    let fileContent = File.ReadAllLines(parameters.OutputFileName, encoding)

    use writer = new StreamWriter(parameters.OutputFileName, false, encoding)

    fileContent
    |> Seq.map replaceLine
    |> Seq.toList // break laziness
    |> Seq.iter writer.WriteLine

/// Update all AssemblyInfo.[fs|cs|vb] files in the specified directory and its subdirectories
/// ## Parameters
///
/// - 'dir' - The directory (subdirectories will be included), which inhabits the AssemblyInfo files.
/// - 'replacementParameters' - The replacement parameters for the AssemblyInfo files.
///
/// ## Sample
///
///        BulkReplaceAssemblyInfoVersions "test/" (fun f -> 
///                                                   {f with
///                                                       AssemblyVersion = "1.1.1.1"
///                                                       AssemblyInformationalVersion = "1.1.1.1"})
let BulkReplaceAssemblyInfoVersions (dir:string) (replacementParameters:AssemblyInfoReplacementParams->AssemblyInfoReplacementParams) = 
    let directory = directoryInfo dir
    if directory.Exists then 
        !!(directory.FullName @@ @"\**\AssemblyInfo.*")
            |> Seq.iter(fun file ->
              ReplaceAssemblyInfoVersions ((fun p -> {p with OutputFileName = file }) >> replacementParameters))
    else logfn "%s does not exist." directory.FullName

/// Update all AssemblyInfos that were passed with given FileInclude
/// ## Parameters
///
/// - 'dir' - The directory (subdirectories will be included), which inhabits the AssemblyInfo files.
/// - 'replacementParameters' - The replacement parameters for the AssemblyInfo files.
///
/// ## Sample
///
///     let assemblyInfos = !!(@".\src\**\AssemblyInfo.cs") 
///                            --(@"**\*Scripts*\**")
///
///     ReplaceAssemblyInfoVersionsBulk assemblyInfos (fun f -> 
///         { f with
///                 AssemblyVersion = asmVersion
///                 AssemblyInformationalVersion = asmInfoVersion
///         })                          
let ReplaceAssemblyInfoVersionsBulk (fileIncludes:FileIncludes) (replacementParameters:AssemblyInfoReplacementParams->AssemblyInfoReplacementParams) = 
   fileIncludes
    |> Seq.iter(fun file ->
        ReplaceAssemblyInfoVersions ((fun p -> {p with OutputFileName = file }) >> replacementParameters))