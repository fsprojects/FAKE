namespace Fake.IO

module FileSystemOperators =
    /// Combines two path strings using Path.Combine. Trims leading slashes of the right operand.
    /// This makes `"/test" @@ "/sub"` return `/test/sub`
    ///
    /// ## Parameters
    ///  - `path1` - The first path to operate on
    ///  - `path2` - The second path to operate on
    let inline (@@) path1 path2 = Path.combineTrimEnd path1 path2
    
    /// Combines two path strings using Path.Combine
    /// 
    /// ## Parameters
    ///  - `path1` - The first path to operate on
    ///  - `path2` - The second path to operate on
    let inline (</>) path1 path2 = Path.combine path1 path2
