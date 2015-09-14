[<AutoOpen>]
/// Contains types and functions for working with [NUnit](http://www.nunit.org/) unit tests result xml.
module Fake.NUnitXml

open System
open System.IO
open System.Text
open System.Xml
open System.Xml.Linq

let inline private imp arg = ((^a : (static member op_Implicit : ^b -> ^a) arg))
let inline private (?) (elem : XElement) attr = elem.Attribute(imp attr).Value

let inline private attr attr value (elem : XElement) = 
    elem.SetAttributeValue(imp attr, value)
    elem

let inline private elem name = XElement(imp name : XName)

/// [omit]
let GetTestAssemblies(xDoc : XDocument) = 
    xDoc.Descendants()
    |> Seq.filter 
        (fun el -> 
                el.Name = (imp "test-suite") && 
                (el.Attribute(imp "type").Value = "Assembly" || el.Attribute(imp "type").Value = "SetUpFixture"))
    |> Seq.toList

/// Returns whether all tests in the given test result have succeeded
let AllSucceeded xDocs = 
    xDocs
    |> Seq.map GetTestAssemblies
    |> Seq.concat
    |> Seq.map (fun assembly -> assembly.Attribute(imp "result").Value)
    |> Seq.map ((<>) "Failure")
    |> Seq.reduce (&&)

/// Used by the NUnitParallel helper, can also be used to merge test results
/// from multiple calls to the normal NUnit helper.
module internal NUnitMerge = 
    type ResultSummary = 
        { Total : int
          Errors : int
          Failures : int
          NotRun : int
          Inconclusive : int
          Ignored : int
          Skipped : int
          Invalid : int
          DateTime : DateTime }
        
        static member ofXDoc (xDoc : XDocument) = 
            let tr = xDoc.Element(imp "test-results")
            { Total = int tr?total
              Errors = int tr?errors
              Failures = int tr?failures
              NotRun = int tr?``not-run``
              Inconclusive = int tr?inconclusive
              Ignored = int tr?ignored
              Skipped = int tr?skipped
              Invalid = int tr?invalid
              DateTime = DateTime.Parse(sprintf "%s %s" tr?date tr?time) }
        
        static member toXElement res = 
            elem "test-results"
            |> attr "name" "Merged results"
            |> attr "total" res.Total
            |> attr "errors" res.Errors
            |> attr "failures" res.Failures
            |> attr "not-run" res.NotRun
            |> attr "inconclusive" res.Inconclusive
            |> attr "skipped" res.Skipped
            |> attr "ignored" res.Ignored
            |> attr "invalid" res.Invalid
            |> attr "date" (res.DateTime.ToString("yyyy-MM-dd"))
            |> attr "time" (res.DateTime.ToString("HH:mm:ss"))
        
        static member append r1 r2 = 
            { r1 with Total = r1.Total + r2.Total
                      Errors = r1.Errors + r2.Errors
                      Failures = r1.Failures + r2.Failures
                      NotRun = r1.NotRun + r2.NotRun
                      Inconclusive = r1.Inconclusive + r2.Inconclusive
                      Ignored = r1.Ignored + r2.Ignored
                      Skipped = r1.Skipped + r2.Skipped
                      Invalid = r1.Invalid + r2.Invalid
                      DateTime = Seq.min [ r1.DateTime; r2.DateTime ] }
    
    type Environment = 
        { NUnitVersion : string
          ClrVersion : string
          OSVersion : string
          Platform : string
          Cwd : string
          MachineName : string
          User : string
          UserDomain : string }
        
        static member ofXDoc (xDoc : XDocument) = 
            let env = xDoc.Element(imp "test-results").Element(imp "environment")
            { NUnitVersion = env?``nunit-version``
              ClrVersion = env?``clr-version``
              OSVersion = env?``os-version``
              Platform = env?platform
              Cwd = env?cwd
              MachineName = env?``machine-name``
              User = env?user
              UserDomain = env?``user-domain`` }
        
        static member toXElement env = 
            elem "environment"
            |> attr "nunit-version" env.NUnitVersion
            |> attr "clr-version" env.ClrVersion
            |> attr "os-version" env.OSVersion
            |> attr "platform" env.Platform
            |> attr "cwd" env.Cwd
            |> attr "machine-name" env.MachineName
            |> attr "user" env.User
            |> attr "user-domain" env.UserDomain
    
    type Culture = 
        { CurrentCulture : string
          CurrentUICulture : string }
        
        static member ofXDoc (xDoc : XDocument) = 
            let culture = xDoc.Element(imp "test-results").Element(imp "culture-info")
            { CurrentCulture = culture?``current-culture``
              CurrentUICulture = culture?``current-uiculture`` }
        
        static member toXElement culture = 
            elem "culture-info"
            |> attr "current-culture" culture.CurrentCulture
            |> attr "current-uiculture" culture.CurrentUICulture
    
    type Doc = 
        { Doc : XDocument
          Summary : ResultSummary
          Env : Environment
          Culture : Culture
          Assemblies : XElement list }
        
        static member ofXDoc doc = 
            { Doc = doc
              Summary = ResultSummary.ofXDoc doc
              Env = Environment.ofXDoc doc
              Culture = Culture.ofXDoc doc
              Assemblies = GetTestAssemblies doc }
        
        static member append doc1 doc2 = 
            // Sanity check!
            if doc1.Env <> doc2.Env || doc1.Culture <> doc2.Culture then 
                traceImportant 
                    "Unmatched environment and/or cultures detected: some of theses results files are not from the same test run."
            { doc1 with Summary = ResultSummary.append doc2.Summary doc1.Summary
                        Assemblies = doc2.Assemblies @ doc1.Assemblies }
    
    let foldAssemblyToProjectTuple (result, time, asserts) (assembly : XElement) = 
        let outResult = 
            match assembly?result, result with
            | "Failure", _ -> "Failure"
            | "Inconclusive", "Success" -> "Inconclusive"
            | _ -> result
        outResult, time + double assembly?time, asserts + int assembly?asserts
    
    let TestProjectSummary assemblies = assemblies |> List.fold foldAssemblyToProjectTuple ("Success", 0.0, 0)
    
    let createTestProjectNode assemblies = 
        let result, time, asserts = TestProjectSummary assemblies
        
        let projectEl = 
            elem "test-suite"
            |> attr "type" "Test Project"
            |> attr "name" ""
            |> attr "executed" "True"
            |> attr "result" result
            |> attr "time" time
            |> attr "asserts" asserts
        
        let results = elem "results"
        results.Add(Seq.toArray assemblies)
        projectEl.Add results
        projectEl
    
    let getXDocs directory filter = 
        Directory.GetFiles(directory, filter, SearchOption.AllDirectories)
        |> Array.toList
        |> List.map (fun fileName -> XDocument.Parse(File.ReadAllText(fileName)))
    
    /// Merges non-empty list of test result XDocuments into a single XElement
    let mergeXDocs xDocs : XElement = 
        xDocs
        |> List.map Doc.ofXDoc
        |> List.reduce Doc.append
        |> fun merged -> 
            let res = ResultSummary.toXElement merged.Summary
            res.Add [ Environment.toXElement merged.Env
                      Culture.toXElement merged.Culture
                      createTestProjectNode merged.Assemblies ]
            res
    
    let writeMergedNunitResults (directory, filter, outfile) = 
        getXDocs directory filter
        |> mergeXDocs
        |> sprintf "%O"
        |> WriteStringToFile false outfile
