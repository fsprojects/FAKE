namespace Fake.IO

/// <summary>
/// Defines custom operators for manipulating files and directories in a file system
/// </summary>
module FileSystemOperators =
    /// <summary>
    /// Combines two path strings using Path.Combine. Trims leading slashes of the right operand.
    /// This makes <c>"/test" @@ "/sub"</c> return <c>/test/sub</c>
    /// </summary>
    ///
    /// <param name="path1">The first path to operate on</param>
    /// <param name="path2">The second path to operate on</param>
    let inline (@@) path1 path2 = Path.combineTrimEnd path1 path2
    
    /// <summary>
    /// Combines two path strings using Path.Combine
    /// </summary>
    /// 
    /// <param name="path1">The first path to operate on</param>
    /// <param name="path2">The second path to operate on</param>
    let inline (</>) path1 path2 = Path.combine path1 path2
