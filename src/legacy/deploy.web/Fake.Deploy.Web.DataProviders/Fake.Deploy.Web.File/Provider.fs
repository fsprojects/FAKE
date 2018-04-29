namespace Fake.Deploy.Web.File
open Fake.Deploy.Web
open Newtonsoft.Json
open System
open System.IO
open System.Runtime.Serialization.Json

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module FileIO =
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let assertDirectory f (path:string) = 
        let dir = IO.Path.GetDirectoryName(path)
        if not <| IO.Directory.Exists(dir)
        then IO.Directory.CreateDirectory(dir) |> ignore
        f path
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]    
    let mutable Exists = IO.File.Exists
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let mutable OpenRead = fun (path:string) -> IO.File.OpenRead(path) :> IO.Stream
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let mutable Create = assertDirectory (fun path ->  IO.File.Create(path) :> IO.Stream)

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Provider =
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let mutable dataFolder = "" 
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let environmentsFile() = IO.Path.Combine(dataFolder, "Environments.json")
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let agentsFile() = IO.Path.Combine(dataFolder, "Agents.json")
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let usersFile() = IO.Path.Combine(dataFolder, "Users.json")
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let rolesFile() = IO.Path.Combine(dataFolder, "Roles.json")
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let init folder =
        dataFolder <- folder

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let write<'T> fileName (data: 'T) = 
        use file = FileIO.Create(fileName())
        use wr = new StreamWriter(file)
        let json = JsonConvert.SerializeObject(data, Formatting.Indented)
        wr.Write(json)
        wr.Flush()
        wr.Close()

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
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

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let getSavedExcept<'T> (saved:seq<'T>) (toSave:seq<'T>) (compare:'T->'T->bool) =
        let toSave' = toSave |> List.ofSeq
        let savedNotInToSave = saved |> List.ofSeq |> List.filter(fun x -> not (toSave' |> List.exists(fun y -> compare x y)))
        savedNotInToSave @ toSave' |> Array.ofList
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let getEnvironments() =
        let data = read<Fake.Deploy.Web.Environment array> environmentsFile
        match data with
            | None -> [||]
            | Some x -> x

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let saveEnvironments(envs: seq<Fake.Deploy.Web.Environment>) =
        let envsWithId = envs |> Seq.map(fun e -> if String.IsNullOrEmpty e.Id then { e with Id = Guid.NewGuid().ToString() } else e)
        let toSave = getSavedExcept (getEnvironments()) envsWithId (fun x y -> x.Id = y.Id)
        write environmentsFile toSave
        
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]    
    let deleteEnvironment id = 
        getEnvironments()
        |> Seq.filter(fun e -> e.Id <> id)
        |> (fun envs -> write environmentsFile (envs |> Array.ofSeq))

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let getAgents () =
        let data = read<Agent array> agentsFile
        match data with
            | None -> [||]
            | Some x -> x

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let saveAgents(agents: seq<Agent>) =
        let agentsWithId = agents |> Seq.map(fun e -> if String.IsNullOrEmpty e.Id then { e with Id = Guid.NewGuid().ToString() } else e)
        let toSave = getSavedExcept (getAgents()) agentsWithId (fun x y -> x.Id = y.Id)
        write agentsFile toSave

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let deleteAgent (id:string) =
        getAgents()
        |> Seq.filter(fun a -> a.Id <> id)
        |> (fun envs -> write agentsFile (envs |> Array.ofSeq))

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let getRoles () =
        let data = read<Role array> rolesFile
        match data with
            | None -> [||]
            | Some x -> x

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let saveRoles roles =
        let toSave = getSavedExcept (getRoles()) roles (fun x y -> x.Id = y.Id)
        write rolesFile toSave

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let getAllUsers () =
        let data = read<User array> usersFile
        match data with
            | None -> [||]
            | Some x -> x
        |> Array.sortBy(fun u -> u.Username)

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let saveUsers (users:seq<User>) = 
        let toSave = getSavedExcept (getAllUsers()) users (fun x y -> x.Id = y.Id)
        write usersFile toSave

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let deleteUser (id : string) =
        getAllUsers() 
        |> Array.filter(fun u -> u.Id <> id)
        |> (fun envs -> write usersFile (envs |> Array.ofSeq))

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    let getUsers ids = 
        getAllUsers() 
        |> Array.filter(fun u -> Seq.exists(fun id -> id = u.Id) ids)
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]    
    let tryGetUser id = 
        getAllUsers() 
        |> Array.tryFind(fun u -> u.Id = id)
        