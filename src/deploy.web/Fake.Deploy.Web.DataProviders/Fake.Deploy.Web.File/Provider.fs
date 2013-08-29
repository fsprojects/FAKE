namespace Fake.Deploy.Web.File
open Fake.Deploy.Web
open System
open System.Runtime.Serialization.Json

module FileIO =
    let mutable Exists = IO.File.Exists
    let mutable OpenRead = fun (path:string) -> IO.File.OpenRead(path) :> IO.Stream
    let mutable Create = fun (path:string) -> IO.File.Create(path) :> IO.Stream

module Provider =
    let mutable dataFolder = "" 
    let environmentsFile() = IO.Path.Combine(dataFolder, "Environments.json")
    let agentsFile() = IO.Path.Combine(dataFolder, "Agents.json")
    let usersFile() = IO.Path.Combine(dataFolder, "Users.json")
    let rolesFile() = IO.Path.Combine(dataFolder, "Roles.json")
    let environmentSerializer = new DataContractJsonSerializer(typedefof<Fake.Deploy.Web.Environment array>, [typedefof<AgentRef>])
    let agentSerializer = new DataContractJsonSerializer(typedefof<Agent array>, [typedefof<Uri>])
    let userSerializer = new DataContractJsonSerializer(typedefof<User array>)
    let roleSerializer = new DataContractJsonSerializer(typedefof<Role array>)
    
    let init folder =
        dataFolder <- folder

    let write (serializer:DataContractJsonSerializer) fileName data = 
        use file = FileIO.Create(fileName())
        serializer.WriteObject(file, data)
        file.Close()

    let read (serializer:DataContractJsonSerializer) fileName =
        if not (FileIO.Exists (fileName())) then
            None
        else
            use file = FileIO.OpenRead (fileName())
            let objs = serializer.ReadObject(file)
            file.Close()
            Some objs

    let getSavedExcept<'T> (saved:seq<'T>) (toSave:seq<'T>) (compare:'T->'T->bool) =
        let toSave' = toSave |> List.ofSeq
        let savedNotInToSave = saved |> List.ofSeq |> List.filter(fun x -> not (toSave' |> List.exists(fun y -> compare x y)))
        savedNotInToSave @ toSave' |> Array.ofList
    
    let getEnvironments() =
        let data = read environmentSerializer environmentsFile
        match data with
            | None -> [||]
            | Some x -> x :?> Fake.Deploy.Web.Environment array

    let saveEnvironments(envs: seq<Fake.Deploy.Web.Environment>) =
        envs |> Seq.iter(fun e -> if e.Id = null then e.Id <- Guid.NewGuid().ToString())
        let toSave = getSavedExcept (getEnvironments()) envs (fun x y -> x.Id = y.Id)
        write environmentSerializer environmentsFile toSave
        
    let deleteEnvironment id = 
        getEnvironments()
        |> Seq.filter(fun e -> e.Id <> id)
        |> (fun envs -> write environmentSerializer environmentsFile (envs |> Array.ofSeq))

    let getAgents () =
        let data = read agentSerializer agentsFile
        match data with
            | None -> [||]
            | Some x -> x :?> Agent array


    let saveAgents(agents: seq<Agent>) =
        agents |> Seq.iter(fun x -> if x.Id = null then x.Id <- Guid.NewGuid().ToString())
        let toSave = getSavedExcept (getAgents()) agents (fun x y -> x.Id = y.Id)
        write agentSerializer agentsFile toSave

    let deleteAgent (id:string) =
        getAgents()
        |> Seq.filter(fun a -> a.Id <> id)
        |> (fun envs -> write agentSerializer agentsFile (envs |> Array.ofSeq))

    let getRoles () =
        let data = read roleSerializer rolesFile
        match data with
            | None -> [||]
            | Some x -> x :?> Role array

    let saveRoles roles =
        let toSave = getSavedExcept (getRoles()) roles (fun x y -> x.Id = y.Id)
        write roleSerializer rolesFile toSave

    let getAllUsers () =
        let data = read userSerializer usersFile
        match data with
            | None -> [||]
            | Some x -> x :?> User array
        |> Array.sortBy(fun u -> u.Username)

    let saveUsers (users:seq<User>) = 
        let toSave = getSavedExcept (getAllUsers()) users (fun x y -> x.Id = y.Id)
        write userSerializer usersFile toSave

    let deleteUser (id : string) =
        getAllUsers() 
        |> Array.filter(fun u -> u.Id <> id)
        |> (fun envs -> write userSerializer usersFile (envs |> Array.ofSeq))

    let getUsers ids = 
        getAllUsers() 
        |> Array.filter(fun u -> Seq.exists(fun id -> id = u.Id) ids)
        
    let tryGetUser id = 
        getAllUsers() 
        |> Array.tryFind(fun u -> u.Id = id)
        