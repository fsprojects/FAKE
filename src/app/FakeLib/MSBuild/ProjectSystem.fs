module Fake.MsBuild.ProjectSystem

open Fake

type ProjectSystem(projectFile : string) = 
    let file = ReadFileAsString projectFile

    member x.FileExistsInProject(path : string) = 
        file.Contains path