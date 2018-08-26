# Fake.Core.Vault module

[API-Reference](apidocs/v5/fake-core-vault.html)

## Goals and non-goals

The FAKE-Vault works similar to secret variables in VSTS (in fact they were added to better support VSTS).

Context:

* [VSTS secrets as environment variables](https://stackoverflow.com/questions/50110315/vsts-secrets-as-environment-variables/50113557#50113557)
* [Secret variables are not secure](https://github.com/Microsoft/vsts-tasks/issues/4284#issuecomment-300354042)

Goals:

* Prevent accidental leakage
* Hide from environment variable listing
* Hide from process snapshots
* Forward secret variables from your build server into FAKE without implicit access for all sub-processes started by FAKE

Non-Goals:

* Complete fool-proof implementation
* Hiding variables from build script writers
* Hiding variables from the build output (see below)
* Manage secrets in your repository instead of your build server (ie. committing the json)
  > Please look at related tools like [git-secret](https://github.com/sobolevn/git-secret) instead

## API Usage

In order to get variables into FAKE you need to encrypt them via AES. When in doubt consult the source code of `Vault.encryptVariable` or look at the `myVault.ts` implementation of the [vsts fake 5 task](https://github.com/isaacabraham/vsts-fsharp).

You need to create a json in the following format:

```json
{ "keyFile": "<Path to file of the 32 byte key, encoded as base64 and saved in utf8>",
  "iv": "<base64 string of the 16 byte IV>",
  "values": [
      { "secret": true,
        "value": "<the raw value or the aes encrypted base64 string value when 'secret' is true>",
        "name": "<name>" }
  ] }
```

And save it in a environment variable `FAKE_VAULT_VARIABLES` for example.

```fsharp

#r "paket:
nuget Fake.Core.Vault //"
open Fake.Core
let vault = Vault.fromFakeEnvironmentVariable()

let usage1 = vault |> Vault.get "my variable"
let usage2 = vault.Get "my other variable"
let tryUsage1 = vault |> Vault.tryGet "my variable"
let tryUsage2 = vault.TryGet "my other variable"
```

You can reference [`Fake.Core.Vault`](https://www.nuget.org/packages/Fake.Core.Vault/) in your regular project and use `Vault.encryptVariable` to simplify the creation of the json.

## Hide from Build output

By default the vault will not keep unencrypted variables in memory in order to 'protect' against memory dumps.
Once you retrieve a secret variable we recommend to use build server features (like VSTS secret variables) to remove values from the output.
If that is not an option you can use the [protect secrets](https://fake.build/core-trace.html#Protect-secrets) feature of the trace module.
Keep in mind that this keeps the variables in memory and is therefore an explicit opt-in for the variables you use.
