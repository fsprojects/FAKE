[<AutoOpen>]
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
  { OutputFileName: string;
    ComVisible: bool;
    CLSCompliant: bool;
    Guid: string;
    CodeLanguage: CodeLanguage;
    AssemblyTitle: string;
    AssemblyDescription: string;
    AssemblyConfiguration: string;
    AssemblyCompany: string;
    AssemblyProduct: string;
    AssemblyCopyright: string;
    AssemblyTrademark: string;
    AssemblyCulture: string;
    AssemblyVersion: string;
    AssemblyFileVersion: string;
    AssemblyInformationalVersion: string;
    AssemblyKeyFile: string;
    AssemblyKeyName: string;
    AssemblyDelaySign: bool;
    GenerateClass:bool }

/// AssemblyInfo default params
let AssemblyInfoDefaults =
  { OutputFileName = String.Empty ;
    ComVisible = false ;
    CLSCompliant = false ;
    Guid = Guid.NewGuid().ToString() ;
    CodeLanguage = CSharp;
    AssemblyTitle = String.Empty;
    AssemblyDescription = String.Empty;
    AssemblyConfiguration = String.Empty;
    AssemblyCompany = String.Empty;
    AssemblyProduct = String.Empty;
    AssemblyCopyright = String.Empty;
    AssemblyTrademark = String.Empty;
    AssemblyCulture = String.Empty;
    AssemblyVersion = "1.0.0.0";
    AssemblyFileVersion = String.Empty;
    AssemblyInformationalVersion = String.Empty;
    AssemblyKeyFile = String.Empty;
    AssemblyKeyName = String.Empty;
    AssemblyDelaySign = false;
    GenerateClass = false}  
    
/// generates the assembly info file
let generateFile param (attributes:Dictionary<string, string>) imports (writer:TextWriter) =
  let provider,outputFileName = 
    match param.CodeLanguage with
    | CSharp      -> 
        let provider = new Microsoft.CSharp.CSharpCodeProvider() :> CodeDomProvider
        provider, Path.ChangeExtension(param.OutputFileName, ".cs")
    | FSharp      -> failwith "No CodeDom available."            
    | VisualBasic -> 
        let provider = new Microsoft.VisualBasic.VBCodeProvider() :> CodeDomProvider
        provider, Path.ChangeExtension(param.OutputFileName, ".vb")           

  let codeCompileUnit = new CodeCompileUnit()
  let codeNamespace = new CodeNamespace()

  imports
    |> Seq.iter (fun import ->
          new CodeNamespaceImport(import)
            |> codeNamespace.Imports.Add)
      
  codeCompileUnit.Namespaces.Add codeNamespace |> ignore
  
  let codeAttrArg value = new CodeAttributeArgument(new CodePrimitiveExpression(value))

  let addString =
    match param.CodeLanguage with
    | FSharp      ->          
       (attributes 
         |> Seq.map
             (fun attr ->
               match bool.TryParse attr.Value with
               | true, value -> sprintf "[<assembly: %s (%A)>]" attr.Key value
               | _ -> sprintf "[<assembly: %s (\"%s\")>]" attr.Key attr.Value)
         |> separated "\n") + "\n()"        
    | _ ->  
      for attr in attributes do            
        // create new assembly-level attribute
        let codeAttributeDeclaration = new CodeAttributeDeclaration(attr.Key)      
        
        match attr.Key with
        | "CLSCompliant"
        | "AssemblyDelaySign"
        | "ComVisible" -> 
          match bool.TryParse attr.Value with
          | true,value ->
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
          | "CLSCompliant"
          | "AssemblyDelaySign"
          | "ComVisible"
          | "AssemblyKeyFile" -> false
          | _ -> true)
      |> Seq.iter (fun attr ->
          let field = new CodeMemberField(typeof<string>, attr.Key)
          field.Attributes <- MemberAttributes.Assembly ||| MemberAttributes.Const
          field.InitExpression <- new CodePrimitiveExpression(attr.Value)
          thisAssemblyType.Members.Add field |> ignore)
    
    codeNamespace.Types.Add thisAssemblyType |> ignore                                     
             
  let options = new CodeGeneratorOptions()
             
  provider.GenerateCodeFromCompileUnit(codeCompileUnit, writer, options)
  if addString <> String.Empty then
    writer.WriteLine addString
    

/// Generates an AssemblyInfo file for projects
let AssemblyInfo setParams = 
  let param' = setParams AssemblyInfoDefaults
  traceStartTask "AssemblyInfo" param'.OutputFileName
  let param'' =
    if param'.AssemblyFileVersion <> String.Empty then param' else
    {param' with AssemblyFileVersion = param'.AssemblyVersion}
  
  let param =
    if param''.AssemblyProduct <> String.Empty then param'' else
    {param'' with AssemblyProduct = param''.AssemblyTitle}
  
  if isNullOrEmpty param.OutputFileName then failwith "You have to specify the OutputFileName for the AssemblyInfo task."
  let attributes = new Dictionary<_,_>()
  let attr name p =
    try
      let value = p.ToString()
      if value <> String.Empty then attributes.Add(name, value)
    with 
    | exn -> ()
    
  attr "ComVisible" param.ComVisible
  attr "CLSCompliant" param.CLSCompliant
  attr "Guid" param.Guid
  attr "AssemblyTitle" param.AssemblyTitle
  attr "AssemblyDescription" param.AssemblyDescription
  attr "AssemblyConfiguration" param.AssemblyConfiguration
  attr "AssemblyCompany" param.AssemblyCompany
  attr "AssemblyProduct" param.AssemblyProduct
  attr "AssemblyCopyright" param.AssemblyCopyright
  attr "AssemblyTrademark" param.AssemblyTrademark
  attr "AssemblyCulture" param.AssemblyCulture
  attr "AssemblyVersion" param.AssemblyVersion
  attr "AssemblyFileVersion" param.AssemblyFileVersion
  attr "AssemblyInformationalVersion" param.AssemblyInformationalVersion
  attr "AssemblyKeyFile" param.AssemblyKeyFile
  attr "AssemblyKeyName" param.AssemblyKeyName
  attr "AssemblyDelaySign" param.AssemblyDelaySign
  
  let imports = 
      ["System" 
       "System.Reflection"
       "System.Runtime.CompilerServices"
       "System.Runtime.InteropServices" ]

  let fi = fileInfo param.OutputFileName
  use writer = File.CreateText param.OutputFileName
  let write (x:string) = writer.WriteLine x
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

     attributes 
         |> Seq.iter
             (fun attr ->
               match bool.TryParse attr.Value with
               | true, value -> write(sprintf "\n[<assembly: %s (%A)>]" attr.Key value)
               | false, _ ->  write(sprintf "\n[<assembly: %s (\"%s\")>]" attr.Key (attr.Value.ToString())))
     write "\n()"        

  | _ -> generateFile param attributes imports writer
  writer.Flush()
  writer.Close()
  tracefn "Created AssemblyInfo file \"%s\"." param.OutputFileName
  traceEndTask "AssemblyInfo" param'.OutputFileName