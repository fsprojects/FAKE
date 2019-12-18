# SignTool

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later. The old documentation can be found <a href="apidocs/v4/fake-signtoolhelper.html">here</a>.</p>
</div>


This module is a wrapper around the [signtool.exe](https://docs.microsoft.com/en-gb/windows/win32/seccrypto/signtool) tool, a command-line tool that digitally signs files, verifies signatures in files, or time stamps files.

The 3 supported functions are:

 - [SignTool.sign: digitally signing files](#Signing)
 - [SignTool.timeStamp: time stamping previously signed files](#Time-stamping)
 - [SignTool.verify: verify signed files](#Verifying)

Additional information:

 - [Common options: options common to all supported functions](#Common-options)
 - [Certificates: notes and how to get one](#Certificates)
 - [SHA1/SHA256: differences and when to use which](#SHA1-SHA256)

API Reference:

 - [`SignTool`](apidocs/v5/fake-tools-signtool.html)

<hr /><hr />

## Signing

Digitally signing files. A [certificate](#Certificates) is needed to do this.


### When the certificate is located in a .pfx file

Only PFX files are supported by signtool.exe.

```fsharp
open Fake.Tools

// certificate is in a file
let certificate = SignTool.SignCertificate.File { SignTool.CertificateFromFile.Create("path/to/certificate-file.pfx") with
                                                      Password = Some "certificate-password" }
// create SignOptions
let signOptions = SignTool.SignOptions.Create(certificate)
// files to sign
let filesToSign = ["program.exe"; "library.dll"]
// sign
SignTool.sign signOptions filesToSign
```

Only a subset of all options is shown in the example, see API Reference for all available options: [`CertificateFromFile`](apidocs/v5/fake-tools-signtool-certificatefromfile.html), [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html).

### When the certificate is located in a certificate store

All options are optional and any combination may be used, depending on specific needs.

If no `StoreName` is specified, the "My" store is opened.

```fsharp
open Fake.Tools

// certificate is in a store
let certificate = SignTool.SignCertificate.Store { SignTool.CertificateFromStore.Create() with
                                                       AutomaticallySelectCertificate = Some true
                                                       SubjectName = Some "subject"
                                                       StoreName = Some "My" }
// create SignOptions
let signOptions = SignTool.SignOptions.Create(certificate)
// files to sign
let filesToSign = ["program.exe"; "library.dll"]
// sign
SignTool.sign signOptions filesToSign
```

Only a subset of all options is shown in the example, see API Reference for all available options: [`CertificateFromStore`](apidocs/v5/fake-tools-signtool-certificatefromstore.html), [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html).

### Adding a time stamp

This option specifies that time stamping be done at the same time as signing. If you do not want to time stamp signed files, do not set this option. If you want to time stamp previously signed files, use the [Time stamping](#Time-stamping) function.

For more information about time stamping [see Time stamping](#Time-stamping).

Digest algorithm used for time stamping is set separately from the digest algorithm used for signing.

Uses SHA256 by default ([see SHA1/SHA256](#SHA1-SHA256)).

```fsharp
// add time stamp to the signature
let signOptions = { SignTool.SignOptions.Create(certificate) with
                        TimeStamp = Some (SignTool.TimeStampOption.Create()) }
```

Only a subset of all options is shown in the example, see API Reference for all available options: [`TimeStampOption`](apidocs/v5/fake-tools-signtool-timestampoption.html), [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html).

<hr /><hr />

## Time stamping

Time stamping previously signed files.

When signing a file, the signature is valid only as long as the certificate used to create it is valid. The moment the certificated expires, the signature becomes invalid.
Time stamping is used to extend the validity of the signature. A time stamp proves that the signature was created while the certificate was still valid and effectively extends the signature's validity indefinitely.

Release binaries should be time stamped. Testing and dev binaries are really only useful for a limited time and so it is not neccessary to time stamp them - this will also speed up builds, as time stamping takes a bit of time (mostly because of the need to communicate with a time stamping server).


### Default options

Uses SHA256 by default ([see SHA1/SHA256](#SHA1-SHA256)).

Time stamp server does not have to be from the same CA as the certificate. The default is "http://timestamp.digicert.com".

```fsharp
open Fake.Tools

// create TimeStampOptions
let timestampOptions = SignTool.TimeStampOptions.Create()
// files to time stamp
let filesToTimestamp = ["program.exe"; "library.dll"]
// sign
SignTool.timeStamp timestampOptions filesToTimestamp
```

Only a subset of all options is shown in the example, see API Reference for all available options: [`TimeStampOptions`](apidocs/v5/fake-tools-signtool-timestampoptions.html).

### Custom options

Use SHA1 and a custom time stamp server.

```fsharp
open Fake.Tools

// create TimeStampOptions
let timestampOptions = { SignTool.TimeStampOptions.Create() with
                            TimeStamp = { SignTool.TimeStampOption.Create() with
                                            Algorithm = Some SignTool.DigestAlgorithm.SHA1
                                            ServerUrl = Some "http://timestamp.digicert.com" } }
// files to time stamp
let filesToTimestamp = ["program.exe"; "library.dll"]
// sign
SignTool.timeStamp timestampOptions filesToTimestamp
```

Only a subset of all options is shown in the example, see API Reference for all available options: [`TimeStampOption`](apidocs/v5/fake-tools-signtool-timestampoption.html), [`TimeStampOptions`](apidocs/v5/fake-tools-signtool-timestampoptions.html).

<hr /><hr />

## Verifying

Verify signed files.

The verify command determines whether the signing certificate was issued by a trusted authority, whether the signing certificate has been revoked, and, optionally, whether the signing certificate is valid for a specific policy.


### Default options

```fsharp
open Fake.Tools

// create VerifyOptions
let verifyOptions = SignTool.VerifyOptions.Create()
// files to verify
let filesToVerify = ["program.exe"; "library.dll"]
// verify
SignTool.verify verifyOptions filesToVerify
```

Only a subset of all options is shown in the example, see API Reference for all available options: [`VerifyOptions`](apidocs/v5/fake-tools-signtool-verifyoptions.html).


### Custom options

```fsharp
open Fake.Tools

// create VerifyOptions
let verifyOptions = { SignTool.VerifyOptions.Create() with
                          AllSignatures = Some true
                          RootSubjectName = Some "root subject"
                          WarnIfNotTimeStamped = Some true }
// files to verify
let filesToVerify = ["program.exe"; "library.dll"]
// verify
SignTool.verify verifyOptions filesToVerify
```

Only a subset of all options is shown in the example, see API Reference for all available options: [`VerifyOptions`](apidocs/v5/fake-tools-signtool-verifyoptions.html).

<hr /><hr />

## Common options

All functions share some common options.


### ToolOptions

```fsharp
// set path to signtool.exe - if you want to use a specific version or you don't have Windows SDKs installed
// by default, an attempt will be made to locate it automatically in 'Program Files (x86)\Windows Kits'
let signOptionsWithToolPath = { signOptions with
                                    ToolOptions = Some { SignTool.ToolOptions.Create() with
                                                             ToolPath = Some "path/to/signtool.exe" } }

// set the timeout - default value is 10 seconds per file
let timestampOptionsWithTimeout = { timestampOptions with
                                        ToolOptions = Some { SignTool.ToolOptions.Create() with
                                                                 Timeout = Some (TimeSpan.FromMinutes 1.0) } }

// set the working directory - uses current directory by default
let verifyOptionsWithWorkingDir = { verifyOptions with
                                        ToolOptions = Some { SignTool.ToolOptions.Create() with
                                                                 WorkingDir = Some (Directory.GetCurrentDirectory()) } }
```

API Reference: [`ToolOptions`](apidocs/v5/fake-tools-signtool-tooloptions.html).

### Debug

Displays debugging information (signtool option: /debug). This option is not set by default.

```fsharp
// display debugging information (/debug)
let signOptionsWithDebug = { signOptions with
                                 Debug = Some true }

// do not display debugging information
let timestampOptionsWithDebug = { timestampOptions with
                                      Debug = Some false }

// use default
let verifyOptionsWithoutDebug = { verifyOptions with
                                      Debug = None }
```

### Verbosity

Output verbosity (signtool options: /q, /v). This option is not set by default.

```fsharp
// set verbosity to verbose (/v)
let signOptionsWithVerbosity = { signOptions with
                                     Verbosity = Some SignTool.Verbosity.Verbose }

// set verbosity to quiet (/q)
let timestampOptionsWithVerbosity = { timestampOptions with
                                          Verbosity = Some SignTool.Verbosity.Quiet }

// use default
let verifyOptionsWithoutVerbosity = { verifyOptions with
                                          Verbosity = None }
```

API Reference: [`Verbosity`](apidocs/v5/fake-tools-signtool-verbosity.html).

<hr /><hr />

## Certificates

The SignTool needs a certificate to sign files.


### Prod / release

For production / release purposes a proper publically trusted code signing certificate can be purchased from many CA's.


### Dev / test

For dev and testing purposes a certificate can be created using [`New-SelfSignedCertificate` PowerShell cmdlet](https://docs.microsoft.com/en-us/powershell/module/pkiclient/new-selfsignedcertificate):
```powershell
New-SelfSignedCertificate -CertStoreLocation cert:\currentuser\my `
-Subject "CN=My Company, Inc.;O=My Company, Inc.;L=My City;C=SK" `
-KeyAlgorithm RSA `
-KeyLength 2048 `
-Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
-KeyExportPolicy Exportable `
-KeyUsage DigitalSignature `
-Type CodeSigningCert
```
This creates the certificate under "Certificates - Current User" -> "Personal" -> "Certificates" and prints the certificate Thumbprint. The certificate can be used as-is using the [`CertificateFromStore`](#When-the-certificate-is-located-in-a-certificate-store) option.

If you want to export the certificate to a file, use [`Export-PfxCertificate` PowerShell cmdlet](https://docs.microsoft.com/en-us/powershell/module/pkiclient/export-pfxcertificate). Replace "{thumbprint}" with the value from `New-SelfSignedCertificate` output:
```powershell
$certpwd = ConvertTo-SecureString -String "mycertpassword" -Force -AsPlainText
Get-ChildItem -Path cert:\currentuser\my\{thumbprint} | Export-PfxCertificate -FilePath C:\certificate.pfx -Password $certpwd
```
Now the certificate can be used with the [`CertificateFromFile`](#When-the-certificate-is-located-in-a-pfx-file) option.

This certificate should not be used for prod / release purposes as it is self-signed and not trusted.

<hr /><hr />

## SHA1/SHA256

If the signed binaries are run on Windows 7 or newer, using SHA256 only is fine - this is also the default value for `DigestAlgorithm` (/fd and /td options).

If the signed binaries are run on Windows older than Windows 7, SHA1 should be used.

If the signed binaries are run on newer and older versions of Windows, then dual signing is probably the way to go. This means signing all binaries twice - first using SHA1, and then SHA256. Make sure to set `AppendSignature` to true when signing the second time, otherwise the first signature will be replaced. [More information about dual signing](https://support.ksoftware.net/support/solutions/articles/215805-the-truth-about-sha1-sha-256-dual-signing-and-code-signing-certificates-).
