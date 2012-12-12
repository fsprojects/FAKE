[<AutoOpen>]
module Fake.FileSetHelper

open System.IO
open System.Globalization
open System.Text
open System.Text.RegularExpressions

/// The FileSet is eagerly loaded into a list of strings.
/// The scan is only done once.
type EagerFileSet = string list

/// The FileSet is lazy loaded into a sequence of strings.
/// Every time the FileSet is used it scans again.
type LazyFileSet = string seq

type RegexEntry =
  { IsRecursive : bool;
    BaseDirectory: string;
    Pattern: string}

type FileIncludes =
  { BaseDirectories: string list;
    Includes: string list;
    Excludes: string list}
    
/// Patterns can use either / \ as a directory separator.
/// cleanPath replaces both of these characters with Path.DirectorySeparatorChar
let cleanPathBuilder (path:string) =    
    (new StringBuilder(path))
      .Replace('/',  Path.DirectorySeparatorChar)
      .Replace('\\', Path.DirectorySeparatorChar)
    
/// Patterns can use either / \ as a directory separator.
/// cleanPath replaces both of these characters with Path.DirectorySeparatorChar
let cleanPath path = (cleanPathBuilder path).ToString()      
    
let combinePath baseDirectory path =
    baseDirectory @@ cleanPath(path)
      |> Path.GetFullPath
            
/// The base directory to scan. The default is the 
/// <see cref="Environment.CurrentDirectory">current directory</see>.
let baseDirectory value = cleanPath value |> directoryInfo
  
/// Ensures that the last character of the given <see cref="string" />
/// matches Path.DirectorySeparatorChar.          
let ensureEndsWithSlash value =
    if endsWithSlash value then value else
    value + string Path.DirectorySeparatorChar
  
/// Converts search pattern to a regular expression pattern.
let regexPattern originalPattern =
  let pattern = cleanPathBuilder originalPattern

  // The '\' character is a special character in regular expressions
  // and must be escaped before doing anything else.
  let pattern = pattern.Replace(@"\", @"\\")

  // Escape the rest of the regular expression special characters.
  // NOTE: Characters other than . $ ^ { [ ( | ) * + ? \ match themselves.
  // TODO: Decide if ] and } are missing from this list, the above
  // list of characters was taking from the .NET SDK docs.
  let pattern = 
     pattern
       .Replace(".", @"\.")
       .Replace("$", @"\$")
       .Replace("^", @"\^")
       .Replace("{", @"\{")
       .Replace("[", @"\[")
       .Replace("(", @"\(")
       .Replace(")", @"\)")
       .Replace("+", @"\+")

  // Special case directory seperator string under Windows.
  let seperator =
    let s = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)
    if s = @"\" then @"\\" else s
    
  let replacementSeparator = seperator

  // Convert pattern characters to regular expression patterns.

  // Start with ? - it's used below
  let pattern = pattern.Replace("?", "[^" + seperator + "]?")

  // SPECIAL CASE: any *'s directory between slashes or at the end of the
  // path are replaced with a 1..n pattern instead of 0..n: (?<=\\)\*(?=($|\\))
  // This ensures that C:\*foo* matches C:\foo and C:\* won't match C:.
  let pattern = 
    new StringBuilder(
      Regex.Replace(
        pattern.ToString(),
        "(?<=" + seperator + ")\\*(?=($|" + seperator + "))",
        "[^" + replacementSeparator + "]+"))

  // SPECIAL CASE: to match subdirectory OR current directory, If
  // we do this then we can write something like 'src/**/*.cs'
  // to match all the files ending in .cs in the src directory OR
  // subdirectories of src.
  let pattern = 
    pattern
      .Replace(seperator + "**" + seperator, replacementSeparator + "(.|?" + replacementSeparator + ")?" )
      .Replace("**" + seperator, ".|(?<=^|" + replacementSeparator + ")" )
      .Replace(seperator + "**", "(?=$|" + replacementSeparator + ").|" )
      // .| is a place holder for .* to prevent it from being replaced in next line
      .Replace("**", ".|")
      .Replace("*", "[^" + replacementSeparator + "]*")
      .Replace(".|", ".*") // replace place holder string

  // Help speed up the search
  let pattern = 
    if pattern.Length = 0 then pattern else
    pattern
      .Insert(0, '^') // start of line        
      .Append('$') // end of line
        
  let patternText = 
    let s = pattern.ToString()
    let s1 = if s.StartsWith("^.*") then s.Substring(3) else s
    if s1.EndsWith(".*$") then s1.Substring(0, pattern.Length - 3) else s1
    
  patternText.ToString()
  
  
/// Given a search pattern returns a search directory and an regex search pattern.
let parseSearchDirectoryAndPattern (baseDir:DirectoryInfo) originalPattern =
  let s = cleanPath originalPattern
  
  // Get indices of pieces used for recursive check only
  let indexOfFirstDirectoryWildcard = s.IndexOf("**")
  let indexOfLastOriginalDirectorySeparator = s.LastIndexOf(Path.DirectorySeparatorChar)

  // search for the first wildcard character (if any) and exclude the rest of the string beginnning from the character
  let wildcards = [| '?'; '*' |]
  let indexOfFirstWildcard = s.IndexOfAny(wildcards)
  let s = if indexOfFirstWildcard <> -1 then s.Substring(0, indexOfFirstWildcard) else s

  // find the last DirectorySeparatorChar (if any) and exclude the rest of the string
  let indexOfLastDirectorySeparator = s.LastIndexOf Path.DirectorySeparatorChar

  // The pattern is potentially recursive if and only if more than one base directory could be matched.
  // ie: 
  //    **
  //    **/*.txt
  //    foo*/xxx
  //    x/y/z?/www
  // This condition is true if and only if:
  //  - The first wildcard is before the last directory separator, or
  //  - The pattern contains a directory wildcard ("**")
  let recursivePattern = 
    (indexOfFirstWildcard <> -1 && (indexOfFirstWildcard < indexOfLastOriginalDirectorySeparator )) ||
       indexOfFirstDirectoryWildcard <> -1

  // substring preceding the separator represents our search directory 
  // and the part following it represents nant search pattern relative 
  // to it
  
  let s = 
    if indexOfLastDirectorySeparator = -1 then "" else
    let s1 = originalPattern.Substring(0, indexOfLastDirectorySeparator)
    if s1.Length = 2 && s.[1] = Path.VolumeSeparatorChar then
      ensureEndsWithSlash s1
    else
      s1      
  
  // We only prepend BaseDirectory when s represents a relative path.
  let searchDirectory =
    if Path.IsPathRooted s then
        Path.GetFullPath s
    else 
        // we also (correctly) get to this branch of code when s.Length == 0
        baseDir.FullName @@ s
          |> Path.GetFullPath
  
  // remove trailing directory separator character, fixes bug #1195736
  //
  // do not remove trailing directory separator if search directory is
  // root of drive (eg. d:\)
  let searchDirectory =
    if endsWithSlash searchDirectory && 
        (searchDirectory.Length <> 3 || searchDirectory.[1] <> Path.VolumeSeparatorChar) 
    then
      searchDirectory.Substring(0, searchDirectory.Length - 1)
    else
      searchDirectory

  let modifiedPattern = originalPattern.Substring(indexOfLastDirectorySeparator + 1)    
  
  let regexPattern,isRegex = 
    if indexOfFirstWildcard = -1 then
      combinePath baseDir.FullName originalPattern,false
    else
      //if the fs in case-insensitive, make all the regex directories lowercase.
     regexPattern modifiedPattern,true
     
  searchDirectory, recursivePattern, isRegex, regexPattern


/// Parses specified search patterns for search directories and 
/// corresponding regex patterns.
let convertPatterns baseDir patterns =
  patterns
    |> List.fold (fun (regExPatterns,names) pattern ->
        let searchDirectory, isRecursive, isRegex, regexPattern = 
           parseSearchDirectoryAndPattern baseDir pattern
        
        if isRegex then
          if regexPattern.EndsWith(@"**/*") || regexPattern.EndsWith(@"**\*") then
            failwith "**/* pattern may not produce desired results"          
          { IsRecursive = isRecursive;
            BaseDirectory = searchDirectory;
            Pattern = regexPattern}
            :: regExPatterns,names                          
        else
          let exactName = searchDirectory @@ regexPattern
          if names |> List.exists ((=) exactName) then
              regExPatterns,names
          else
              regExPatterns,exactName::names)
       ([],[])
    

open System.Collections.Generic

let cachedCaseSensitiveRegexes   = new Dictionary<_,_>()
let cachedCaseInsensitiveRegexes = new Dictionary<_,_>()

let testRegex caseSensitive (path:string) (entry:RegexEntry) =
  let regexCache = if caseSensitive then cachedCaseSensitiveRegexes else cachedCaseInsensitiveRegexes
  let regexOptions = if caseSensitive then RegexOptions.Compiled else RegexOptions.Compiled ||| RegexOptions.IgnoreCase
  let r = lookup entry.Pattern (fun () -> new Regex(entry.Pattern, regexOptions)) regexCache
    
  // Check to see if the empty string matches the pattern
  if path.Length = entry.BaseDirectory.Length then r.IsMatch "" else

  if endsWithSlash entry.BaseDirectory then
      r.IsMatch(path.Substring(entry.BaseDirectory.Length))
  else
      r.IsMatch(path.Substring(entry.BaseDirectory.Length + 1))

          
let isPathIncluded path caseSensitive compareOptions includeNames includedPatterns excludeNames excludedPatterns =     
  let compare = CultureInfo.InvariantCulture.CompareInfo
  let included =
       
    let nameIncluded = // check path against include names
      includeNames
        |> List.exists (fun name -> compare.Compare(name, path, compareOptions) = 0)
      
    if nameIncluded then true else // check path against include regexes
    includedPatterns |> List.exists (testRegex caseSensitive path)
  
  if not included then false else
      
  // check path against exclude names
  if excludeNames |> List.exists (fun name -> compare.Compare(name, path, compareOptions) = 0) then false else            
  
  // check path against exclude regexes
  not (excludedPatterns |> List.exists (testRegex caseSensitive path))
 
  
/// Searches a directory recursively for files and directories matching 
/// the search criteria.
let rec scanDirectory caseSensitive includeNames 
     includePatterns excludeNames excludePatterns path recursivePattern =
  if not <| Directory.Exists(path) then Seq.empty else

  let currentDirectoryInfo = directoryInfo path

  let compare = CultureInfo.InvariantCulture.CompareInfo    
  let compareOptions = 
    if not caseSensitive then CompareOptions.IgnoreCase else CompareOptions.None
  
  // Only include the valid patterns for this path
  let includedPatterns = 
    includePatterns |> List.fold (fun acc entry ->          
      // check if the directory being searched is equal to the 
      // base directory of the RegexEntry
      if compare.Compare(path, entry.BaseDirectory, compareOptions) = 0 then entry::acc else
      
      // check if the directory being searched is subdirectory of 
      // base directory of RegexEntry
      if entry.IsRecursive &&        
           compare.IsPrefix(path,ensureEndsWithSlash entry.BaseDirectory, compareOptions) 
      then entry :: acc else acc)
     []          
    
  let excludedPatterns =
    excludePatterns |> List.fold (fun acc entry ->            
      if entry.BaseDirectory.Length = 0 || 
          compare.Compare(path, entry.BaseDirectory, compareOptions) = 0 then entry::acc else 
      // check if the directory being searched is subdirectory of 
      // basedirectory of RegexEntry

      if entry.IsRecursive &&
           compare.IsPrefix(path,ensureEndsWithSlash entry.BaseDirectory, compareOptions)
      then entry :: acc else acc)
      []         

  seq {
    for dirInfo in subDirectories currentDirectoryInfo do          
      if recursivePattern then
        yield! scanDirectory caseSensitive includeNames includePatterns 
                 excludeNames excludePatterns dirInfo.FullName recursivePattern
      else
        if isPathIncluded dirInfo.FullName caseSensitive compareOptions includeNames includedPatterns excludeNames excludePatterns then
          yield dirInfo.FullName

    // scan files
    for fi in filesInDir currentDirectoryInfo do
      let fileName = path @@ fi.Name
      if isPathIncluded fileName caseSensitive compareOptions includeNames includedPatterns excludeNames excludePatterns then                      
        yield fileName

    // check current path last so that delete task will correctly delete empty directories.
    if isPathIncluded path caseSensitive compareOptions includeNames includedPatterns excludeNames excludePatterns then
      yield path
 }
  
/// Searches the directories recursively for files and directories matching 
/// the search criteria.    
let Files baseDirs includes excludes =
  seq {    
    for actBaseDir in baseDirs do
      let baseDir = baseDirectory actBaseDir
      
      // convert given patterns to regex patterns with absolute paths
      let includePatterns, includeNames = convertPatterns baseDir includes      
      let excludePatterns, excludeNames = convertPatterns baseDir excludes 
         
      yield! scanDirectory false includeNames includePatterns excludeNames excludePatterns baseDir.FullName true}

/// Logs the given files with the message  
let Log message files = files |> Seq.iter (log << sprintf "%s%s" message)

/// The default base directory 
let DefaultBaseDir = Path.GetFullPath "."
  
/// Include files  
let Include x =    
    { BaseDirectories = [DefaultBaseDir];
      Includes = [x];
      Excludes = []}           
       
/// Lazy scan for include files
/// Will be processed at the time when needed
let Scan includes : LazyFileSet = Files includes.BaseDirectories includes.Includes includes.Excludes

/// Adds a directory as baseDirectory for fileIncludes  
let AddBaseDir dir fileInclude = {fileInclude with BaseDirectories = dir::fileInclude.BaseDirectories}    
    
/// Sets a directory as baseDirectory for fileIncludes  
let SetBaseDir (dir:string) fileInclude = {fileInclude with BaseDirectories = [dir.TrimEnd(directorySeparator.[0])]}   
      
/// Scans immediately for include files
/// Files will be memoized
let ScanImmediately includes : EagerFileSet = Scan includes |> Seq.toList
  
/// Include prefix operator
let (!+) x = Include x

/// Add Include operator
let (++) x y = {x with Includes = y::x.Includes}

/// Exclude operator
let (--) x y = {x with Excludes = y::x.Excludes}  

/// Includes a single pattern and scans the files - !! x = AllFilesMatching x
let (!!) x = !+ x |> Scan

/// Includes a single pattern and scans the files - !! x = AllFilesMatching x
let AllFilesMatching x = !! x