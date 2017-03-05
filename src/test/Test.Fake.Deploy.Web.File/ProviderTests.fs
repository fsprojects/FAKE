module Test.Fake.Deploy.Web.File.xunitTest
    open System
    open System.IO
    open Xunit
    open Fake.Deploy.Web.File

    let toStream (str:string) =
        let ms = new MemoryStream()
        let sr = new StreamWriter(ms)
        sr.Write(str)
        sr.Flush()
        ms.Seek(0L, SeekOrigin.Begin) |> ignore
        ms :> Stream

    let toString (ms:MemoryStream) =
        let b = ms.ToArray()
        let ms2 = new MemoryStream()
        ms2.Write(b,0,b.Length)
        ms2.Seek(0L, SeekOrigin.Begin) |> ignore
        (new StreamReader(ms2)).ReadToEnd()
    
    let removeAllWhiteSpaces s =
        Text.RegularExpressions.Regex.Replace(s, "\s", "")

    [<Fact>]
    let ``File doesnt exist when reading Roles`` () =
        Fake.Deploy.Web.File.FileIO.Exists <- fun x -> false
        let ms = new MemoryStream()
        Fake.Deploy.Web.File.FileIO.OpenRead <- fun x -> ms :> Stream

        Provider.init (Path.GetTempPath())
        let roles = Provider.getRoles()
        Assert.Empty(roles)
        
    [<Fact>]
    let ``Should save one role`` () =
        FileIO.Exists <- fun x -> false
        let ms = new MemoryStream()
        FileIO.Create <- fun x -> ms :> Stream
        Provider.init (Path.GetTempPath())        
        Provider.saveRoles [{Id = "Admin"}]
        let s = toString ms |> removeAllWhiteSpaces
        Assert.Equal<string>("[{\"Id\":\"Admin\"}]", s)

    [<Fact>]
    let ``should save environment`` () =
        FileIO.Exists <- fun x -> false
        let ms = new MemoryStream()
        FileIO.Create <- fun x -> ms :> Stream

        Provider.init (Path.GetTempPath())
        Provider.saveEnvironments([{Name="Test"; Description="desc"; Agents=[{Id="a"; Name="Orange"}]; Id="22"}])
        let s = toString ms |> removeAllWhiteSpaces
        Assert.Equal<string>("[{\"Id\":\"22\",\"Name\":\"Test\",\"Description\":\"desc\",\"Agents\":[{\"Id\":\"a\",\"Name\":\"Orange\"}]}]", s)


    [<Fact>]
    let ``should delete one environment`` () =
        FileIO.Exists <- fun x -> true
        FileIO.OpenRead <- 
            fun (s:string) -> 
                toStream ("[{\"Agents\":[{\"Id\":\"a\",\"Name\":\"a\"}],\"Description\":\"desc\",\"Id\":\"1\",\"Name\":\"a\"}," +
                          "{\"Agents\":[{\"Id\":\"b\",\"Name\":\"b\"}],\"Description\":\"desc\",\"Id\":\"2\",\"Name\":\"b\"}," +
                          "{\"Agents\":[{\"Id\":\"c\",\"Name\":\"c\"}],\"Description\":\"desc\",\"Id\":\"3\",\"Name\":\"c\"}]")
        let ms = new MemoryStream()
        FileIO.Create <- fun x -> ms :> Stream

        Provider.init (Path.GetTempPath())
        Provider.deleteEnvironment "2"
        let actual = toString ms |> removeAllWhiteSpaces
        let expected = "[{\"Id\":\"1\",\"Name\":\"a\",\"Description\":\"desc\",\"Agents\":[{\"Id\":\"a\",\"Name\":\"a\"}]}," +
                       "{\"Id\":\"3\",\"Name\":\"c\",\"Description\":\"desc\",\"Agents\":[{\"Id\":\"c\",\"Name\":\"c\"}]}]"

        Assert.Equal<string>(expected, actual)
