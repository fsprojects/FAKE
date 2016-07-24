[<AutoOpen>]
/// Contains tasks to create msi installers using the [WiX toolset](http://wixtoolset.org/)

module Fake.WiXHelper

open System
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions;
open System.Security.Cryptography
open System.Text


let mutable internal fileCount = 0
let mutable internal dirs = Dictionary()

let dirName dir = 
    match dirs.TryGetValue dir with
    | true, n -> 
        dirs.[dir] <- n + 1
        dir + n.ToString()
    | _ -> 
        dirs.[dir] <- 1
        dir

let mutable internal compRefs = Dictionary()

let compRefName compRef = 
    match compRefs.TryGetValue compRef with
    | true, n -> 
        compRefs.[compRef] <- n + 1
        compRef + n.ToString()
    | _ -> 
        compRefs.[compRef] <- 1
        compRef

let mutable internal comps = Dictionary()

let compName comp = 
    match comps.TryGetValue comp with
    | true, n -> 
        comps.[comp] <- n + 1
        comp + n.ToString()
    | _ -> 
        comps.[comp] <- 1
        comp

/// Creates a WiX File tag from the given FileInfo
let wixFile (fileInfo : FileInfo) = 
    fileCount <- fileCount + 1
    sprintf "<File Id=\"fi_%d\" Name=\"%s\" Source=\"%s\" />" fileCount fileInfo.Name fileInfo.FullName

/// Creates WiX File tags from the given files
let getFilesAsWiXString files = 
    files
    |> Seq.map (fileInfo >> wixFile)
    |> toLines

type Architecture = 
    | X64
    | X86
    override a.ToString() =
        match a with 
        | X64 -> "x64"
        | X86 -> "x86"

/// WiX File Element
type WiXFile =
    {
        /// File Id in WiX definition
        Id : string
        /// File Name in WiX definition
        Name : string
        /// File Path in WiX definition
        Source : string
        /// File Architecture, either X64 or X86, defaults to X64
        ProcessorArchitecture : Architecture
    }
    override w.ToString() = sprintf "<File Id=\"%s\" Name=\"%s\" Source=\"%s\" ProcessorArchitecture=\"%s\" />"
                                w.Id w.Name w.Source (w.ProcessorArchitecture.ToString())

/// Defaults for WiX file
let WiXFileDefaults = 
    {
        Id = "fi"
        Name = ""
        Source = ""
        ProcessorArchitecture = X64
    }

/// Specifies whether an action occur on install, uninstall or both.
type InstallUninstall = 
    | Install
    | Uninstall
    | Both
    | Never
    override w.ToString() = 
        match w with
        | Install -> "install"
        | Uninstall -> "uninstall"
        | Both -> "both"
        | Never -> null

/// These are used in many methods for generating WiX nodes, regard them as booleans
type YesOrNo = 
    | Yes
    | No
    override y.ToString() =
        match y with
        | Yes -> "yes"
        | No -> "no"

/// Service Control Element. Can Start, Stop and Remove services
type WiXServiceControl =
    {
        Id : string
        Name: string
        Remove : InstallUninstall
        Start : InstallUninstall
        Stop : InstallUninstall
        Wait : YesOrNo
    }
    member w.createAttributeList () =
        seq 
            {
                yield ("Id", w.Id)
                yield ("Name", w.Name)
                match w.Remove with
                | Never -> ()
                | _ -> yield ("Remove", w.Remove.ToString())
                match w.Start with
                | Never -> ()
                | _ -> yield ("Start", w.Start.ToString())
                match w.Stop with
                | Never -> ()
                | _ -> yield ("Stop", w.Stop.ToString())
                yield ("Wait", w.Wait.ToString())
            }
    override w.ToString() = 
        sprintf "<ServiceControl%s/>" 
            (Seq.fold(fun acc (key, value) -> acc + sprintf " %s=\"%s\"" key value) "" (w.createAttributeList()))             

/// Defaults for service control element
let WiXServiceControlDefaults =
    {
        Id = "ServiceControl"
        Name = "Service"
        Remove = Both
        Start = Install
        Stop = Both
        Wait = Yes
    }

/// Use this for generating service controls
let generateServiceControl (setParams : WiXServiceControl -> WiXServiceControl) =
    let parameters = WiXServiceControlDefaults |> setParams
    if parameters.Id = "" then 
        failwith "No parameter passed for service control Id!"
    parameters

/// Determines what action should be taken on an error.
type ErrorControl = 
    /// Logs the error and continues with the startup operation.
    | Ignore
    /// Logs the error, displays a message box and continues the startup operation.
    | Normal
    /// Logs the error if it is possible and the system is restarted with the last configuration known to be good. If the last-known-good configuration is being started, the startup operation fails.
    | Critical
    override w.ToString() = 
        match w with
        | Ignore -> "ignore"
        | Normal -> "normal"
        | Critical -> "critical"

/// Determines when the service should be started. The Windows Installer does not support boot or system. 
type ServiceInstallStart = 
    /// The service will start during startup of the system.
    | Auto
    /// The service will start when the service control manager calls the StartService function.
    | Demand
    /// The service can no longer be started.
    | Disabled
    /// The service is a device driver that will be started by the operating system boot loader. This value is not currently supported by the Windows Installer.
    | Boot
    /// The service is a device driver that will be started by the IoInitSystem function. This value is not currently supported by the Windows Installer.
    | System
    override w.ToString() = 
        match w with
        | Auto -> "auto"
        | Demand -> "demand"
        | Disabled -> "disabled"
        | Boot -> "boot"
        | System -> "system"

/// Determines the type of the service. The Windows Installer does not currently support kernelDriver or systemDriver.
type ServiceInstallType = 
    /// A Win32 service that runs its own process.
    | OwnProcess
    /// A Win32 service that shares a process.
    | ShareProcess
    /// A kernel driver service. This value is not currently supported by the Windows Installer.
    | KernelDriver
    /// A file system driver service. This value is not currently supported by the Windows Installer.
    | SystemDriver
    override w.ToString() = 
        match w with
        | OwnProcess -> "ownProcess"
        | ShareProcess -> "shareProcess"
        | KernelDriver -> "kernelDriver"
        | SystemDriver -> "systemDriver"

/// Service or group of services that must start before the parent service.
type WiXServiceDependency = 
    {
        /// [Required] The value of this attribute should be one of the following:
        /// 1. The name (not the display name) of a previously installed service.
        /// 2. The name of a service group (in which case the Group attribute must be set to 'yes').
        Id : string
        /// Set to 'yes' to indicate that the value in the Id attribute is the name of a group of services.	
        Group : YesOrNo option
    }
    member w.createAttributeList () =
        seq {           
            yield ("Id", w.Id)           
            if w.Group.IsSome then yield ("Group", w.Group.Value.ToString())
        }
    override w.ToString() =
        sprintf "<ServiceDependency%s />"
            (Seq.fold(fun acc (key, value) -> acc + sprintf " %s=\"%s\"" key value) "" (w.createAttributeList())) 

let WiXServiceDependencyDefaults =
    {       
        Id = ""
        Group = None
    }

/// Use this for generating service dependencies
let generateServiceDependency (setParams : WiXServiceDependency -> WiXServiceDependency) =
    let parameters = WiXServiceDependencyDefaults |> setParams
    if String.IsNullOrWhiteSpace parameters.Id then 
        failwith "No parameter passed for service dependency id!"
    parameters

/// Adds services for parent Component. Use the ServiceControl element to remove services.
type WiXServiceInstall =
    {
        /// Fully qualified names must be used even for local accounts, e.g.: ".\LOCAL_ACCOUNT". Valid only when ServiceType is ownProcess.
        Account : string
        /// Contains any command line arguments or properties required to run the service.
        Arguments : string
        /// Sets the description of the service.      
        Description : string
        /// This column is the localizable string that user interface programs use to identify the service.
        DisplayName: string
        /// Determines whether the existing service description will be ignored. If 'yes', the service description will be null, even if the Description attribute is set.
        EraseDescription : YesOrNo option
        /// [Required] Determines what action should be taken on an error. (Default: Normal)
        ErrorControl: ErrorControl
        /// Unique identifier for this service configuration. This value will default to the Name attribute if not specified.
        Id : string
        /// Whether or not the service interacts with the desktop.
        Interactive : YesOrNo option
        /// The load ordering group that this service should be a part of.
        LoadOrderGroup : string
        /// [Required] This column is the string that gives the service name to install.
        Name: string
        /// The password for the account. Valid only when the account has a password.	
        Password: string
        /// [Required] Determines when the service should be started. The Windows Installer does not support boot or system. (Default: Demand)
        Start : ServiceInstallStart        
        /// [Required] The Windows Installer does not currently support kernelDriver or systemDriver. (Default: OwnProcess)
        Type: ServiceInstallType        
        /// The overall install should fail if this service fails to install. (Default: Yes)
        Vital : YesOrNo
        /// Services or groups of services that must start before the parent service.
        ServiceDependencies : WiXServiceDependency seq

    }
    member w.createAttributeList () =
        seq {
            if not (String.IsNullOrWhiteSpace w.Account) then yield ("Account", w.Account)
            if not (String.IsNullOrWhiteSpace w.Arguments) then yield ("Arguments", w.Arguments)
            if not (String.IsNullOrWhiteSpace w.Description) then yield ("Description", w.Description)
            if not (String.IsNullOrWhiteSpace w.DisplayName) then yield ("DisplayName", w.DisplayName)
            if w.EraseDescription.IsSome then yield ("EraseDescription", w.EraseDescription.Value.ToString())
            yield ("ErrorControl", w.ErrorControl.ToString())            
            if not (String.IsNullOrWhiteSpace w.Id) then yield ("Id", w.Id)
            if w.Interactive.IsSome then yield ("Interactive", w.Interactive.Value.ToString())
            if not (String.IsNullOrWhiteSpace w.LoadOrderGroup) then yield ("LoadOrderGroup", w.LoadOrderGroup)
            yield ("Name", w.Name)
            if not (String.IsNullOrWhiteSpace w.Password) then yield ("Password", w.Password)
            yield ("Start", w.Start.ToString())
            yield ("Type", w.Type.ToString())
            yield ("Vital", w.Vital.ToString())
        }
    override w.ToString() = 
        sprintf "<ServiceInstall%s>%s</ServiceInstall>" 
            (Seq.fold(fun acc (key, value) -> acc + sprintf " %s=\"%s\"" key value) "" (w.createAttributeList())) 
            (Seq.fold(fun acc elem -> acc + elem.ToString()) "" w.ServiceDependencies)

/// Defaults for service install element
let WiXServiceInstallDefaults =
    {       
        Account = ""        
        Arguments = ""        
        Description = ""        
        DisplayName = ""        
        EraseDescription = None        
        ErrorControl = Normal        
        Id = ""        
        Interactive = None        
        LoadOrderGroup = ""        
        Name = ""
        Password = ""        
        Start = Demand        
        Type = OwnProcess        
        Vital = Yes        
        ServiceDependencies = []
    }

/// Use this for generating service installs
let generateServiceInstall (setParams : WiXServiceInstall -> WiXServiceInstall) =
    let parameters = WiXServiceInstallDefaults |> setParams
    if String.IsNullOrWhiteSpace parameters.Name then 
        failwith "No parameter passed for service name!"
    parameters

/// Represents the registry root under which this key should be written
type WiXRegistryRootType =
    /// Writes this registry key inside either HKEY_LOCAL_MACHINE or HKEY_CURRENT_USER. Wix decides at install time based on wether or not this is an "all users" install
    | HKMU
    /// Writes this registry key inside either the HKEY_CLASSES_ROOT registry root
    | HKCR
    /// Writes this registry key inside either the HKEY_CURRENT_USER registry root
    | HKCU
    /// Writes this registry key inside either the HKEY_LOCAL_MACHINE registry root
    | HKLM
    /// Writes this registry key inside either the HKEY_USers registry root
    | HKU
    override w.ToString() =
        match w with
        | HKMU -> "HKMU"
        | HKCR -> "HKCR"
        | HKCU -> "HKCU"
        | HKLM -> "HKLM"
        | HKU -> "HKU"

/// The action that will be taken for a registry value
type WiXRegistryValueAction = 
    /// Appends the specified value(s) to a multiString registry value
    | Append
    /// Prepends the specified value(s) to a multiString registry value
    | Prepend
    /// Writes a registry value
    | Write
    override a.ToString() =
        match a with
        | Append -> "append"
        | Prepend -> "prepend"
        | Write -> "write"

/// The desired type of a registry key.
type WiXRegistryValueType =
    /// The value is interpreted and stored as a string (REG_SZ)
    | String
    /// The value is interpreted and stored as an integer (REG_DWORD)
    | Integer
    /// The value is interpreted and stored as a hexadecimal value (REG_BINARY)
    | Binary
    /// The value is interpreted and stored as an expandable string (REG_EXPAND_SZ)
    | Expandable
    /// The value is interpreted and stored as a multiple strings (REG_MULTI_SZ)
    | MultiString
    override t.ToString() =
        match t with
        | String -> "string"
        | Integer -> "integer"
        | Binary -> "binary"
        | Expandable -> "expandable"
        | MultiString -> "multistring"

/// Parameters for WiX RegistryValue
type WiXRegistryValue =
    {
        /// The Id of this value
        Id : string
        /// The localizable registry value name. If this attribute is not provided the default value for the registry key will be set instead
        Name : string 
        /// The localizable registry value. 
        Value : string
        /// The action that will be taken for this registry value
        Action : WiXRegistryValueAction
        /// The type of the desired registry key
        Type : WiXRegistryValueType
        /// The localizable key for the registry value
        /// If the parent element is a RegistryKey, this value may be omitted to use the path of the parent, or if its specified it will be appended to the path of the parent
        Key : string
        /// Set this attribute to 'yes' to make this registry key the KeyPath of the parent component
        KeyPath : YesOrNo
        /// The predefined root key for the registry value.
        Root : WiXRegistryRootType Option
    }
    member v.createAttributeList () =
        seq {
            if not (String.IsNullOrWhiteSpace v.Id) then yield ("Id", v.Id)
            if not (String.IsNullOrWhiteSpace v.Name) then yield ("Name", v.Name)
            if not (String.IsNullOrWhiteSpace v.Key) then yield ("Key", v.Key)
            if not (Option.isNone v.Root) then yield ("Root", v.Root.Value.ToString())
            yield ("Type", v.Type.ToString())
            yield ("Value", v.Value)
            yield ("KeyPath", v.KeyPath.ToString())
        }
    override v.ToString() = 
        sprintf "<RegistryValue%s />" 
            (Seq.fold(fun acc (key, value) -> acc + sprintf " %s=\"%s\"" key value) "" (v.createAttributeList())) 
    
let wixRegistryValueDefaults =
    {
        Id = ""
        Name = ""
        Value = ""
        Type = WiXRegistryValueType.String
        Action = WiXRegistryValueAction.Write
        Key = ""
        KeyPath = YesOrNo.No
        Root = None
    }

/// Generates a registry value based on the given parameters, use toString on it when embedding it
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
/// let registryValue = generateRegistryValue(fun v -> 
///                                               {v with
///                                                   Id = "asdasd"
///                                                   Name = "Something"
///                                                   Key = "Somewhere"
///                                                   Root = Some WiXRegistryRootType.HKU
///                                                   Type = WiXRegistryValueType.Integer
///                                                   KeyPath = YesOrNo.No
///                                                   Value = "2"
///                                               })

let generateRegistryValue (setParams : WiXRegistryValue -> WiXRegistryValue) =
    let parameters = wixRegistryValueDefaults |> setParams
    parameters

/// Parameters for WiX RegistryKey
type WiXRegistryKey =
    {
        /// Primary key used to identify this particular entry
        Id : string
        /// The predefined root key for the registry value
        Root : WiXRegistryRootType Option
        /// The localizable key for the registry value
        /// If the parent element is a RegistryKey, this value may be omitted to use the path of the parent, or if its specified it will be appended to the path of the parent
        Key : string
        /// Set this attribute to 'yes' to create an empty key, if absent, when the parent component is installed
        /// This value is needed only to create an empty key with no subkeys or values.
        /// Windows Installer creates keys as needed to store subkeys and values. The default is "no"
        ForceCreateOnInstall : YesOrNo
        /// Set this attribute to 'yes' to remove the key with all its values and subkeys when the parent component is uninstalled
        /// Note that this value is useful only if your program creates additional values or subkeys under this key and you want an uninstall to remove them
        /// MSI already removes all values and subkeys that it creates, so this option just adds additional overhead to uninstall. The default is "no"
        ForceDeleteOnUninstall : YesOrNo
        /// You can nest child registry keys here
        Keys : WiXRegistryKey seq
        /// You can nest child registry values here
        Values : WiXRegistryValue seq
    }
    member k.createAttributeList () = 
        seq {
            if not (String.IsNullOrWhiteSpace k.Id) then yield ("Id", k.Id)
            if not (String.IsNullOrWhiteSpace k.Key) then yield ("Key", k.Key)
            if not (Option.isNone k.Root) then yield ("Root", k.Root.Value.ToString())
            yield ("ForceCreateOnInstall", k.ForceCreateOnInstall.ToString())
            yield ("ForceDeleteOnUninstall", k.ForceDeleteOnUninstall.ToString())
        }
    override k.ToString() = 
          sprintf "<RegistryKey%s>%s%s</RegistryKey>" 
              (Seq.fold(fun acc (key, value) -> acc + sprintf " %s=\"%s\"" key value) "" (k.createAttributeList())) 
              (Seq.fold(fun acc elem -> acc + elem.ToString()) "" k.Keys) 
              (Seq.fold(fun acc elem -> acc + elem.ToString()) "" k.Values)
    
let wixRegistryKeyDefaults =
    {
        Id = ""
        Root = None
        Key = ""
        ForceCreateOnInstall = YesOrNo.No
        ForceDeleteOnUninstall = YesOrNo.No
        Keys = Seq.empty
        Values = Seq.empty
    }

/// Generates a registry key based on the given parameters, use toString on it when embedding it
/// You can pass other registry keys and values into RegistryKeys or RegistryValues for making a hierarchy
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
/// let key = generateRegistryKey(fun k ->
///                                 {k with
///                                   Id = "KeyId"
///                                   Key = "SomeKey"
///                                   Root = Some WiXRegistryRootType.HKCR
///                                   ForceCreateOnInstall = YesOrNo.Yes
///                                   ForceDeleteOnUninstall = YesOrNo.No
///                                   Keys = someChildKeys
///                                   Values = someChildValues
///                                 })
let generateRegistryKey (setParams : WiXRegistryKey -> WiXRegistryKey) =
    let parameters = wixRegistryKeyDefaults |> setParams
    parameters

/// Reference to a component for including it in a feature
type WiXComponentRef =
    {
        Id : string
    }
    override w.ToString() = sprintf "<ComponentRef Id=\"%s\" />" w.Id

/// Defaults for component ref
let WiXComponentRefDefaults =
    {
        Id = ""
    }

/// Use this for generating component refs
let generateComponentRef (setParams : WiXComponentRef -> WiXComponentRef) =
    let parameters = WiXComponentRefDefaults |> setParams
    if parameters.Id = "" then 
        failwith "No parameter passed for component ref Id!"
    Some(parameters)


type WiXDirectoryComponent = 
    | C of WiXComponent
    | D of WiXDir      
    member w.ToComponentRef() = 
        match w with
        | C c -> c.ToComponentRef()
        | D d -> None  
    override w.ToString() =
          match w with
            | C c -> c.ToString()
            | D d -> d.ToString()
/// Component which wraps files into logical components and which allows to 
and WiXComponent = 
    {
        Id : string
        Guid : string
        Files : WiXFile seq
        Win64 : YesOrNo
        ServiceControls : WiXServiceControl seq
        ServiceInstalls : WiXServiceInstall seq
        RegistryKeys : WiXRegistryKey seq
        RegistryValues : WiXRegistryValue seq
    }
    member w.ToComponentRef() = generateComponentRef (fun f -> { f with Id = w.Id })
    override w.ToString() = sprintf "<Component Id=\"%s\" Guid=\"%s\" Win64=\"%s\">%s%s%s%s%s</Component>" 
                                w.Id w.Guid (w.Win64.ToString())
                                (Seq.fold(fun acc elem -> acc + elem.ToString()) "" w.Files) 
                                (Seq.fold(fun acc elem -> acc + elem.ToString()) "" w.ServiceControls)
                                (Seq.fold(fun acc elem -> acc + elem.ToString()) "" w.ServiceInstalls)
                                (Seq.fold(fun acc elem -> acc + elem.ToString()) "" w.RegistryKeys)
                                (Seq.fold(fun acc elem -> acc + elem.ToString()) "" w.RegistryValues)
                                
/// WiX Directories define a logical directory which can include components and files
and WiXDir = 
    {
        Id : string
        Name : string
        Files : WiXFile seq
        Components : WiXDirectoryComponent seq
    }
    override d.ToString() = sprintf "<Directory Id=\"%s\" Name=\"%s\">%s%s</Directory>"
                                d.Id 
                                d.Name 
                                (Seq.fold(fun acc elem -> acc + elem.ToString()) "" d.Files) 
                                (Seq.fold(fun acc elem -> (                                                            
                                                            acc + match elem with
                                                                    | C c -> c.ToString()
                                                                    | D d -> d.ToString()
                                                           )) "" d.Components)

/// Reference to a component for including it in a feature
type WiXDirectoryRef =
   {
       Id : string
       Components : WiXDirectoryComponent seq
   }
   override r.ToString() = sprintf "<DirectoryRef Id=\"%s\">%s</DirectoryRef>" 
                              r.Id
                              (Seq.fold(fun acc elem -> acc + elem.ToString()) "" r.Components)

/// Defaults for component ref
let WiXDirectoryRefDefaults =
   {
       Id = ""
       Components = []
   }

/// Use this for generating component refs
let generateDirectoryRef (setParams : WiXDirectoryRef -> WiXDirectoryRef) =
   let parameters = WiXDirectoryRefDefaults |> setParams
   if parameters.Id = "" then 
       failwith "No parameter passed for component ref Id!"
   parameters

///get component refs from a directory component hierarchy
let rec getComponentRefs (elements : WiXDirectoryComponent seq) = 
    let refs = elements
                |> Seq.choose (fun e -> 
                                match e with
                                | D d -> Some (d)
                                | _ -> None)
                |> Seq.map (fun d -> getComponentRefs d.Components)
                |> Seq.concat
    let cRefs = elements
                |> Seq.choose (fun e -> 
                                match e with
                                | C c -> Some c
                                | _ -> None)
                |> Seq.map (fun c -> c.ToComponentRef())
    Seq.append refs cRefs
    

/// Defaults for component
let WiXComponentDefaults =
    {
        Id = ""
        Guid = "*"
        Win64 = Yes
        Files = []
        ServiceControls = []
        ServiceInstalls = []
        RegistryKeys = []
        RegistryValues = []
    }

/// Use this for generating single components
let generateComponent (setParams : WiXComponent -> WiXComponent) =
    let parameters = WiXComponentDefaults |> setParams
    if parameters.Id = "" then 
        failwith "No parameter passed for component Id!"
    parameters

/// Defaults for directories
let WiXDirDefaults = 
    {
        Id = ""
        Name = ""
        Files = []
        Components = []
    }

/// Use this for generating directories
let generateDirectory (setParams : WiXDir -> WiXDir) =
    let parameters = WiXDirDefaults |> setParams
    if parameters.Id = "" then 
        failwith "No parameter passed for directory Id!"
    parameters

/// Calculates the SHA1 for a given string.
let private calcSHA1 (text:string) =
    Fake.EnvironmentHelper.encoding.GetBytes text
      |> (new SHA1CryptoServiceProvider()).ComputeHash
      |> Array.fold (fun acc e -> 
           let t = System.Convert.ToString(e, 16)
           if t.Length = 1 then acc + "0" + t else acc + t) 
           ""
let private getDirectoryId (directoryName : string) =        
    "d" + calcSHA1 directoryName

let private getFileId (fileName : string) =
    "f" + calcSHA1 fileName
    
let private IsWin64 architecture =
    match architecture with
    | X64 -> Yes
    | X86 -> No

let private createComponents fileFilter directoryInfo directoryName architecture =
    directoryInfo
        |> filesInDir
        |> Seq.filter fileFilter
        |> Seq.map (fun file -> 
                        { 
                            Id = getFileId (directoryName + directoryInfo.Name + file.Name)
                            Name = file.Name
                            Source = file.FullName
                            ProcessorArchitecture = architecture
                        })
        |> Seq.map(fun file->
                        C{
                            Id = "c" + file.Id.Substring(1)
                            Guid = "*"
                            Win64 = IsWin64 architecture
                            Files = [file]
                            ServiceControls = []
                            ServiceInstalls = []
                            RegistryKeys = []
                            RegistryValues = []
                        })

/// Creates a WiX directory and component hierarchy from the given DirectoryInfo
/// The function will create one component for each file [best practice](https://support.microsoft.com/de-de/kb/290997/en-us)
/// and set the GUID to "*", which will make WiX produce consistent Component Guids if the Component's target path doesn't change.
/// This is vital for major upgrades, since windows installer needs a consistent component guid for tracking each of them.
/// You can use the getComponentRefs function for getting all created component refs and adding them to features.
/// You can use attachServiceControlToComponents or attachServiceInstallToComponents to attach ServiceControl or ServiceInstall to the directory component hierarchy
let rec bulkComponentTreeCreation fileFilter directoryFilter directoryInfo architecture =
    let directoryName = ""
    let directories = directoryInfo
                      |> subDirectories
                      |> Seq.filter directoryFilter
                      |> Seq.map (fun d -> bulkComponentTreeSubCreation fileFilter directoryFilter d directoryInfo.Name architecture)
    let components = createComponents fileFilter directoryInfo directoryName architecture
    Seq.append directories components  
and private bulkComponentTreeSubCreation fileFilter directoryFilter directoryInfo directoryName architecture =    
    let directories = directoryInfo
                      |> subDirectories
                      |> Seq.filter directoryFilter
                      |> Seq.map (fun d -> bulkComponentTreeSubCreation fileFilter directoryFilter d (directoryName + directoryInfo.Name) architecture)       
    let components = createComponents fileFilter directoryInfo directoryName architecture
    let currentDirectory = D{
        Id = getDirectoryId (directoryInfo.Name + directoryName)
        Name = directoryInfo.Name
        Files = []
        Components = Seq.append directories components
    }    
    currentDirectory

/// Creates WiX component with directories and files from the given DirectoryInfo
/// The function will create one component for each file [best practice](https://support.microsoft.com/de-de/kb/290997/en-us)
/// and set the GUID to "*", which will make WiX produce consistent Component Guids if the Component's target path doesn't change.
/// This is vital for major upgrades, since windows installer needs a consistent component guid for tracking each of them.
/// You can use the getComponentIdsFromWiXString function for getting all created component refs and adding them to features.
let bulkComponentCreation fileFilter directoryInfo architecture = 
    directoryInfo
        |> filesInDir
        |> Seq.filter fileFilter
        |> Seq.map (fun file -> 
                        { 
                            Id = getFileId(file.FullName)
                            Name = file.Name
                            Source = file.FullName
                            ProcessorArchitecture = architecture
                        })
        |> Seq.map(fun file->
                        C{
                            Id = "c" + file.Id.Substring(1)
                            Guid = "*"
                            Win64 = IsWin64 architecture
                            Files = [file]
                            ServiceControls = []
                            ServiceInstalls = []
                            RegistryKeys = []
                            RegistryValues = []
                        })

/// Creates WiX component with directories and files from the given DirectoryInfo
/// The function will create one component for each file [best practice](https://support.microsoft.com/de-de/kb/290997/en-us)
/// and set the GUID to "*", which will make WiX produce consistent Component Guids if the Component's target path doesn't change.
/// This is vital for major upgrades, since windows installer needs a consistent component guid for tracking each of them.
/// The components are embedded into the passed in root directory.
let bulkComponentCreationAsSubDir fileFilter (directoryInfo : DirectoryInfo) architecture = 
    {
        Id = getDirectoryId(directoryInfo.FullName)
        Name = directoryInfo.Name 
        Files = []
        Components = bulkComponentCreation fileFilter directoryInfo architecture
    }  
                 
///// Use this to attach service controls to your components.   
let rec attachServiceControlToComponent (comp : WiXDirectoryComponent) fileFilter serviceControls = 
    match comp with
    | C c -> C (if fileFilter c then                        
                        { Id = c.Id; Guid = c.Guid; Files = c.Files; ServiceControls = Seq.append c.ServiceControls serviceControls; ServiceInstalls = c.ServiceInstalls; RegistryKeys = c.RegistryKeys; RegistryValues = c.RegistryValues; Win64 = c.Win64 }
                        else
                            c
                        )                                          
    | D d -> D({Id = d.Id; Name = d.Name; Files = d.Files; Components = (attachServiceControlToComponents d.Components fileFilter serviceControls)})
and attachServiceControlToComponents (components : WiXDirectoryComponent seq) fileFilter serviceControls = 
    components 
    |> Seq.map(fun c -> attachServiceControlToComponent c fileFilter serviceControls)
            
/// Use this to attach service installs to your components.          
let rec attachServiceInstallToComponent (comp : WiXDirectoryComponent) fileFilter serviceInstalls = 
    match comp with
    | C c -> C (if fileFilter c then                        
                        { Id = c.Id; Guid = c.Guid; Files = c.Files; ServiceControls = c.ServiceControls; ServiceInstalls = Seq.append c.ServiceInstalls serviceInstalls; RegistryKeys = c.RegistryKeys; RegistryValues = c.RegistryValues; Win64 = c.Win64 }
                        else
                            c
                        )                                          
    | D d -> D({Id = d.Id; Name = d.Name; Files = d.Files; Components = (attachServiceInstallToComponents d.Components fileFilter serviceInstalls)})
and attachServiceInstallToComponents (components : WiXDirectoryComponent seq) fileFilter serviceInstalls = 
    components 
    |> Seq.map(fun c -> attachServiceInstallToComponent c fileFilter serviceInstalls)
                            
/// Creates recursive WiX directory and file tags from the given DirectoryInfo
/// The function will create one component for each file [best practice](https://support.microsoft.com/de-de/kb/290997/en-us)
/// and set the GUID to "*", which will make WiX produce consistent Component Guids if the Component's target path doesn't change.
/// This is vital for major upgrades, since windows installer needs a consistent component guid for tracking each of them.
/// You can use the getComponentIdsFromWiXString function for getting all created component refs and adding them to features.
let rec wixDir fileFilter asSubDir (directoryInfo : DirectoryInfo) = 
    let dirs = 
        directoryInfo
        |> subDirectories
        |> Seq.map (wixDir fileFilter true)
        |> toLines
    
    let files = 
        directoryInfo
        |> filesInDir
        |> Seq.filter fileFilter
        |> Seq.map wixFile
        |> toLines
    
    let compo = 
        if files = "" then ""
        else 
            split '\n' files
            |> Seq.map(fun f -> sprintf "<Component Id=\"c%s\" Guid=\"%s\">\r\n%s\r\n</Component>\r\n" (compName (directoryInfo.Name.GetHashCode().ToString("x8"))) "*" f)
            |> toLines

    if asSubDir then 
        sprintf "<Directory Id=\"d%s\" Name=\"%s\">\r\n%s%s\r\n</Directory>\r\n" (dirName (directoryInfo.Name.GetHashCode().ToString("x8"))) 
            directoryInfo.Name dirs compo
    else sprintf "%s%s" dirs compo

/// Retrieves the file id of the first file in WiXString, which name matches fileRegex
/// ## Parameters
///  - `wiXString` - The directory string which was generated by wixDir
///  - `fileRegex` - Regex which matches the file name
///
/// ## Sample
///     let directoryString = wixDir (fun file -> true) true (DirectoryInfo directoryWithFilesForSetup)
///     let executableFileId = getFileIdFromWiXString directoryString "\S*.exe"
let getFileIdFromWiXString wiXString fileRegex =
    let lines = split '\n' wiXString

    // Filter for lines which have a name tag matching the given regex, pick the first and return its ID
    lines
        |> Seq.filter(fun line -> Regex.IsMatch(line, "Name=\"" + fileRegex + "\""))
        |> Seq.head
        // Substring starts immediately after "Id=" tag and is as long as the given file id
        |> fun f -> f.Substring(f.IndexOf("Id=") + 4, Regex.Match(f, "Id=\"[^\"]*\"").Length - 5)


/// Retrieves all component ids from given WiX directory string
/// ## Parameters
///  - `wiXString` - The directory string which was generated by wixDir
///
/// ## Sample
///     let directoryString = wixDir (fun file -> true) true (DirectoryInfo directoryWithFilesForSetup)
///     let componentIds = getComponentIdsFromWiXString directoryString
let getComponentIdsFromWiXString wiXString =
    let lines = split '\n' wiXString

    // Filter for lines which have a name tag matching the given regex, pick the first and return its ID
    lines
        |> Seq.filter(fun line -> Regex.IsMatch(line, "<Component"))
        |> Seq.map(fun f -> sprintf "<ComponentRef Id=\"%s\" />" (f.Substring(f.IndexOf("Id=") + 4, Regex.Match(f, "Id=\"[^\"]*\"").Length - 5)))
        |> System.String.Concat

/// Creates WiX ComponentRef tags from the given DirectoryInfo
let rec wixComponentRefs (directoryInfo : DirectoryInfo) = 
    let compos = 
        directoryInfo
        |> subDirectories
        |> Seq.map wixComponentRefs
        |> toLines
    if (filesInDir directoryInfo).Length > 0 then 
        sprintf "%s<ComponentRef Id=\"%s\"/>\r\n" compos (compRefName directoryInfo.Name)
    else compos

/// Take a component string and set "neverOverwrite" Tag
/// This is useful for config files, since they are not replaced on upgrade like that
let setComponentsNeverOverwrite (components : string) = 
    components.Replace("<Component", "<Component NeverOverwrite=\"yes\"")

open System

/// WiX parameter type
type WiXParams = 
    { ToolDirectory : string
      TimeOut : TimeSpan
      AdditionalCandleArgs : string list
      AdditionalLightArgs : string list }

/// Contains the WiX default parameters  
let WiXDefaults : WiXParams = 
    { ToolDirectory = currentDirectory @@ "tools" @@ "Wix"
      TimeOut = TimeSpan.FromMinutes 5.0
      AdditionalCandleArgs = [ "-ext WiXNetFxExtension" ]
      AdditionalLightArgs = [ "-ext WiXNetFxExtension"; "-ext WixUIExtension.dll"; "-ext WixUtilExtension.dll" ] }

/// Used for determing whether the feature should be visible in the select features installer pane or not
type FeatureDisplay = 
    /// Initially shows the feature collapsed. This is the default value.
    | Collapse
    /// Initially shows the feature expanded.
    | Expand
    /// Prevents the feature from displaying in the user interface.
    | Hidden
    override f.ToString() =
        match f with 
        | Collapse -> "collapse"
        | Expand -> "expand"
        | Hidden -> "hidden"

/// Parameters for creating WiX Feature, use ToString for creating the string xml nodes
[<Obsolete("Please use the new Feature type which features automatic string concatenation of inner features")>]
type WiXFeature = 
    {
        /// Unique identifier of the feature.
        Id : string

        /// Short string of text identifying the feature. 
        /// This string is listed as an item by the SelectionTree control of the Selection Dialog. 
        Title : string

        /// Sets the install level of this feature. A value of 0 will disable the feature. 
        /// Processing the Condition Table can modify the level value (this is set via the Condition child element).
        /// The default value is "1". 
        Level : int

        /// Longer string of text describing the feature. This localizable string is displayed by the Text Control of the Selection Dialog. 
        Description : string

        ///Determines the initial display of this feature in the feature tree. This attribute's value should be one of the following:
        ///collapse
        ///    Initially shows the feature collapsed. This is the default value.
        ///expand
        ///    Initially shows the feature expanded.
        ///hidden
        ///    Prevents the feature from displaying in the user interface.
        ///<an explicit integer value>
        ///    For advanced users only, it is possible to directly set the integer value of the display value that will appear in the Feature row. 
        Display : FeatureDisplay

        /// Nest sub features or components in here
        InnerContent : string 
    }
    override f.ToString() = "<Feature Id=\"" + f.Id + "\" Title=\"" + f.Title + "\" Level=\"" + f.Level.ToString() + "\" Description=\"" + f.Description + "\" Display=\"" 
                            + f.Display.ToString() + "\" ConfigurableDirectory=\"INSTALLDIR\">" + f.InnerContent + "</Feature>"

/// Default values for creating WiX Feature
[<Obsolete("Please use the new Feature type which features automatic string concatenation of inner features")>]
let WiXFeatureDefaults =
    {   
        Id = ""
        Title = "Default Feature"
        Level = 1
        Description = "Default Feature"
        Display = FeatureDisplay.Expand
        InnerContent = ""
    }

/// Parameters for creating WiX Feature, use ToString for creating the string xml nodes
type Feature =
    {
         /// Unique identifier of the feature.
        Id : string

        /// Short string of text identifying the feature. 
        /// This string is listed as an item by the SelectionTree control of the Selection Dialog. 
        Title : string

        /// Sets the install level of this feature. A value of 0 will disable the feature. 
        /// Processing the Condition Table can modify the level value (this is set via the Condition child element).
        /// The default value is "1". 
        Level : int

        /// Longer string of text describing the feature. This localizable string is displayed by the Text Control of the Selection Dialog. 
        Description : string

        ///Determines the initial display of this feature in the feature tree. This attribute's value should be one of the following:
        ///collapse
        ///    Initially shows the feature collapsed. This is the default value.
        ///expand
        ///    Initially shows the feature expanded.
        ///hidden
        ///    Prevents the feature from displaying in the user interface.
        ///<an explicit integer value>
        ///    For advanced users only, it is possible to directly set the integer value of the display value that will appear in the Feature row. 
        Display : FeatureDisplay

        /// Nest sub features
        NestedFeatures : Feature seq

        /// Components included in this feature
        Components : WiXComponentRef option seq
    }
    override f.ToString() =
        let (|Empty|NotEmpty|) seq = if Seq.isEmpty seq then Empty else NotEmpty seq

        let rec ConcatAll feature (node : string) = 
            match feature.NestedFeatures with
            | Empty ->  "<Feature Id=\"" + feature.Id + "\" Title=\"" + feature.Title + "\" Level=\"" + feature.Level.ToString() + "\" Description=\"" + feature.Description + "\" Display=\"" + feature.Display.ToString() + "\" ConfigurableDirectory=\"INSTALLDIR\">" + (feature.Components |> Seq.choose id |> Seq.fold(fun acc elem -> acc + elem.ToString()) "") + "</Feature>"
            | NotEmpty list -> "<Feature Id=\"" + feature.Id + "\" Title=\"" + feature.Title + "\" Level=\"" + feature.Level.ToString() + "\" Description=\"" + feature.Description + "\" Display=\"" + feature.Display.ToString() + "\" ConfigurableDirectory=\"INSTALLDIR\">" + Seq.fold(fun acc elem -> acc + ConcatAll elem "") "" list + (feature.Components |> Seq.choose id |> Seq.fold(fun acc elem -> acc + elem.ToString()) "") + "</Feature>"
        ConcatAll f ""

/// Default values for creating WiX Feature
let FeatureDefaults =
    {   
        Id = ""
        Title = "Default Feature"
        Level = 1
        Description = "Default Feature"
        Display = FeatureDisplay.Expand
        NestedFeatures = Seq.empty<Feature>
        Components = []
    }

/// Type for defining, which program directory should be used for installation. ProgramFiles32 refers to 'Program Files (x86)', ProgramFiles64 refers to 'Program Files'
type ProgramFilesFolder = 
    | ProgramFiles32
    | ProgramFiles64
    override p.ToString() = 
        match p with 
        | ProgramFiles32 -> "ProgramFilesFolder"
        | ProgramFiles64 -> "ProgramFiles64Folder"

/// Parameters for WiX Script properties, use ToString for creating the string xml nodes
[<Obsolete("Please use new 'Script' type")>]
type WiXScript =
    {
        /// The product code GUID for the product.
        ProductCode : Guid

        /// The descriptive name of the product.
        ProductName : string

        /// The program files folder
        ProgramFilesFolder : ProgramFilesFolder

        /// Product description
        Description : string

        /// The decimal language ID (LCID) for the product.
        ProductLanguage : string

        /// The product's version string.
        ProductVersion : string

        /// The manufacturer of the product.
        ProductPublisher : string

        /// The upgrade code GUID for the product.
        UpgradeGuid : Guid

        /// You can nest upgrade elements in here
        Upgrade : string

        /// Nest major upgrade elements in here
        MajorUpgrade : string

        /// Nest UIRefs in here
        UIRefs : string

        /// Nest WiXVariables in here
        WiXVariables : string

        /// Nest directories in here
        Directories : string

        /// Build Number of product
        BuildNumber : string

        /// You can nest feature elements in here
        Features : string

        /// You can nest custom actions in here
        CustomActions : string

        /// You can nest InstallExecuteSequence actions in here
        ActionSequences : string

        /// Specify architecture of package. For 64Bit Setups set ProgramFilesFolder to ProgramFiles64, package platform to X64, all components to Win64 = yes and all files' processorArchitecture to X64.
        Platform : Architecture
    }

/// Default values for WiX Script properties
[<Obsolete("Please use new 'Script' type")>]
let WiXScriptDefaults = 
    {
        ProductCode = Guid.Empty
        ProductName = ""
        ProgramFilesFolder = ProgramFilesFolder.ProgramFiles32
        Description = ""
        ProductLanguage = ""
        ProductVersion = ""
        ProductPublisher = ""
        UpgradeGuid = Guid.Empty
        Upgrade = ""
        MajorUpgrade = ""
        UIRefs = ""
        WiXVariables = ""
        Directories = ""
        BuildNumber = "1.0.0"
        Features = ""
        CustomActions = ""
        ActionSequences = ""
        Platform = X86
    }

/// Used in WiXCustomAction for determing when to run the custom action
type CustomActionExecute = 
    /// Indicates that the custom action will run after successful completion of the installation script (at the end of the installation). 
    | Commit
    /// Indicates that the custom action runs in-script (possibly with elevated privileges). 
    | Deferred
    /// Indicates that the custom action will only run in the first sequence that runs it. 
    | FirstSequence
    /// Indicates that the custom action will run during normal processing time with user privileges. This is the default. 
    | Immediate
    /// Indicates that the custom action will only run in the first sequence that runs it in the same process. 
    | OncePerProcess
    /// Indicates that a custom action will run in the rollback sequence when a failure occurs during installation, usually to undo changes made by a deferred custom action. 
    | Rollback
    /// Indicates that a custom action should be run a second time if it was previously run in an earlier sequence. 
    | SecondSequence
    override c.ToString() =
        match c with 
        | Commit -> "commit"
        | Deferred -> "deferred"
        | FirstSequence -> "firstSequence"
        | Immediate -> "immediate"
        | OncePerProcess -> "oncePerProcess"
        | Rollback -> "rollback"
        | SecondSequence -> "secondSequence"

/// Used in WiXCustomAction for determing the return type
type CustomActionReturn = 
    /// Indicates that the custom action will run asyncronously and execution may continue after the installer terminates. 
    | AsyncNoWait
    /// Indicates that the custom action will run asynchronously but the installer will wait for the return code at sequence end. 
    | AsyncWait
    /// Indicates that the custom action will run synchronously and the return code will be checked for success. This is the default. 
    | Check
    /// Indicates that the custom action will run synchronously and the return code will not be checked. 
    | Ignore
    override c.ToString() =
        match c with
        | AsyncNoWait -> "asyncNoWait"
        | AsyncWait -> "asyncWait"
        | Check -> "check"
        | Ignore -> "ignore"

/// Parameters for WiX custom action, use ToString for creating the string xml nodes
type WiXCustomAction = 
    {
        ///	The identifier of the custom action. 
        Id : string

        /// This attribute specifies a reference to a File element with matching Id attribute that will execute the custom action code 
        /// in the file after the file is installed. This attribute is typically used with the ExeCommand attribute to specify 
        /// a type 18 custom action that runs an installed executable, with the DllEntry attribute to specify an installed custom action 
        /// DLL to use for a type 17 custom action, or with the VBScriptCall or JScriptCall attributes to specify a type 21 or 22 custom action. 
        FileKey : string

        /// This attribute indicates the scheduling of the custom action.
        Execute : CustomActionExecute
        /// This attribute specifies whether the Windows Installer, which executes as LocalSystem, should impersonate the user context of 
        /// the installing user when executing this custom action. Typically the value should be 'yes', except when the custom action needs 
        /// elevated privileges to apply changes to the machine. 
        Impersonate : YesOrNo
        /// This attribute specifies the command line parameters to supply to an externally run executable. 
        /// This attribute is typically used with the BinaryKey attribute for a type 2 custom action, the FileKey attribute for a type 18 
        /// custom action, the Property attribute for a type 50 custom action, or the Directory attribute for a type 34 custom action that 
        /// specify the executable to run. 
        ExeCommand : string
        /// Set this attribute to set the return behavior of the custom action. 
        Return : CustomActionReturn
    } 
    override w.ToString() = "<CustomAction Id=\"" + w.Id + "\" FileKey=\"" + w.FileKey + "\" Execute=\"" + w.Execute.ToString() + "\" Impersonate=\"" + w.Impersonate.ToString() + "\" ExeCommand=\""
                            + w.ExeCommand + "\" Return=\"" + w.Return.ToString() + "\" />"

/// Default values for WiX custom actions
let WiXCustomActionDefaults = 
    {
        Id = ""
        FileKey = ""
        Execute = CustomActionExecute.Immediate
        Impersonate = YesOrNo.Yes
        ExeCommand = ""
        Return = CustomActionReturn.Check
    }

/// Used for specifying the point of time for action execution in WiXCustomActionExecution
type ActionExecutionVerb = 
    /// Specifies that action should be executed after some standard or custom action
    | After
    /// Specifies that action should be executed before some standard or custom action
    | Before
    override a.ToString() =
        match a with
        | After -> "After"
        | Before -> "Before"

/// Parameters for WiX Custom Action executions (In InstallExecuteSequence), use ToString for creating the string xml nodes
type WiXCustomActionExecution = 
    {
        /// The action to which the Custom element applies.
        ActionId : string
        /// Specify if action should be executed before or after target action
        Verb : ActionExecutionVerb
        /// Name of the standard or custom action that the verb points to
        Target : string
        /// Conditions that have to be fulfilled for running execution
        Condition : string
    }
    override w.ToString() = "<Custom Action=\"" + w.ActionId + "\" " + w.Verb.ToString() + "=\"" + w.Target + "\"> " + w.Condition + " </Custom>"

/// Default values for WiX custom action executions
let WixCustomActionExecutionDefaults = 
    {
        ActionId = ""
        Verb = ActionExecutionVerb.After
        Target = ""
        Condition = ""
    }

/// Parameters for WiX UI Reference, use ToString for creating the string xml nodes
type WiXUIRef = 
    {   
        /// Name of referenced UI
        Id : string
    }
    override w.ToString() = "<UIRef Id=\"" + w.Id + "\" />"

/// Default value for WiX UI Reference (WixUI_Minimal)
let WiXUIRefDefaults = 
    {
        Id = "WixUI_Minimal"
    }

/// Parameters for WiX Upgrade
type WiXUpgrade =
    {
        /// This value specifies the upgrade code for the products that are to be detected by the FindRelatedProducts action.
        Id: Guid
        /// You can nest WiXUpgradeVersion sequences in here
        UpgradeVersion: string
    }
    override w.ToString() = "<Upgrade Id=\"" + w.Id.ToString("D") + "\">" + w.UpgradeVersion + "</Upgrade>"

/// Default value for WiX Upgrade
let WiXUpgradeDefaults = 
    {
        Id = Guid.Empty
        UpgradeVersion = ""
    }

/// Parameters for WiX Upgrade Version
type WiXUpgradeVersion =
    {
        /// Set to "yes" to detect products and applications but do not uninstall.
        OnlyDetect : YesOrNo
        /// Specifies the lower bound on the range of product versions to be detected by FindRelatedProducts.
        Minimum : string
        /// Specifies the upper boundary of the range of product versions detected by FindRelatedProducts.
        Maximum : string
        /// When the FindRelatedProducts action detects a related product installed on the system, it appends the product code to the property specified in this field. 
        /// Windows Installer documentation for the Upgrade table states that the property specified in this field must be a public property and must be added to the 
        /// SecureCustomProperties property. WiX automatically appends the property specified in this field to the SecureCustomProperties property when creating an MSI. 
        /// Each UpgradeVersion must have a unique Property value. After the FindRelatedProducts action is run, the value of this property is a list of product codes,
        /// separated by semicolons (;), detected on the system.
        Property : string
        /// Set to "no" to make the range of versions detected exclude the value specified in Minimum. This attribute is "yes" by default.
        IncludeMinimum : YesOrNo
        /// Set to "yes" to make the range of versions detected include the value specified in Maximum.
        IncludeMaximum : YesOrNo
    }
    override w.ToString() = "<UpgradeVersion Minimum=\"" + w.Minimum + "\" OnlyDetect=\"" + w.OnlyDetect.ToString() + "\" IncludeMinimum=\"" + w.IncludeMinimum.ToString() + "\" Maximum=\"" + w.Maximum 
                            + "\" IncludeMaximum=\"" + w.IncludeMaximum.ToString() + "\" Property=\"" + w.Property + "\" />"

/// Default value for WiX Upgrade
let WiXUpgradeVersionDefaults = 
    {
        OnlyDetect = YesOrNo.No
        Minimum = ""
        Maximum = ""
        Property = ""
        IncludeMinimum = YesOrNo.Yes
        IncludeMaximum = YesOrNo.No
    }

/// Used for determing when to run RemoveExistingProducts on major upgrade
type MajorUpgradeSchedule =
    /// (Default) Schedules RemoveExistingProducts after the InstallValidate standard action. This scheduling removes the installed product entirely before installing the upgrade product. 
    /// It's slowest but gives the most flexibility in changing components and features in the upgrade product. Note that if the installation of the upgrade product fails, 
    /// the machine will have neither version installed. 
    | AfterInstallValidate
    /// Schedules RemoveExistingProducts after the InstallInitialize standard action. This is similar to the afterInstallValidate scheduling, but if the installation of the upgrade product fails, 
    /// Windows Installer also rolls back the removal of the installed product -- in other words, reinstalls it. 
    | AfterInstallInitialize
    /// Schedules RemoveExistingProducts between the InstallExecute and InstallFinalize standard actions. This scheduling installs the upgrade product "on top of" the installed product then lets 
    /// RemoveExistingProducts uninstall any components that don't also exist in the upgrade product. Note that this scheduling requires strict adherence to the component rules because it relies 
    /// on component reference counts to be accurate during installation of the upgrade product and removal of the installed product. For more information, see Bob Arnson's blog post 
    /// "Paying for Upgrades" for details. If installation of the upgrade product fails, Windows Installer also rolls back the removal of the installed product -- in other words, reinstalls it. 
    | AfterInstallExecute
    /// Schedules RemoveExistingProducts between the InstallExecuteAgain and InstallFinalize standard actions. 
    /// This is identical to the afterInstallExecute scheduling but after the InstallExecuteAgain standard action instead of InstallExecute. 
    | AfterInstallExecuteAgain
    /// Schedules RemoveExistingProducts after the InstallFinalize standard action. This is similar to the afterInstallExecute and afterInstallExecuteAgain schedulings but takes place outside 
    /// the installation transaction so if installation of the upgrade product fails, Windows Installer does not roll back the removal of the installed product, 
    /// so the machine will have both versions installed. 
    | AfterInstallFinalize
    override m.ToString() =
        match m with
        | AfterInstallValidate -> "afterInstallValidate"
        | AfterInstallInitialize -> "afterInstallInitialize"
        | AfterInstallExecute -> "afterInstallExecute"
        | AfterInstallExecuteAgain -> "afterInstallExecuteAgain"
        | AfterInstallFinalize -> "afterInstallFinalize"

/// Parameters for WiX Major Upgrade
type WiXMajorUpgrade = 
    {
        /// Determines the scheduling of the RemoveExistingProducts standard action, which is when the installed product is removed. The default is "afterInstallValidate" which removes the 
        /// installed product entirely before installing the upgrade product. It's slowest but gives the most flexibility in changing components and features in the upgrade product.
        Schedule : MajorUpgradeSchedule
        /// When set to no (the default), products with lower version numbers are blocked from installing when a product with a higher version is installed; the DowngradeErrorMessage 
        /// attribute must also be specified. When set to yes, any version can be installed over any other version. 	 
        AllowDowngrades : YesOrNo
        /// The message displayed if users try to install a product with a lower version number when a product with a higher version is installed. Used only when AllowDowngrades is no (the default). 
        DowngradeErrorMessage : string
    }
    override w.ToString() = "<MajorUpgrade Schedule=\"" + w.Schedule.ToString() + "\" AllowDowngrades=\"" + w.AllowDowngrades.ToString() + "\" DowngradeErrorMessage=\"" + w.DowngradeErrorMessage + "\" />"

/// Default value for WiX Major Upgrade
let WiXMajorUpgradeDefaults =
    {
        Schedule = MajorUpgradeSchedule.AfterInstallValidate
        AllowDowngrades = YesOrNo.No
        DowngradeErrorMessage = "You can't downgrade this product!"
    }

    /// Parameters for WiX Variable, use ToString for creating the string xml nodes
type WiXVariable = 
    {
        /// The name of the variable.
        Id : string
        /// Set this value to 'yes' in order to make the variable's value overridable either by another WixVariable entry or via the command-line option -d<name>=<value> for light.exe.
        /// If the same variable is declared overridable in multiple places it will cause an error (since WiX won't know which value is correct). The default value is 'no'. 
        Overridable : YesOrNo
        /// The value of the variable. The value cannot be an empty string because that would make it possible to accidentally set a column to null. 
        Value : string
    }
    override w.ToString() = "<WixVariable Id=\"" + w.Id + "\" Value=\"" + w.Value + "\" Overridable=\"" + w.Overridable.ToString() + "\"/>"

/// Default value for WiX Variable
let WiXVariableDefaults = 
    {
        Id = ""
        Overridable = YesOrNo.No
        Value = ""
    }

/// Parameters for WiX Script properties, use ToString for creating the string xml nodes
type Script =
    {
        /// The product code GUID for the product.
        ProductCode : Guid

        /// The descriptive name of the product.
        ProductName : string

        /// The program files folder
        ProgramFilesFolder : ProgramFilesFolder

        /// Product description
        Description : string

        /// The decimal language ID (LCID) for the product.
        ProductLanguage : int

        /// The product's version string.
        ProductVersion : string

        /// The manufacturer of the product.
        ProductPublisher : string

        /// The upgrade code GUID for the product.
        UpgradeGuid : Guid

        /// You can nest upgrade elements in here
        Upgrade : WiXUpgrade seq

        /// Nest major upgrade elements in here
        MajorUpgrade : WiXMajorUpgrade seq

        /// Nest UIRefs in here
        UIRefs : WiXUIRef seq

        /// Nest WiXVariables in here
        WiXVariables : WiXVariable seq

        /// Nest directories in here
        Directories : WiXDir seq
        
        /// You can nest DirectoryRefs in here
        DirectoryRefs : WiXDirectoryRef seq

        /// Nest Components in here
        Components : WiXDirectoryComponent seq

        /// Build Number of product
        BuildNumber : string

        /// You can nest feature elements in here
        Features : Feature seq

        /// You can nest custom actions in here
        CustomActions : WiXCustomAction seq

        /// You can nest InstallExecuteSequence actions in here
        ActionSequences : WiXCustomActionExecution seq

        /// You can add custom replacements for the wix xml here.
        CustomReplacements: (string * string) seq

        /// Specify architecture of package. For 64Bit Setups set ProgramFilesFolder to ProgramFiles64, package platform to X64, all components to Win64 = yes and all files' processorArchitecture to X64.
        Platform : Architecture
    }

/// Default values for WiX Script properties
let ScriptDefaults = 
    {
        ProductCode = Guid.Empty
        ProductName = ""
        ProgramFilesFolder = ProgramFilesFolder.ProgramFiles64
        Description = ""
        ProductLanguage = 1033
        ProductVersion = ""
        ProductPublisher = ""
        UpgradeGuid = Guid.Empty
        Upgrade = []
        MajorUpgrade = []
        UIRefs = []
        WiXVariables = []
        Directories = []
        DirectoryRefs = []
        Components = []
        BuildNumber = "1.0.0"
        Features = []
        CustomActions = []
        ActionSequences = []
        CustomReplacements = []
        Platform = Architecture.X64
    }

/// Generates WiX Template with specified file name (you can prepend location too)
/// You need to run this once every build an then use FillInWiXScript to replace placeholders
/// ## Parameters
///  - `fileName` - Pass desired fileName for your wiXScript file
///
/// ## Sample
///     generateWiXScript "Setup.wxs"
let generateWiXScript fileName =
    let scriptTemplate = 
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>
        <Wix xmlns=\"http://schemas.microsoft.com/wix/2006/wi\">
          <!-- Values will be set by build script, use processTemplates function. UpgradeGuid may never change -->
            
          <!-- Version uses Major.Minor.Build format -->
          <Product
            Id=\"@Product.ProductCode@\"
            Name=\"@Product.ProductName@\"
            Language=\"@Product.Language@\"
            Version=\"@Product.Version@\"
            Manufacturer=\"@Product.Publisher@\"
            UpgradeCode=\"@Product.UpgradeGuid@\"
            >
            
            <!-- Auto Increment Package Id for every release -->
            <Package
              Id=\"*\"
              InstallerVersion=\"200\"
              Compressed=\"yes\"
              Platform=\"@Product.Platform@\"
              Description=\"@Product.Description@\"
              Manufacturer=\"@Product.Publisher@\"
            />

            <!-- Include user interface -->
            @Product.UIRefs@

            <!-- Add various WiXVariables -->
            @Product.Variables@

            <!-- WiX uses media for splitting up files if using CDs for publishing. We make just one. All files will be embedded in it. -->
            <Media Id=\"1\" Cabinet=\"media1.cab\" EmbedCab=\"yes\" />

            <Directory Id=\"TARGETDIR\" Name=\"SourceDir\">
              <Directory Id=\"@Product.ProgramFilesFolder@\" Name=\"ProgramFiles\">
                <Directory Id=\"PUBLISHERDIR\" Name=\"@Product.Publisher@\">
                  <Directory Id=\"INSTALLDIR\" Name=\"@Product.ProductName@\">
                    @Product.Directories@
                    @Product.Components@
                  </Directory>
                </Directory>
              </Directory>
            </Directory>
            
            @Product.DirectoryRefs@

            @Product.Features@
        
            @Product.MajorUpgrade@

            @Product.Upgrade@

            @Product.CustomActions@

            <InstallExecuteSequence>
              @Product.ActionSequences@
            </InstallExecuteSequence>
          </Product>
        </Wix>"
    WriteStringToFile false fileName scriptTemplate
    
/// Takes path where script files reside and sets all parameters as defined
/// ## Parameters
///  - `wiXPath` - Pass path where your script is located at. Function will search for all Scripts in that location and fill in parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     FillInWixScript "" (fun f ->
///                            {f with
///                                ProductCode = WiXProductCode
///                                ProductName = WiXProductName
///                                Description = projectDescription
///                                ProductLanguage = WiXProductLanguage
///                                ProductVersion = WiXProductVersion
///                                ProductPublisher = WixProductPublisher
///                                UpgradeGuid = WixProductUpgradeGuid
///                                UIRefs = uiRef1.ToString() + uiRef2.ToString()
///                                WiXVariables = wiXLicense.ToString()
///                                Directories = directories
///                                BuildNumber = "1.0.0"
///                                Features = rootFeature.ToString()
///                                CustomActions = action1.ToString() + action2.ToString()
///                                ActionSequences = actionExecution1.ToString() + actionExecution2.ToString()
///                            })
[<Obsolete("Please use new FillInWiXTemplate function")>]
let FillInWixScript wiXPath (setParams : WiXScript -> WiXScript) =
    let parameters = WiXScriptDefaults |> setParams
    let wixScript = !!( wiXPath @@ "*.wxs" )
    let replacements = [
        "@Product.ProductCode@", parameters.ProductCode.ToString("D")
        "@Product.ProductName@", parameters.ProductName
        "@Product.ProgramFilesFolder@", parameters.ProgramFilesFolder.ToString()
        "@Product.Description@", parameters.Description
        "@Product.UIRefs@", parameters.UIRefs
        "@Product.Language@", parameters.ProductLanguage
        "@Product.Version@", parameters.ProductVersion
        "@Product.Variables@", parameters.WiXVariables
        "@Product.Publisher@", parameters.ProductPublisher
        "@Product.UpgradeGuid@", parameters.UpgradeGuid.ToString("D")
        "@Product.Upgrade@", parameters.Upgrade
        "@Product.MajorUpgrade@", parameters.MajorUpgrade
        "@Product.Directories@", parameters.Directories
        "@Product.Components@", ""
        "@Product.Platform@", parameters.Platform.ToString()
        "@Product.Features@", parameters.Features
        "@Product.CustomActions@", parameters.CustomActions
        "@Product.ActionSequences@", parameters.ActionSequences
        "@Build.number@", parameters.BuildNumber]    
    processTemplates replacements wixScript
    
/// Takes path where script files reside and sets all parameters as defined
/// ## Parameters
///  - `wiXPath` - Pass path where your script is located at. Function will search for all Scripts in that location and fill in parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     FillInWixScript "" (fun f ->
///                            {f with
///                                ProductCode = WiXProductCode
///                                ProductName = WiXProductName
///                                Description = projectDescription
///                                ProductLanguage = WiXProductLanguage
///                                ProductVersion = WiXProductVersion
///                                ProductPublisher = WixProductPublisher
///                                UpgradeGuid = WixProductUpgradeGuid
///                                UIRefs = uiRef1.ToString() + uiRef2.ToString()
///                                WiXVariables = wiXLicense.ToString()
///                                Directories = directories
///                                DirectoryRefs = directoryrefs
///                                BuildNumber = "1.0.0"
///                                Features = rootFeature.ToString()
///                                CustomActions = action1.ToString() + action2.ToString()
///                                ActionSequences = actionExecution1.ToString() + actionExecution2.ToString()
///                            })
let FillInWiXTemplate wiXPath setParams =
    let parameters = ScriptDefaults |> setParams
    let wixScript = !!( wiXPath @@ "*.wxs" )
    let replacements = [
        "@Product.ProductCode@", parameters.ProductCode.ToString("D")
        "@Product.ProductName@", parameters.ProductName
        "@Product.ProgramFilesFolder@", parameters.ProgramFilesFolder.ToString()
        "@Product.Description@", parameters.Description
        "@Product.UIRefs@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.UIRefs
        "@Product.Language@", parameters.ProductLanguage.ToString()
        "@Product.Version@", parameters.ProductVersion
        "@Product.Variables@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.WiXVariables
        "@Product.Publisher@", parameters.ProductPublisher
        "@Product.UpgradeGuid@", parameters.UpgradeGuid.ToString("D")
        "@Product.Upgrade@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.Upgrade
        "@Product.MajorUpgrade@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.MajorUpgrade
        "@Product.Directories@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.Directories
        "@Product.DirectoryRefs@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.DirectoryRefs
        "@Product.Components@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.Components
        "@Product.Features@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.Features
        "@Product.CustomActions@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.CustomActions
        "@Product.ActionSequences@", Seq.fold(fun acc elem -> acc + elem.ToString()) "" parameters.ActionSequences
        "@Product.Platform@", parameters.Platform.ToString()
        "@Build.number@", parameters.BuildNumber]
    let customReplacements = parameters.CustomReplacements |> Seq.map (fun (key, value) -> ((sprintf "@Custom.%s@" key), value)) |> List.ofSeq
    let replacements = replacements @ customReplacements
    processTemplates replacements wixScript

/// Generates a feature based on the given parameters, use toString on it when embedding it
/// You can pass other features into InnerContent for making a hierarchy
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     let feature = generateFeature (fun f -> 
///                                        {f with  
///                                            Id = "UniqueName"
///                                            Title = "Title which is shown"
///                                            Level = 1 
///                                            Description = "Somewhat longer description" 
///                                            Display = "expand" 
///                                            InnerContent = otherFeature.ToString()
///                                        })
[<Obsolete("Please use the new ")>]
let generateFeature (setParams : WiXFeature -> WiXFeature) =
    let parameters = WiXFeatureDefaults |> setParams
    if parameters.Id = "" then 
        failwith "No parameter passed for feature Id!"
    parameters

/// Generates a feature based on the given parameters, use toString on it when embedding it
/// You can pass other features into InnerContent for making a hierarchy
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     let feature = generateFeature (fun f -> 
///                                        {f with  
///                                            Id = "UniqueName"
///                                            Title = "Title which is shown"
///                                            Level = 1 
///                                            Description = "Somewhat longer description" 
///                                            Display = "expand" 
///                                            InnerContent = [otherFeature1; otherFeature2]
///                                        })
let generateFeatureElement setParams =
    let parameters : Feature = FeatureDefaults |> setParams
    if parameters.Id = "" then 
        failwith "No parameter passed for feature Id!"
    parameters

/// Generates a customAction based on the given parameters, use toString on it when embedding it
/// Be careful to make Id unique. FileKey is a reference to a file Id which you added by using wixDir or wixFile
/// Set impersonate to no if your action needs elevated privileges, you should then also set execute as "deferred"
/// ExeCommand are the parameters passed to your executable
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     let action = generateCustomAction (fun f ->
///                                            {f with
///                                                Id = "UniqueActionId"
///                                                FileKey = "fi_5"
///                                                Execute = "deferred"
///                                                Impersonate = "no"
///                                                ExeCommand = "install"
///                                                Return = "check"
///                                            })
let generateCustomAction setParams =
    let parameters : WiXCustomAction = WiXCustomActionDefaults |> setParams
    if parameters.Id = "" then 
        failwith "No parameter passed for feature Id!"
    parameters

/// Generates a custom action execution based on the given parameters, use toString on it when embedding it
/// Condition in sample makes execute only on install
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     let actionExecution = generateCustomActionExecution (fun f ->
///                                                                {f with 
///                                                                    ActionId = action.Id
///                                                                    Verb = "After"
///                                                                    Target = "InstallFiles"                                                                        
///                                                                    Condition = "<![CDATA[(&" + feature.Id + " = 3) AND NOT (!" + feature.Id + " = 3)]]>"
///                                                                })
let generateCustomActionExecution setParams =
    let parameters : WiXCustomActionExecution = WixCustomActionExecutionDefaults |> setParams
    if parameters.ActionId = "" then 
        failwith "No parameter passed for action Id!"
    parameters

/// Generates a ui ref based on the given parameters, use toString on it when embedding it
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     let UIRef = generateUIRef (fun f ->
///                                    {f with
///                                        Id = "WixUI_Mondo"
///                                    })
let generateUIRef setParams =
    let parameters : WiXUIRef = WiXUIRefDefaults |> setParams
    if parameters.Id = "" then 
        failwith "No parameter passed for action Id!"
    parameters


/// Generates an upgrade based on the given parameters, use toString on it when embedding it
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     let upgrade = generateUpgrade (fun f ->
///                                       {f with
///                                          Id = productUpgradeCode
///                                       })
let generateUpgrade setParams =
    let parameters : WiXUpgrade = WiXUpgradeDefaults |> setParams
    if parameters.Id = Guid.Empty then 
        failwith "No parameter passed for action Id!"
    parameters

/// Generates an upgrade version based on the given parameters, use toString on it when embedding it
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     let upgradeVersion = generateUpgradeVersion (fun f ->
///                                                     {f with
///                                                        Minimum = productVersion
///                                                        OnlyDetect = "yes"
///                                                     })
let generateUpgradeVersion setParams =
    let parameters : WiXUpgradeVersion = WiXUpgradeVersionDefaults |> setParams
    parameters


/// Generates a major upgrade based on the given parameters, use toString on it when embedding it
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///
/// ## Sample
///     let majorUpgradeVersion = generateMajorUpgradeVersion(fun f ->
///                                                     {f with 
///                                                         DowngradeErrorMessage = "A later version is already installed, exiting."
///                                                     })
let generateMajorUpgradeVersion setParams =
    let parameters : WiXMajorUpgrade = WiXMajorUpgradeDefaults |> setParams
    parameters

/// Runs the [Candle tool](http://wixtoolset.org/documentation/manual/v3/overview/candle.html) on the given WiX script with the given parameters
let Candle (parameters : WiXParams) wixScript = 
    traceStartTask "Candle" wixScript
    let fi = fileInfo wixScript
    let wixObj = fi.Directory.FullName @@ sprintf @"%s.wixobj" fi.Name
    let tool = parameters.ToolDirectory @@ "candle.exe"
    let args = 
        sprintf "-out \"%s\" \"%s\" %s" wixObj (wixScript |> FullName) (separated " " parameters.AdditionalCandleArgs)
    tracefn "%s %s" parameters.ToolDirectory args
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- tool
                info.WorkingDirectory <- null
                info.Arguments <- args) parameters.TimeOut
    then failwithf "Candle %s failed." args
    traceEndTask "Candle" wixScript
    wixObj

/// Runs the [Light tool](http://wixtoolset.org/documentation/manual/v3/overview/light.html) on the given WiX script with the given parameters
let Light (parameters : WiXParams) outputFile wixObj = 
    traceStartTask "Light" wixObj
    let tool = parameters.ToolDirectory @@ "light.exe"
    let args = 
        sprintf "\"%s\" -spdb -dcl:high -out \"%s\" %s" (wixObj |> FullName) (outputFile |> FullName) 
            (separated " " parameters.AdditionalLightArgs)
    tracefn "%s %s" parameters.ToolDirectory args
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- tool
                info.WorkingDirectory <- null
                info.Arguments <- args) parameters.TimeOut
    then failwithf "Light %s failed." args
    traceEndTask "Light" wixObj

/// Uses the WiX tools [Candle](http://wixtoolset.org/documentation/manual/v3/overview/candle.html) and [Light](http://wixtoolset.org/documentation/manual/v3/overview/light.html) to create an msi.
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///  - `outputFile` - The msi output file path (given to Light).
///  - `wixScript` - The path to a WiX script that will be used with Candle.
///
/// ## Sample
///     Target "BuildSetup" (fun _ ->
///         // Copy all important files to the deploy directory
///         !! (buildDir + "/**/*.dll")
///           ++ (buildDir + "/**/*.exe")
///           ++ (buildDir + "/**/*.config")
///           |> Copy deployPrepDir 
///    
///         // replace tags in a template file in order to generate a WiX script
///         let ALLFILES = fun _ -> true
///     
///         let replacements = [
///             "@build.number@",if not isLocalBuild then buildVersion else "0.1.0.0"
///             "@product.productcode@",System.Guid.NewGuid().ToString()
///             "@HelpFiles@",getFilesAsWiXString helpFiles
///             "@ScriptFiles@",getFilesAsWiXString scriptFiles
///             "@icons@",wixDir ALLFILES true (directoryInfo(bundledDir @@ "icons"))]
///         
///         processTemplates replacements setupFiles
///     
///         // run the WiX tools
///         WiX (fun p -> {p with ToolDirectory = WiXPath}) 
///             setupFileName
///             (setupBuildDir + "Setup.wxs.template")
///     )
let WiX setParams outputFile wixScript = 
    let parameters = setParams WiXDefaults
    wixScript
    |> Candle parameters
    |> Light parameters outputFile

type HeatParams = 
    { 
      /// Directory that contains the Heat tool
      ToolDirectory : string
      /// Timeout for the call to Heat
      TimeOut : TimeSpan       
      /// Auto generate component guids at compile time, e.g. set Guid="*". (Parameter: -ag)
      AutoGenerateGuid : bool
      /// Generate guids now. All components are given a guid when heat is run. (Parameter: -gg)
      GenerateGuidNow : bool
      /// Suppress COM elements. (Parameter: -scom)
      SupressComElements : bool
      /// Suppress generation of fragments for directories and components. (Parameter: -sfrag)
      SupressDirectoryFragments : bool
      /// Suppress harvesting the root directory as an element. (Parameter: -srd)
      SupressRootDirectory : bool
      /// Suppress registry harvesting. (Parameter: -sreg)
      SupressRegistry : bool
      /// Suppress unique identifiers for files, components, & directories.(Parameter: -suid)
      SupressUniqueIds : bool
      /// Directory reference to root directories, cannot contains spaces. (Parameter: -dr)
      DirectoryReference : string
      /// Component group name, cannot contain spaces. (Parameter: -cg)
      ComponentGroupName: string
      /// Substitute File/@Source="SourceDir" with a preprocessor or a wix variable  (Parameter: -var)
      VariableName : string
      AdditionalHeatArgs : string list
    }

/// Default values for the Heat harvesting
let HeatDefaulParams = 
    {
      ToolDirectory = currentDirectory @@ "tools" @@ "Wix"
      TimeOut =  TimeSpan.FromMinutes 5.0
      AutoGenerateGuid = true
      GenerateGuidNow = false
      SupressComElements = true
      SupressDirectoryFragments = true
      SupressRootDirectory = true
      SupressRegistry = true
      SupressUniqueIds = true
      DirectoryReference = "INSTALLDIR"
      ComponentGroupName = "binaries"
      VariableName = "var.SourceDir"
      AdditionalHeatArgs = []
    }

/// Harvests the contents of a Directory for use with Wix using the [Heat](http://wixtoolset.org/documentation/manual/v3/overview/heat.html) tool.
/// ## Parameters
///  - `setParams` - Function used to manipulate the Heat default parameters.
///  - `directory` - The path to the directory that will be harvested by Heat.
///  - `outputFile` - The output file path given to Heat.
///
let HarvestDirectory (setParams : HeatParams -> HeatParams) directory outputFile = 
    traceStartTask "Heat" directory
    let conditionalArgument condition arg args =
        match condition with
            | true ->  arg :: args
            | false -> args
    let parameters = setParams HeatDefaulParams
    let tool = parameters.ToolDirectory @@ "heat.exe"
    let arglist = 
        parameters.AdditionalHeatArgs
        |> conditionalArgument parameters.AutoGenerateGuid "-ag" 
        |> conditionalArgument parameters.GenerateGuidNow "-gg" 
        |> conditionalArgument parameters.SupressComElements "-scom" 
        |> conditionalArgument parameters.SupressDirectoryFragments "-sfrag" 
        |> conditionalArgument parameters.SupressRootDirectory "-srd" 
        |> conditionalArgument parameters.SupressRegistry "-sreg" 
        |> conditionalArgument parameters.SupressUniqueIds "-suid" 
    let args = 
        sprintf "dir \"%s\" -o \"%s\" -dr %s -cg %s -var %s %s" 
            (directory |> FullName)
            (outputFile |> FullName) 
            parameters.DirectoryReference 
            parameters.ComponentGroupName 
            parameters.VariableName 
            (separated " " arglist)    
    tracefn "%s %s" parameters.ToolDirectory args
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- tool
                info.WorkingDirectory <- null
                info.Arguments <- args) parameters.TimeOut
    then failwithf "Heat %s failed." args
    traceEndTask "Heat" directory


