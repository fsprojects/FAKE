[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.Deploy.Auth

open System
open System.Collections.Concurrent
open Fake
open Fake.SshRsaModule
open Fake.DeployAgentModule
open Fake.AppConfig
open Nancy
open Nancy.Security

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let AuthTokenName = "AuthToken"

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type LoginChallange = 
    { UserId : string
      Challenge : string
      ValidTo : DateTime }

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let NotAuthenticated = AuthenticatedUser("anonymous", ["none"])

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type LoginRequests() = 
    let requests = ConcurrentDictionary<string, LoginChallange>()
    
    let removeStale() = 
        let stale = requests |> Seq.filter (fun r -> r.Value.ValidTo <= DateTime.Now)
        stale |> Seq.iter (fun s -> requests.TryRemove(s.Key) |> ignore)
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.Add (userId : string) (challenge : string) = 
        removeStale()
        let c = 
            { UserId = userId.ToLower()
              Challenge = challenge
              ValidTo = DateTime.Now.AddMinutes 2. }
        requests.TryAdd(challenge, c) |> ignore
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.Get challenge = 
        if challenge = null then None
        else 
            removeStale()
            let c, x = requests.TryGetValue challenge
            if c && x.ValidTo > DateTime.Now then Some x.UserId
            else None
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.Remove challenge = requests.TryRemove challenge |> ignore

let private loginRequest = LoginRequests()

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type AuthModule(userMapper : UserMapper, users : list<PublicKey>) as http = 
    inherit FakeModule("/fake")
    
    do 
        http.get "/login/{userId}" (fun p -> 
            let userId = p ?> "userId"
            let value = http.CreateLoginRequest userId
            http.Response.AsText value)
        http.post "/login" (fun p -> 
            let challenge = http.Request.Form ?> "challenge"
            let signature = http.Request.Form ?> "signature"
            http.HandleLoginRequest challenge signature)
        http.get "/logout" (fun p -> 
            http.Logout(http.Request.Headers.Item(AuthTokenName) |> Seq.head)
            http.Response.AsText("logged out"))
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member public http.Logout(ticket : string) = 
        (ticket
         |> Guid.Parse
         |> userMapper.RemoveByToken)
        |> ignore
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member public http.CreateLoginRequest userId = 
        use rnd = new System.Security.Cryptography.RNGCryptoServiceProvider()
        let bytes = Array.zeroCreate 256
        rnd.GetNonZeroBytes(bytes)
        let value = Convert.ToBase64String bytes
        loginRequest.Add userId value
        value
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member public http.HandleLoginRequest valueToSign signature = 
        let failLogin() = http.Response.AsText("").WithStatusCode 401
        match AppConfig.Authorization, (loginRequest.Get valueToSign) with
        | Off, _ -> let token = userMapper.AddUser(NotAuthenticated)
                    http.Response.AsJson token
        | On, None -> failLogin()
        | On, Some userId -> 
            loginRequest.Remove valueToSign
            let user = 
                users 
                |> Seq.tryFind 
                       (fun u -> String.Compare(u.UserId, userId, StringComparison.CurrentCultureIgnoreCase) = 0)
            if user = None then failLogin()
            else 
                let value = Convert.FromBase64String valueToSign
                let sign = Convert.FromBase64String signature
                match users |> Seq.tryFind (fun u -> u.UserId.Equals(userId, StringComparison.CurrentCultureIgnoreCase)) with
                | None -> failLogin()
                | Some user -> 
                    if verifySignature value sign user then 
                        let token = 
                            userMapper.AddUser
                                (AuthenticatedUser(user.UserId, [ "For now everyone authenticated can do everything" ]))
                        http.Response.AsJson token
                    else failLogin()
