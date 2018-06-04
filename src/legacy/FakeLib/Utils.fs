[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.Utils

open System
open System.Net

//Using the same code as https://github.com/fsprojects/Paket for Memoization & Proxy handling
let inline internal memoizeByExt (getKey : 'a -> 'key) (f: 'a -> 'b) : ('a -> 'b) * ('key * 'b -> unit) =
    let cache = System.Collections.Concurrent.ConcurrentDictionary<'key, 'b>()
    (fun (x: 'a) ->
        cache.GetOrAdd(getKey x, fun _ -> f x)),
    (fun (key, c) ->
        cache.TryAdd(key, c) |> ignore)

let inline internal memoizeBy (getKey : 'a -> 'key) (f: 'a -> 'b) : ('a -> 'b) =
    memoizeByExt getKey f |> fst

let inline internal memoize (f: 'a -> 'b) : 'a -> 'b = memoizeBy id f

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let envProxies () =
    let getEnvValue (name:string) =
        let v = Environment.GetEnvironmentVariable(name.ToUpperInvariant())
        // under mono, env vars are case sensitive
        if isNull v then Environment.GetEnvironmentVariable(name.ToLowerInvariant()) else v
    let bypassList =
        let noproxyString = getEnvValue "NO_PROXY"
        let noproxy = if not (String.IsNullOrEmpty (noproxyString)) then System.Text.RegularExpressions.Regex.Escape(noproxyString).Replace(@"*", ".*")  else noproxyString

        if String.IsNullOrEmpty noproxy then [||] else
        noproxy.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries)
    let getCredentials (uri:Uri) =
        let userPass = uri.UserInfo.Split([| ':' |], 2)
        if userPass.Length <> 2 || userPass.[0].Length = 0 then None else
        let credentials = NetworkCredential(Uri.UnescapeDataString userPass.[0], Uri.UnescapeDataString userPass.[1])
        Some credentials

    let getProxy (scheme:string) =
        let envVarName = sprintf "%s_PROXY" (scheme.ToUpperInvariant())
        let envVarValue = getEnvValue envVarName
        if isNull envVarValue then None else
        match Uri.TryCreate(envVarValue, UriKind.Absolute) with
        | true, envUri ->
#if NETSTANDARD1_6
            Some
                { new IWebProxy with
                    member __.Credentials
                        with get () = (Option.toObj (getCredentials envUri)) :> ICredentials
                        and set value = ()
                    member __.GetProxy _ =
                        Uri (sprintf "http://%s:%d" envUri.Host envUri.Port)
                    member __.IsBypassed (host : Uri) =
                        Array.contains (string host) bypassList
                }
#else
            let proxy = WebProxy (Uri (sprintf "http://%s:%d" envUri.Host envUri.Port))
            proxy.Credentials <- Option.toObj (getCredentials envUri)
            proxy.BypassProxyOnLocal <- true
            proxy.BypassList <- bypassList
            Some proxy
#endif
        | _ -> None

    let addProxy (map:Map<string, WebProxy>) scheme =
        match getProxy scheme with
        | Some p -> Map.add scheme p map
        | _ -> map

    [ "http"; "https" ]
    |> List.fold addProxy Map.empty

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let calcEnvProxies = lazy (envProxies())

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let getDefaultProxyForUrl =
    memoize
      (fun (url:string) ->
            let uri = Uri url
            let getDefault () =
#if NETSTANDARD1_6
                let result = WebRequest.DefaultWebProxy
#else
                let result = WebRequest.GetSystemWebProxy()
#endif
#if NETSTANDARD1_6
                let proxy = result
#else
                let address = result.GetProxy uri
                if address = uri then null else
                let proxy = WebProxy address
                proxy.BypassProxyOnLocal <- true
#endif
                proxy.Credentials <- CredentialCache.DefaultCredentials
                proxy

            match calcEnvProxies.Force().TryFind uri.Scheme with
            | Some p -> if p.GetProxy uri <> uri then p else getDefault()
            | None -> getDefault())
