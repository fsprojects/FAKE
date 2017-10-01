namespace Fake.IO

open System.IO

module FileSystemOperators =
    /// Combines two path strings using Path.Combine
    let inline (@@) path1 path2 = Path.combineTrimEnd path1 path2
    /// Combines two path strings using Path.Combine
    let inline (</>) path1 path2 = Path.combine path1 path2
