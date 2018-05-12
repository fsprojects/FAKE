namespace Fake.Deploy.Web.File
open Fake.Deploy.Web
open Newtonsoft.Json
open System
open System.IO
open System.Runtime.Serialization.Json

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module FileIO =
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let assertDirectory f (path:string) = 
        let dir = IO.Path.GetDirectoryName(path)
        if not <| IO.Directory.Exists(dir)
        then IO.Directory.CreateDirectory(dir) |> ignore
        f path
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]    
    let mutable Exists = IO.File.Exists
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let mutable OpenRead = fun (path:string) -> IO.File.OpenRead(path) :> IO.Stream
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let mutable Create = assertDirectory (fun path ->  IO.File.Create(path) :> IO.Stream)

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Provider =
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let mutable dataFolder = "" 
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let environmentsFile() = IO.Path.Combine(dataFolder, "Environments.json")
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let agentsFile() = IO.Path.Combine(dataFolder, "Agents.json")
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let usersFile() = IO.Path.Combine(dataFolder, "Users.json")
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let rolesFile() = IO.Path.Combine(dataFolder, "Roles.json")
    
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let init folder =
        dataFolder <- folder

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let write<'T> fileName (data: 'T) = 
        use file = FileIO.Create(fileName())
        use wr = new StreamWriter(file)
        let json = JsonConvert.SerializeObject(data, Formatting.Indented)
        wr.Write(json)
        wr.Flush()
        wr.Close()

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let read<'T> fileName =
        if not (FileIO.Exists (fileName())) then
            None
        else
            use file = FileIO.OpenRead (fileName())
            use rd = new StreamReader(file)
            let json = rd.ReadToEnd()
            let objs = JsonConvert.DeserializeObject<'T>(json)
            file.Close()
            Some objs

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let getSavedExcept<'T> (saved:seq<'T>) (toSave:seq<'T>) (compare:'T->'T->bool) =
        let toSave' = toSave |> List.ofSeq
        let savedNotInToSave = saved |> List.ofSeq |> List.filter(fun x -> not (toSave' |> List.exists(fun y -> compare x y)))
        savedNotInToSave @ toSave' |> Array.ofList
    
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let getEnvironments() =
        let data = read<Fake.Deploy.Web.Environment array> environmentsFile
        match data with
            | None -> [||]
            | Some x -> x

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let saveEnvironments(envs: seq<Fake.Deploy.Web.Environment>) =
        let envsWithId = envs |> Seq.map(fun e -> if String.IsNullOrEmpty e.Id then { e with Id = Guid.NewGuid().ToString() } else e)
        let toSave = getSavedExcept (getEnvironments()) envsWithId (fun x y -> x.Id = y.Id)
        write environmentsFile toSave
        
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]    
    let deleteEnvironment id = 
        getEnvironments()
        |> Seq.filter(fun e -> e.Id <> id)
        |> (fun envs -> write environmentsFile (envs |> Array.ofSeq))

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let getAgents () =
        let data = read<Agent array> agentsFile
        match data with
            | None -> [||]
            | Some x -> x

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let saveAgents(agents: seq<Agent>) =
        let agentsWithId = agents |> Seq.map(fun e -> if String.IsNullOrEmpty e.Id then { e with Id = Guid.NewGuid().ToString() } else e)
        let toSave = getSavedExcept (getAgents()) agentsWithId (fun x y -> x.Id = y.Id)
        write agentsFile toSave

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let deleteAgent (id:string) =
        getAgents()
        |> Seq.filter(fun a -> a.Id <> id)
        |> (fun envs -> write agentsFile (envs |> Array.ofSeq))

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let getRoles () =
        let data = read<Role array> rolesFile
        match data with
            | None -> [||]
            | Some x -> x

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let saveRoles roles =
        let toSave = getSavedExcept (getRoles()) roles (fun x y -> x.Id = y.Id)
        write rolesFile toSave

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let getAllUsers () =
        let data = read<User array> usersFile
        match data with
            | None -> [||]
            | Some x -> x
        |> Array.sortBy(fun u -> u.Username)

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let saveUsers (users:seq<User>) = 
        let toSave = getSavedExcept (getAllUsers()) users (fun x y -> x.Id = y.Id)
        write usersFile toSave

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let deleteUser (id : string) =
        getAllUsers() 
        |> Array.filter(fun u -> u.Id <> id)
        |> (fun envs -> write usersFile (envs |> Array.ofSeq))

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    let getUsers ids = 
        getAllUsers() 
        |> Array.filter(fun u -> Seq.exists(fun id -> id = u.Id) ids)
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]    
    let tryGetUser id = 
        getAllUsers() 
        |> Array.tryFind(fun u -> u.Id = id)
        