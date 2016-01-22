/// Contains various functions for interacting with Dynamics CRM. So far there is support for exporting and importing solutions, zipping and unzipping using the Solution Packager, as well as publishing customizations.
module Fake.DynamicsCRMHelper

open System
open System.Configuration
open System.IO

/// Specify which action Solution Packager should be invoked with
type SolutionPackagerAction = 
    | Extract
    | Pack 
     override s.ToString() =
        match s with
        | Extract -> "/a:Extract"
        | Pack -> "/a:Pack"

/// Specify Package Type for usage with Solution Packager
type PackageType = 
    | Unmanaged
    | Managed
    | Both
    override p.ToString() =
        match p with
        | Unmanaged -> "/p:Unmanaged"
        | Managed -> "/p:Managed"
        | Both -> "/p:Both"

/// Parameters for executing Dynamics CRM Helper functions
type DynamicsCrmHelperParams =
    {
        /// Url of CRM Organization / Discovery Service URL if using AllOrganizations
        Url : string
        /// Username of user that should be used to connect to the CRM Organization. Leave blank to use default credentials
        User : string
        /// Password of user that should be used to connect to the CRM Organization. Leave blank to use default credentials
        Password : string
        /// TimeOut for each function. Set to a higher value, i.e. 60 minutes if using AllOrganizations
        TimeOut : TimeSpan
        /// Tool Directory where Solution Exchanger is stored
        ToolDirectory : string
        /// Working Directory for actions, can be used to influence storage locations of files
        WorkingDirectory : string
        /// Set for specifying output file name when exporting solution or input solution name when importing
        FileName : string
        /// Set for specifying unique name of solution when exporting single solution
        Solution : string
        /// Specify, whether solution should be exported as managed or unmanaged
        Managed : bool
        /// Export all solutions for given organization
        AllSolutions : bool
        /// Export all solutions for all organizations that the current user has access to. Be sure to pass discovery service url for URL parameter
        AllOrganizations : bool
    }

/// Default values for Dynamics CRM Helper
/// You can obtain the solution exchanger as NuGet Package "Dynamics.CRM.SolutionExchanger"
let DynamicsCrmHelperDefaults = 
    {
        Url = ""
        User = ""
        Password = ""
        TimeOut = TimeSpan.FromMinutes 10.0
        ToolDirectory = currentDirectory @@ "tools" @@ "Dynamics.CRM.SolutionExchanger"
        WorkingDirectory = ""
        FileName = ""
        Solution = ""
        Managed = false
        AllSolutions = false
        AllOrganizations = false
    }

/// Parameters for invoking Solution Packager
type SolutionPackagerParams =
    {
        /// Action to start, either pack or extract
        Action : SolutionPackagerAction
        /// Path to solution that should be packed or extracted
        ZipFile : string
        /// PackageType for packing solution, either managed, unmanaged or both
        PackageType : PackageType
        /// Directory where SolutionPackager can be found
        ToolDirectory : string
        /// Working Directory for calls
        WorkingDirectory : string
        /// Timeout for calls
        TimeOut : TimeSpan
        /// Folder for output of calls
        Folder : string
    }

/// Default values for invoking Solution Packager
let SolutionPackagerDefaults =
    {
        ToolDirectory = currentDirectory @@ "tools" @@ "SolutionPackager"
        TimeOut = TimeSpan.FromMinutes 1.0
        Action = Extract
        ZipFile = ""
        PackageType = Both
        WorkingDirectory = "."
        Folder = "."
    }

/// Publishes all solution component changes.
/// ## Parameters
///
///  - `setParams` - Parameters for invoking solution exchanger
let PublishAll (setParams : DynamicsCrmHelperParams -> DynamicsCrmHelperParams) =
    traceStartTask "Publish All" ""
    let parameters = setParams DynamicsCrmHelperDefaults
    let tool = parameters.ToolDirectory @@ "Dynamics.CRM.SolutionExchanger.exe"
    let args = sprintf "Publish /url:%s /user:%s /password:%s /timeout:%i" parameters.Url parameters.User parameters.Password (int parameters.TimeOut.TotalMinutes)
    if 0 <> ExecProcess (fun pInfo -> 
                pInfo.FileName <- tool
                pInfo.WorkingDirectory <- parameters.WorkingDirectory
                pInfo.Arguments <- args) parameters.TimeOut
    then failwithf "Publishing All %s failed." args
    traceEndTask "Successfully finished Publish All" args
           
/// Exports solution from Dynamics CRM and save it to file
/// ## Parameters
///
///  - `setParams` - Parameters for invoking solution exchanger
/// ## Sample
///     // This Target will get all solutions of all organizations that the executing user has access to, export them as unmanaged and extract them using Solution Packager.
///     // Extracted solutions are stored in a folder named according to the name of the organization they were exported from.
///     Target "SaveAndUnzipAllSolutions" (fun _ ->
///             CreateDir solutions
///      
///             ExportSolution(fun f -> 
///                             {f with 
///                                 Url = "http://YourOrganizationDiscoveryService"
///                                 Managed = false
///                                 AllOrganizations = true
///                                 WorkingDirectory = solutions
///                                 TimeOut = TimeSpan.FromMinutes 60.0
///                             })
///         
///             !!(solutions @@ @"\**\*.zip")
///               |> Seq.iter(fun solution -> 
///                             let dir = DirectoryInfo(solution)                    
///       
///                             SolutionPackager (fun f ->
///                                 {f with 
///                                     Action = Extract
///                                     ZipFile = solution
///                                     PackageType = Unmanaged
///                                     Folder = extractedDir @@ dir.Parent.Name @@ (Path.GetFileName solution).Replace(".zip", "")
///                                     ToolDirectory = @".\tools\SolutionPackager\"
///                                 }))
///     )
let ExportSolution (setParams : DynamicsCrmHelperParams -> DynamicsCrmHelperParams) =
    let parameters = setParams DynamicsCrmHelperDefaults
    traceStartTask "Exporting Solution" (parameters.Solution + ": " + if parameters.Managed then "Managed" else "Unmanaged")
    let tool = parameters.ToolDirectory @@ "Dynamics.CRM.SolutionExchanger.exe"
    let args = sprintf "Export /url:%s /user:%s /password:%s /solution:%s /managed:%s /workingdir:%s /filename:%s /allSolutions:%s /allOrganizations:%s /timeout:%i" 
                    parameters.Url parameters.User parameters.Password parameters.Solution (parameters.Managed.ToString()) 
                    parameters.WorkingDirectory parameters.FileName (parameters.AllSolutions.ToString()) (parameters.AllOrganizations.ToString()) (int parameters.TimeOut.TotalMinutes)
    if 0 <> ExecProcess (fun pInfo -> 
                pInfo.FileName <- tool
                pInfo.WorkingDirectory <- parameters.WorkingDirectory
                pInfo.Arguments <- args) parameters.TimeOut
    then failwithf "Exporting Solution failed."
    traceEndTask "Successfully finished Exporting Solution" args

/// Imports zipped solution file to Dynamics CRM
/// ## Parameters
///
///  - `setParams` - Parameters for invoking solution exchanger
let ImportSolution (setParams : DynamicsCrmHelperParams -> DynamicsCrmHelperParams) =
    let parameters = setParams DynamicsCrmHelperDefaults
    traceStartTask "Importing Solution" parameters.FileName
    let tool = parameters.ToolDirectory @@ "Dynamics.CRM.SolutionExchanger.exe"
    let args = sprintf "Import /url:%s /user:%s /password:%s /filename:%s /timeout:%i" 
                    parameters.Url parameters.User parameters.Password parameters.FileName (int parameters.TimeOut.TotalMinutes)
    if 0 <> ExecProcess (fun pInfo -> 
                pInfo.FileName <- tool
                pInfo.WorkingDirectory <- parameters.WorkingDirectory
                pInfo.Arguments <- args) parameters.TimeOut
    then failwithf "Importing Solution failed."
    traceEndTask "Successfully finished Importing Solution" args
    
/// Runs the solution packager tool on the given file for extracting the zip file or packing the extracted XML representation of a solution to a zip file again
/// ## Parameters
///
///  - `setParams` - Parameters for invoking solution packager
let SolutionPackager setParams = 
    let parameters = setParams SolutionPackagerDefaults
    traceStartTask "Running Solution Packager" (parameters.Action.ToString() + ": " + parameters.ZipFile)
    if not (File.Exists(parameters.ZipFile)) then
        failwith (sprintf "File at path %A does not exist!" parameters.ZipFile)
    let tool = parameters.ToolDirectory @@ "SolutionPackager.exe"
    let args = sprintf "%s %s /z:\"%s\" /f:\"%s\"" (parameters.Action.ToString()) (parameters.PackageType.ToString()) parameters.ZipFile parameters.Folder
    if 0 <> ExecProcess (fun pInfo -> 
                pInfo.FileName <- tool
                pInfo.WorkingDirectory <- parameters.WorkingDirectory
                pInfo.Arguments <- args) parameters.TimeOut
    then 
        failwith (sprintf "SolutionPackager %s failed." args)
    else
        traceEndTask "Successfully ran Solution Packager" args
