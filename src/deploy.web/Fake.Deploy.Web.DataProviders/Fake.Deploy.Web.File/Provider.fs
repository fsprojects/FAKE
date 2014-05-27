namespace Fake.Deploy.Web.File
open Fake.Deploy.Web
open Newtonsoft.Json
open System
open System.IO
open System.Runtime.Serialization.Json

module FileIO =
    let assertDirectory f (path:string) = 
        let dir = IO.Path.GetDirectoryName(path)
        if not <| IO.Directory.Exists(dir)
        then IO.Directory.CreateDirectory(dir) |> ignore
        f path
    let mutable Exists = IO.File.Exists
    let mutable OpenRead = fun (path:string) -> IO.File.OpenRead(path) :> IO.Stream
    let mutable Create = assertDirectory (fun path ->  IO.File.Create(path) :> IO.Stream)

module Provider =
    let mutable dataFolder = "" 
    let environmentsFile() = IO.Path.Combine(dataFolder, "Environments.json")
    let agentsFile() = IO.Path.Combine(dataFolder, "Agents.json")
    let usersFile() = IO.Path.Combine(dataFolder, "Users.json")
    let rolesFile() = IO.Path.Combine(dataFolder, "Roles.json")
    
    let init folder =
        dataFolder <- folder

    let write<'T> fileName (data: 'T) = 
        use file = FileIO.Create(fileName())
        use wr = new StreamWriter(file)
        let json = JsonConvert.SerializeObject(data, Formatting.Indented)
        wr.Write(json)
        wr.Flush()
        wr.Close()

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

    let getSavedExcept<'T> (saved:seq<'T>) (toSave:seq<'T>) (compare:'T->'T->bool) =
        let toSave' = toSave |> List.ofSeq
        let savedNotInToSave = saved |> List.ofSeq |> List.filter(fun x -> not (toSave' |> List.exists(fun y -> compare x y)))
        savedNotInToSave @ toSave' |> Array.ofList
    
    let getEnvironments() =
        let data = read<Fake.Deploy.Web.Environment array> environmentsFile
        match data with
            | None -> [||]
            | Some x -> x

    let saveEnvironments(envs: seq<Fake.Deploy.Web.Environment>) =
        let envsWithId = envs |> Seq.map(fun e -> if String.IsNullOrEmpty e.Id then { e with Id = Guid.NewGuid().ToString() } else e)
        let toSave = getSavedExcept (getEnvironments()) envsWithId (fun x y -> x.Id = y.Id)
        write environmentsFile toSave
        
    let deleteEnvironment id = 
        getEnvironments()
        |> Seq.filter(fun e -> e.Id <> id)
        |> (fun envs -> write environmentsFile (envs |> Array.ofSeq))

    let getAgents () =
        let data = read<Agent array> agentsFile
        match data with
            | None -> [||]
            | Some x -> x


    let saveAgents(agents: seq<Agent>) =
        let agentsWithId = agents |> Seq.map(fun e -> if String.IsNullOrEmpty e.Id then { e with Id = Guid.NewGuid().ToString() } else e)
        let toSave = getSavedExcept (getAgents()) agentsWithId (fun x y -> x.Id = y.Id)
        write agentsFile toSave

    let deleteAgent (id:string) =
        getAgents()
        |> Seq.filter(fun a -> a.Id <> id)
        |> (fun envs -> write agentsFile (envs |> Array.ofSeq))

    let getRoles () =
        let data = read<Role array> rolesFile
        match data with
            | None -> [||]
            | Some x -> x

    let saveRoles roles =
        let toSave = getSavedExcept (getRoles()) roles (fun x y -> x.Id = y.Id)
        write rolesFile toSave

    let getAllUsers () =
        let data = read<User array> usersFile
        match data with
            | None -> [||]
            | Some x -> x
        |> Array.sortBy(fun u -> u.Username)

    let saveUsers (users:seq<User>) = 
        let toSave = getSavedExcept (getAllUsers()) users (fun x y -> x.Id = y.Id)
        write usersFile toSave

    let deleteUser (id : string) =
        getAllUsers() 
        |> Array.filter(fun u -> u.Id <> id)
        |> (fun envs -> write usersFile (envs |> Array.ofSeq))

    let getUsers ids = 
        getAllUsers() 
        |> Array.filter(fun u -> Seq.exists(fun id -> id = u.Id) ids)
        
    let tryGetUser id = 
        getAllUsers() 
        |> Array.tryFind(fun u -> u.Id = id)
        