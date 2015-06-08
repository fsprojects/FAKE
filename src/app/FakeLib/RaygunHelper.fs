/// Enables deployment tracking using Raygun.io
///
/// Thin wrapper around [the Raygun HTTP deployment API](https://raygun.io/docs/deployments/api)
module Fake.RaygunHelper

open Fake
open Fake.Git
open Newtonsoft.Json
open System.Net

/// Data describing a deployment to Raygun
type RaygunDeploymentData =
  {
    /// Application API key
    /// Required, no sensible default
    apiKey: string

    /// Version string describing deployed version
    /// Should be the same as reported by the application
    /// to raygun when posting an error
    /// Required, no sensible default
    version : string

    /// Name of person responsible for deployment
    /// Optional, defaults to empty string
    ownerName : string

    /// Email address of person responsible for deployment
    /// Optional, defaults to empty string
    emailAddress : string

    /// Release notes
    /// Optional, defaults to empty string
    comment: string

    /// Hash code (or other commit identifier) from
    /// source control system
    /// Optional, Defaults to current git hash if executed from a git repository
    ///           else defaults to empty string
    scmIdentifier : string

    /// Datetime of the deployment
    /// Optional, Defaults to System.DateTime.UtcNow
    createdAt: System.DateTime
    }

/// Connection configuration
type RaygunConnectionSettings =
    {
      /// Endpoint to connect to
      /// Required, Defaults to: https://app.raygun.io/deployments
      endPoint : string

      /// Raygun user access token for allowing API
      /// access. (Creatd under User -> My settings in the web application)
      /// Required, no sensible default
      externalToken: string
     }

let private gitHash =
    try
        getCurrentHash()
    with
    | _ -> ""

let private endPoint = @"https://app.raygun.io/deployments"

let private defaultData =
    {
      apiKey = ""
      version = ""
      ownerName = ""
      emailAddress = ""
      comment = ""
      scmIdentifier = gitHash
      createdAt = System.DateTime.UtcNow
    }

let private defaultSettings =
    {
      endPoint = @"https://app.raygun.io/deployments"
      externalToken = ""
    }

let private createQueryStringCollection token =
    let collection = (new System.Collections.Specialized.NameValueCollection())
    collection.Add("authToken", token)
    collection

let private serialize data = JsonConvert.SerializeObject(data)

/// ### Report a deployment to raygun
///
/// Reports a deployment to raygun so reported errors can be
/// correlated with deployments
///
/// ### Paramteres
///
/// * settings : Function that sets the raygun connection settings.
/// * data : Function that sets the deployment data
let ReportDeployment (settings:RaygunConnectionSettings->RaygunConnectionSettings) (data:RaygunDeploymentData->RaygunDeploymentData) =
    traceStartTask "Raygun.io" "Report new deployment"
    let settings = defaultSettings |> settings
    let data = defaultData |> data
    use client = (new WebClient())
    client.Headers.Add(HttpRequestHeader.ContentType, "application/json")
    client.QueryString <- createQueryStringCollection settings.externalToken
    client.UploadString(settings.endPoint,"POST", (serialize data)) |> ignore
    traceEndTask "Raygun.io" "Report new deployment"
