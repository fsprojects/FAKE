namespace Fake.Windows

/// <summary>
/// Contains functions which allow to read and write information from/to the registry.
/// </summary>
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

    /// <summary>
    /// Maps the RegistryBaseKey to a RegistryKey
    /// </summary>
    /// [omit]
    let getKey name =
        match name with
        | HKEYLocalMachine -> Registry.LocalMachine
        | HKEYClassesRoot -> Registry.ClassesRoot
        | HKEYUsers -> Registry.Users
        | HKEYCurrentUser -> Registry.CurrentUser
        | HKEYCurrentConfig -> Registry.CurrentConfig
        | HKEYPerformanceData -> Registry.PerformanceData

    /// <summary>
    /// Maps the RegistryBaseKey to a RegistryKey for a 64bit System
    /// </summary>
    /// [omit]
    let get64BitKey name =
        match name with
        | HKEYLocalMachine -> RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
        | HKEYClassesRoot -> RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64)
        | HKEYUsers -> RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64)
        | HKEYCurrentUser -> RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
        | HKEYCurrentConfig -> RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry64)
        | HKEYPerformanceData -> RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Registry64)

    /// <summary>
    /// Maps the RegistryBaseKey to a RegistryKey for a 32bit System
    /// </summary>
    /// [omit]
    let get32BitKey name =
        match name with
        | HKEYLocalMachine -> RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
        | HKEYClassesRoot -> RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32)
        | HKEYUsers -> RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry32)
        | HKEYCurrentUser -> RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
        | HKEYCurrentConfig -> RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry32)
        | HKEYPerformanceData -> RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Registry32)

    /// <summary>
    /// Gets a 64-bit registry key
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    /// <param name="writePermission">The write permissions on registry entry</param>
    let getRegistryKey64 baseKey subKey (writePermission: bool) =
        (get64BitKey baseKey).OpenSubKey(subKey, writePermission)

    /// <summary>
    /// Gets a registry key and falls back to 32 bit if the 64bit key is not there
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    /// <param name="writePermission">The write permissions on registry entry</param>
    let getRegistryKey baseKey subKey (writePermission: bool) =
        let x64BitKey = (getKey baseKey).OpenSubKey(subKey, writePermission)

        if (isNull >> not) x64BitKey then
            x64BitKey
        else
            (get32BitKey baseKey).OpenSubKey(subKey, writePermission) // fall back to 32 bit

    /// <summary>
    /// Gets a registry value as string
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    /// <param name="name">The name of the registry entry</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// let AppType = Registry.getRegistryValue Registry.HKEYCurrentUser subkey values.[0]
    /// Trace.trace (sprintf "You are running the %s version" AppType)
    /// </code>
    /// </example>
    let getRegistryValue baseKey subKey name =
        use key = getRegistryKey baseKey subKey false

        if isNull key then
            failwithf "Registry subkey %s could not be found for key %A" subKey baseKey

        let value = key.GetValue name

        if isNull value then
            failwithf "Registry value is null for key %s" (key.ToString())

        value.ToString()

    /// <summary>
    /// Gets a registry value as string
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    /// <param name="name">The name of the registry entry</param>
    let getRegistryValue64 baseKey subKey name =
        use key = getRegistryKey64 baseKey subKey false

        if isNull key then
            failwithf "Registry subkey %s could not be found for key %A" subKey baseKey

        let value = key.GetValue name

        if isNull value then
            failwithf "Registry value is null for key %s" (key.ToString())

        value.ToString()

    /// <summary>
    /// Sets a registry value
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    /// <param name="name">The name of the registry entry</param>
    /// <param name="value">The registry entry new value</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Registry.setRegistryValue Registry.HKEYCurrentUser subkey "AppType" "Premium"
    /// Registry.setRegistryValue Registry.HKEYCurrentUser subkey "Version" "1.0.4"
    /// </code>
    /// </example>
    let setRegistryValue<'T> baseKey subKey name (value: 'T) =
        use key = getRegistryKey baseKey subKey true
        key.SetValue(name, value)

    /// <summary>
    /// Deletes the registry value from its key
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    /// <param name="name">The name of the registry entry</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Registry.deleteRegistryValue Registry.HKEYCurrentUser subkey "AppType"
    /// </code>
    /// </example>
    let deleteRegistryValue baseKey subKey name =
        use key = getRegistryKey baseKey subKey true
        key.DeleteValue name

    /// <summary>
    /// Returns all the value names of a registry key
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// let values = Registry.getRegistryValueNames Registry.HKEYCurrentUser subkey
    /// values |> Array.iter (Trace.trace &lt;&lt; (sprintf "Found value name: %s!"))
    /// </code>
    /// </example>
    let getRegistryValueNames baseKey subKey =
        use key = getRegistryKey baseKey subKey false
        key.GetValueNames()

    /// <summary>
    /// Returns whether or not a registry value name exists for a key
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    /// <param name="name">The name of the registry entry</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// let exists b = if b then Trace.trace "It exists!" else Trace.trace "It doesn't exist!"
    /// exists &lt;| Registry.valueExistsForKey Registry.HKEYCurrentUser subkey "DateCreated"
    /// exists &lt;| Registry.valueExistsForKey Registry.HKEYCurrentUser subkey "Version"
    /// </code>
    /// </example>
    let valueExistsForKey =
        fun baseKey subKey name -> getRegistryValueNames baseKey subKey |> Seq.exists (fun n -> n = name)

    /// <summary>
    /// Create a registry subKey
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// let subkey = "Company/MyApp"
    /// Registry.createRegistrySubKey Registry.HKEYCurrentUser subkey
    /// </code>
    /// </example>
    let createRegistrySubKey baseKey subKey =
        use key = getKey baseKey
        key.CreateSubKey subKey |> ignore

    /// <summary>
    /// Deletes a registry subKey
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Registry.deleteRegistrySubKey Registry.HKEYCurrentUser subkey
    /// </code>
    /// </example>
    let deleteRegistrySubKey baseKey subKey =
        use key = getKey baseKey
        key.DeleteSubKey subKey

    /// <summary>
    /// Returns all the subKey names of a registry key
    /// </summary>
    ///
    /// <param name="baseKey">The registry value base key</param>
    /// <param name="subKey">The sub key</param>
    let getRegistrySubKeyNames baseKey subKey =
        use key = getRegistryKey baseKey subKey false
        key.GetSubKeyNames()
