module Fake.MsBuild.ProjectSystem

open Fake

type ProjectSystem(projectFile : string) = 
    let file = 
        ReadFileAsString projectFile
        |> replace "\\" "/"

    member x.FileExistsInProject(path : string) = 
        let path = replace "\\" "/" path
        file.Contains (sprintf "<Compile Include=\"%s\" />" path)