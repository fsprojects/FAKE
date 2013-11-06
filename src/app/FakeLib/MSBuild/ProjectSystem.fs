module Fake.MsBuild.ProjectSystem

open System
open System.IO
open System.Collections.Generic
open System.Reflection
open Microsoft.Build.Evaluation
open NuGet

let TryGetProject projectFile = 
    ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFile) 
    |> Seq.tryFind(fun p -> p.FullPath = projectFile)

type ProjectSystem(projectFile : string) = 
    inherit PhysicalFileSystem(Path.GetDirectoryName(projectFile))
    
    let project = 
        match TryGetProject projectFile with
        | Some project -> project
        | None -> Project(projectFile)
    
    let projectName = Path.GetFileNameWithoutExtension <| project.FullPath
    let framework = 
        new Runtime.Versioning.FrameworkName(project.GetPropertyValue
                                                 ("TargetFrameworkMoniker"))
    
    let GetReferenceByName name =
        project.GetItems("Reference")
        |> Seq.filter
               (fun i -> 
                   i.EvaluatedInclude.StartsWith
                       (name, StringComparison.OrdinalIgnoreCase))
        |> Seq.tryFind
               (fun i -> 
                   AssemblyName(i.EvaluatedInclude)
                       .Name.Equals(name, StringComparison.OrdinalIgnoreCase))

    let GetReferenceByPath path = 
        let name = Path.GetFileNameWithoutExtension path
        GetReferenceByName name
    
    interface IProjectSystem with
        member x.TargetFramework with get () = framework
        member x.ProjectName with get () = projectName
        
        member x.AddReference(path, stream) = 
            let fullPath = PathUtility.GetAbsolutePath(x.Root, path)
            let relPath = 
                PathUtility.GetRelativePath(project.FullPath, fullPath)
            let includeName = Path.GetFileNameWithoutExtension fullPath
            project.AddItem
                ("Reference", includeName, [|KeyValuePair("HintPath", relPath)|]) 
            |> ignore
            project.Save()
        
        member x.AddFrameworkReference name = 
            project.AddItem("Reference", name) |> ignore
            project.Save()
        
        member x.ReferenceExists path = 
            match GetReferenceByName path with
            | Some _ -> true
            | None -> false
        
        member x.RemoveReference path = 
            match GetReferenceByPath path with
            | Some i -> 
                project.RemoveItem(i) |> ignore
                project.Save()
            | None -> ()
        
        member x.IsSupportedFile path = true
        member x.ResolvePath path = path
        member x.IsBindingRedirectSupported with get () = true
        
        member x.AddImport((targetPath : string), location) = 
            if project.Xml.Imports = null 
               || project.Xml.Imports 
                  |> Seq.forall
                         (fun import -> 
                             not 
                             <| targetPath.Equals
                                    (import.Project, 
                                     StringComparison.OrdinalIgnoreCase)) then 
                project.Xml.AddImport(targetPath) |> ignore
                project.ReevaluateIfNecessary()
                project.Save()
        
        member x.RemoveImport(targetPath : string) = 
            match project.Xml.Imports 
                  |> Seq.tryFind
                         (fun import -> 
                             targetPath.Equals
                                 (import.Project, 
                                  StringComparison.OrdinalIgnoreCase)) with
            | None -> ()
            | Some i -> 
                project.Xml.RemoveChild(i)
                project.ReevaluateIfNecessary()
                project.Save()
        
        member x.FileExistsInProject(path : string) = 
            project.Items 
            |> Seq.exists
                   (fun i -> 
                       i.EvaluatedInclude.Equals
                           (path, StringComparison.OrdinalIgnoreCase) 
                       && (String.IsNullOrEmpty(i.ItemType) 
                           || i.ItemType.[0] <> '_'))
    
    interface IPropertyProvider with
        member x.GetPropertyValue name = project.GetPropertyValue(name) :> obj