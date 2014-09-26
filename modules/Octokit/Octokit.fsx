#r @"../../../../../packages/Octokit/lib/net45/Octokit.dll"

open Octokit
open System
open System.IO

type Draft = 
    { Client : GitHubClient
      Owner : string
      Project : string
      DraftRelease : Release }

let createClient user password = 
    async { 
        let github = new GitHubClient(new ProductHeaderValue("FAKE"))
        github.Credentials <- Credentials(user, password)
        return github
    }

let createDraft owner project version prerelease (notes: string seq) (client : Async<GitHubClient>) = 
    async { 
        let data = new ReleaseUpdate(version)
        data.Name <- version
        data.Body <- String.Join(Environment.NewLine, notes)
        data.Draft <- true
        data.Prerelease <- prerelease
        let! client' = client
        let! draft = Async.AwaitTask <| client'.Release.Create(owner, project, data)
        printfn "Created draft release id %d" draft.Id
        return { Client = client'
                 Owner = owner
                 Project = project
                 DraftRelease = draft }
    }

let uploadFile fileName (draft : Async<Draft>) = 
    async { 
        let fi = FileInfo(fileName)
        let archiveContents = File.OpenRead(fi.FullName)
        let assetUpload = new ReleaseAssetUpload()
        assetUpload.FileName <- fi.Name
        assetUpload.ContentType <- "application/octet-stream"
        assetUpload.RawData <- archiveContents
        let! draft' = draft
        let! asset = Async.AwaitTask <| draft'.Client.Release.UploadAsset(draft'.DraftRelease, assetUpload)
        printfn "Uploaded %s" asset.Name
        return draft'
    }

let releaseDraft (draft : Async<Draft>) = 
    async { 
        let! draft' = draft
        let update = draft'.DraftRelease.ToUpdate()
        update.Draft <- false
        let! released = Async.AwaitTask <| draft'.Client.Release.Edit(draft'.Owner, draft'.Project, draft'.DraftRelease.Id, update)
        printfn "Released %d on github" released.Id
    }
