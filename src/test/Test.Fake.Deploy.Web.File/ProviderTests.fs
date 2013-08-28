module Test.Fake.Deploy.Web.File.xunitTest
    open System
    open System.IO
    open Xunit
    open Xunit.Extensions
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
        let s = toString ms
        Assert.Equal<string>("[{\"Id\":\"Admin\"}]", s)

    [<Fact>]
    let ``should save environment`` () =
        FileIO.Exists <- fun x -> false
        let ms = new MemoryStream()
        FileIO.Create <- fun x -> ms :> Stream

        Provider.init (Path.GetTempPath())
        Provider.saveEnvironments([{Name="Test"; Description="Test env"; Agents=[{Id="a"; Name="Orange"}]; Id="22"}])
        let s = toString ms
        Assert.Equal<string>("[{\"Agents\":[{\"Id\":\"a\",\"Name\":\"Orange\"}],\"Description\":\"Test env\",\"Id\":\"22\",\"Name\":\"Test\"}]", s)


    [<Fact>]
    let ``should delete one environment`` () =
        FileIO.Exists <- fun x -> true
        FileIO.OpenRead <- 
            fun (s:string) -> 
                toStream ("[{\"Agents\":[{\"Id\":\"a\",\"Name\":\"a\"}],\"Description\":\"Test env\",\"Id\":\"1\",\"Name\":\"a\"}," +
                          "{\"Agents\":[{\"Id\":\"b\",\"Name\":\"b\"}],\"Description\":\"Test env\",\"Id\":\"2\",\"Name\":\"b\"}," +
                          "{\"Agents\":[{\"Id\":\"c\",\"Name\":\"c\"}],\"Description\":\"Test env\",\"Id\":\"3\",\"Name\":\"c\"}]")
        let ms = new MemoryStream()
        FileIO.Create <- fun x -> ms :> Stream

        Provider.init (Path.GetTempPath())
        Provider.deleteEnvironment "2"
        let actual = toString ms
        let expected = "[{\"Agents\":[{\"Id\":\"a\",\"Name\":\"a\"}],\"Description\":\"Test env\",\"Id\":\"1\",\"Name\":\"a\"}," +
                       "{\"Agents\":[{\"Id\":\"c\",\"Name\":\"c\"}],\"Description\":\"Test env\",\"Id\":\"3\",\"Name\":\"c\"}]"

        Assert.Equal<string>(expected, actual)
