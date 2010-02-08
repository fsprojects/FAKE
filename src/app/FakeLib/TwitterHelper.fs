[<AutoOpen>]
module Fake.TwitterHelper

open System
open System.IO
open System.Net
open System.Web
open System.Xml
open System.Text
open REST

/// The output formats supported by Twitter. Not all of them can be used with all of the functions.
/// For more information about the output formats and the supported functions check the 
/// Twitter documentation at: http://groups.google.com/group/twitter-development-talk/web/api-documentation
type TwitterFormat =
| JSON
| XML
| RSS
| Atom
  
/// The various object types supported at Twitter.
type TwitterObject =
| Statuses
| Account
| Users
| Help
  
/// The various actions used at Twitter. Not all actions works on all object types.
/// For more information about the actions types and the supported functions Check the 
/// Twitter documentation at: http://groups.google.com/group/twitter-development-talk/web/api-documentation
type TwitterAction =
| Public_Timeline
| User_Timeline
| Friends_Timeline
| Friends
| Followers
| Update
| Account_Settings
| Featured
| Show 
| Test

  
type TwitterClientInfo =
  {
    /// Source is an additional parameters that will be used to fill the "From" field.
    /// Currently you must talk to the developers of Twitter at:
    /// http://groups.google.com/group/twitter-development-talk/
    /// Otherwise, Twitter will simply ignore this parameter and set the "From" field to "web".
    Source: string;
    /// Sets the name of the Twitter client.
    /// According to the Twitter Fan Wiki at http://twitter.pbwiki.com/API-Docs and supported by
    /// the Twitter developers, this will be used in the future (hopefully near) to set more information
    /// in Twitter about the client posting the information as well as future usage in a clients directory.
    ClientName: string;
    /// Sets the version of the Twitter client.
    /// According to the Twitter Fan Wiki at http://twitter.pbwiki.com/API-Docs and supported by
    /// the Twitter developers, this will be used in the future (hopefully near) to set more information
    ClientVersion: string;
    /// Sets the URL of the Twitter client.
    /// Must be in the XML format documented in the "Request Headers" section at:
    /// http://twitter.pbwiki.com/API-Docs.
    /// According to the Twitter Fan Wiki at http://twitter.pbwiki.com/API-Docs and supported by
    /// the Twitter developers, this will be used in the future (hopefully near) to set more information
    /// in Twitter about the client posting the information as well as future usage in a clients directory.    
    ClientUrl: string
  }

let REQUEST_TOKEN = "http://twitter.com/oauth/request_token"
let AUTHORIZE = "http://twitter.com/oauth/authorize"
let ACCESS_TOKEN = "http://twitter.com/oauth/access_token" 

/// Generates a twitter url
let twitterURL (twObject:TwitterObject) action (format:TwitterFormat) = 
  sprintf "http://twitter.com/%s/%s.%s" 
    (lowerString twObject) 
    (lowerString action)
    (lowerString format)                 
  

      
/// Gets the public timeline as Xml
let GetAsXML outputF format =
  if format = JSON then
    invalidArg "format" "This function supports only XML based formats (XML, RSS, Atom)"

  GetAsXML (outputF format) 

let ExecutePostCommand clientInfo =
  ExecutePostCommand (fun headers -> 
    headers.Add("X-Twitter-Client", clientInfo.ClientName)
    headers.Add("X-Twitter-Version", clientInfo.ClientVersion)
    headers.Add("X-Twitter-URL", clientInfo.ClientUrl))

       
/// Tests the twitter server
let GetTestMessage format =
  if format <> JSON && format <> XML then
    invalidArg "format" "Test supports only XML and JSON output format"
      
  twitterURL Help Test format |> ExecuteGetCommand null null    
   
/// Gets the public timeline
let GetPublicTimeline format =
  twitterURL Statuses Public_Timeline format
   |> ExecuteGetCommand null null     

/// Gets the public timeline as Xml
let GetPublicTimelineAsXML format = GetAsXML GetPublicTimeline format 

let getUserUrl userName password screenName action format =
  if String.IsNullOrEmpty screenName then
    twitterURL Statuses action format      
  else
    twitterURL Statuses (lowerString action + "/" + screenName) format     

/// Gets the users timeline
let GetUserTimeline userName password screenName format =
  getUserUrl userName password screenName User_Timeline format
    |> ExecuteGetCommand userName password

/// Gets the users timeline as XML
let GetUserTimelineAsXML userName password screenName format =
  GetAsXML (GetUserTimeline userName password screenName) format

/// Gets the friends timeline
let GetFriendsTimeline userName password format =
  twitterURL Statuses Friends_Timeline format 
   |> ExecuteGetCommand userName password
   
/// Gets the users timeline as XML
let GetFriendsTimelineAsXML userName password format =
  GetAsXML (GetFriendsTimeline userName password) format     

/// Gets the user's friends
let GetFriends userName password screenName format =
  if format <> JSON && format <> XML then
    new ArgumentException("GetFriends supports only XML and JSON output format", "format")
      |> raise
          
  getUserUrl userName password screenName Friends format    
    |> ExecuteGetCommand userName password
  
/// Gets the user's friends as XML
let GetFriendsAsXML userName password screenName =
  GetAsXML (GetFriends userName password screenName) XML    
  
/// Gets the user's follower
let GetFollowers userName password screenName format =
  if format <> JSON && format <> XML then
    invalidArg "format" "GetFollowers supports only XML and JSON output format"
            
  getUserUrl userName password screenName Followers format    
    |> ExecuteGetCommand userName password
  
/// Gets the users timeline as XML
let GetFollowersAsXML userName password screenName =
  GetAsXML (GetFollowers userName password screenName) XML            

/// Sends an update to twitter
let Update clientInfo userName password (status:string) format =
  let url = twitterURL Statuses Update format
  let data = "status=" + HttpUtility.UrlEncode(status)
  ExecutePostCommand clientInfo url userName password data

/// Sends an update to twitter and gets the result as XML
let UpdateAsXML clientInfo userName password status =
  GetAsXML (Update clientInfo userName password status) XML      

/// Gets the featured info
let GetFeatured userName password format =
  if format <> JSON && format <> XML then
    invalidArg "format" "GetFeature supports only XML and JSON output format"
            
  twitterURL Statuses Featured format    
    |> ExecuteGetCommand userName password
  
/// Gets the featured info as XML
let GetFeaturedAsXML userName password =
  GetAsXML (GetFeatured userName password) XML   
  
  
/// Shows the user
let Show userName password screenName format =
  if format <> JSON && format <> XML then
    invalidArg "format" "GetFollowers supports only XML and JSON output format"
  
  let url =          
    if String.IsNullOrEmpty screenName then
      twitterURL Users Show format      
    else
      twitterURL Users (lowerString Show + "/" + screenName) format    
                  
  ExecuteGetCommand userName password url
  
/// Shows the user as XML
let ShowAsXML userName password screenName =
  GetAsXML (Show userName password screenName) XML          