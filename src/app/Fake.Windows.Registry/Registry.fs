namespace Fake.Windows

/// Contains functions which allow to read and write information from/to the registry.
[<RequireQualifiedAccess>]
module Registry =

    open Microsoft.Win32

    /// Registry base keys.
    type RegistryBaseKey =
        | HKEYLocalMachine
        | HKEYClassesRoot
        | HKEYUsers
        | HKEYCurrentUser
        | HKEYCurrentConfig
        | HKEYPerformanceData

    /// Maps the RegistryBaseKey to a RegistryKey
    /// [omit]
    let getKey name =
        match name with
        | HKEYLocalMachine -> Registry.LocalMachine
        | HKEYClassesRoot -> Registry.ClassesRoot
        | HKEYUsers -> Registry.Users
        | HKEYCurrentUser -> Registry.CurrentUser
        | HKEYCurrentConfig -> Registry.CurrentConfig
        | HKEYPerformanceData -> Registry.PerformanceData

    /// Maps the RegistryBaseKey to a RegistryKey for a 64bit System
    /// [omit]
    let get64BitKey name =
        match name with
        | HKEYLocalMachine -> RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
        | HKEYClassesRoot -> RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64)
        | HKEYUsers -> RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64)
        | HKEYCurrentUser -> RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
        | HKEYCurrentConfig -> RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry64)
        | HKEYPerformanceData -> RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Registry64)

    /// Maps the RegistryBaseKey to a RegistryKey for a 32bit System
    /// [omit]
    let get32BitKey name =
        match name with
        | HKEYLocalMachine -> RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
        | HKEYClassesRoot -> RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32)
        | HKEYUsers -> RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry32)
        | HKEYCurrentUser -> RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
        | HKEYCurrentConfig -> RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry32)
        | HKEYPerformanceData -> RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Registry32)

    /// Gets a 64-bit registry key
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    /// - `writePermission` - The write permissions on registery entry
    let getRegistryKey64 baseKey subKey (writePermission: bool) =
        (get64BitKey baseKey)
            .OpenSubKey(subKey, writePermission)

    /// Gets a registry key and falls back to 32 bit if the 64bit key is not there
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    /// - `writePermission` - The write permissions on registery entry
    let getRegistryKey baseKey subKey (writePermission: bool) =
        let x64BitKey =
            (getKey baseKey)
                .OpenSubKey(subKey, writePermission)

        if (isNull >> not) x64BitKey then
            x64BitKey
        else
            (get32BitKey baseKey)
                .OpenSubKey(subKey, writePermission) // fall back to 32 bit

    /// Gets a registry value as string
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    /// - `name` - The name of the registry entry
    ///
    /// ### Sample
    /// let AppType = Registry.getRegistryValue Registry.HKEYCurrentUser subkey values.[0]
    /// Trace.trace (sprintf "You are running the %s version" AppType)
    let getRegistryValue baseKey subKey name =
        use key = getRegistryKey baseKey subKey false

        if isNull key then
            failwithf "Registry subkey %s could not be found for key %A" subKey baseKey

        let value = key.GetValue name

        if isNull value then
            failwithf "Registry value is null for key %s" (key.ToString())

        value.ToString()

    /// Gets a registry value as string
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    /// - `name` - The name of the registry entry
    let getRegistryValue64 baseKey subKey name =
        use key = getRegistryKey64 baseKey subKey false

        if isNull key then
            failwithf "Registry subkey %s could not be found for key %A" subKey baseKey

        let value = key.GetValue name

        if isNull value then
            failwithf "Registry value is null for key %s" (key.ToString())

        value.ToString()

    /// Sets a registry value
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    /// - `name` - The name of the registry entry
    /// - `value` - The registry entry new value
    ///
    /// ### Sample
    /// Registry.setRegistryValue Registry.HKEYCurrentUser subkey "AppType" "Premium"
    /// Registry.setRegistryValue Registry.HKEYCurrentUser subkey "Version" "1.0.4"
    let setRegistryValue<'T> baseKey subKey name (value: 'T) =
        use key = getRegistryKey baseKey subKey true
        key.SetValue(name, value)

    /// Deletes the registry value from its key
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    /// - `name` - The name of the registry entry
    ///
    /// ### Sample
    /// Registry.deleteRegistryValue Registry.HKEYCurrentUser subkey "AppType"
    let deleteRegistryValue baseKey subKey name =
        use key = getRegistryKey baseKey subKey true
        key.DeleteValue name

    /// Returns all the value names of a registry key
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    ///
    /// ### Sample
    /// let values = Registry.getRegistryValueNames Registry.HKEYCurrentUser subkey
    /// values |> Array.iter (Trace.trace << (sprintf "Found value name: %s!"))
    let getRegistryValueNames baseKey subKey =
        use key = getRegistryKey baseKey subKey false
        key.GetValueNames()

    /// Returns whether or not a registry value name exists for a key
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    /// - `name` - The name of the registry entry
    ///
    /// ### Sample
    /// let exists b = if b then Trace.trace "It exists!" else Trace.trace "It doesn't exist!"
    /// exists <| Registry.valueExistsForKey Registry.HKEYCurrentUser subkey "DateCreated"
    /// exists <| Registry.valueExistsForKey Registry.HKEYCurrentUser subkey "Version"
    let valueExistsForKey =
        fun baseKey subKey name ->
            getRegistryValueNames baseKey subKey
            |> Seq.exists (fun n -> n = name)

    /// Create a registry subKey
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    ///
    /// ### Sample
    /// let subkey = "Company/MyApp"
    /// Registry.createRegistrySubKey Registry.HKEYCurrentUser subkey
    let createRegistrySubKey baseKey subKey =
        use key = getKey baseKey
        key.CreateSubKey subKey |> ignore

    /// Deletes a registry subKey
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    ///
    /// ### Sample
    /// Registry.deleteRegistrySubKey Registry.HKEYCurrentUser subkey
    let deleteRegistrySubKey baseKey subKey =
        use key = getKey baseKey
        key.DeleteSubKey subKey

    /// Returns all the subKey names of a registry key
    /// ## Parameters
    /// - `baseKey` - The registry value base key
    /// - `subKey` - The sub key
    ///
    /// ### Sample
    let getRegistrySubKeyNames baseKey subKey =
        use key = getRegistryKey baseKey subKey false
        key.GetSubKeyNames()
