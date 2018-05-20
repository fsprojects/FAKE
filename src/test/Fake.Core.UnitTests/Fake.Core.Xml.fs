module Fake.Core.XmlTests

open System.IO
open Fake.Core
open Fake.DotNet
open Expecto
open Expecto.Flip

let normalize (s:string) = s.Replace("\r", "")

[<Tests>]
let tests =
  testList "Fake.Core.Xml.Tests" [
    testCase "Xml issue #1692" <| fun _ ->
      let original = normalize """<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key>
    <string>foo</string>
</dict>
</plist>"""
      let expected = normalize """<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>CFBundleIdentifier</key>
    <string>whateva</string>
  </dict>
</plist>"""
      let tmpFile = Path.GetTempFileName()
      try
        File.WriteAllText(tmpFile, original)
        let bundleIdentifier = "whateva"
        Xml.pokeInnerText tmpFile "plist/dict/key[text()='CFBundleIdentifier']/following-sibling::string" bundleIdentifier
        let actual = File.ReadAllText tmpFile |> normalize
        Expect.equal "expected same xml" expected actual
      finally
        File.Delete(tmpFile)      
  ]
