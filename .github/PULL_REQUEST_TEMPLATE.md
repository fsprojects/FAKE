### Description

Please give a brief explanation about your change.

If available, link to an existing issue this PR fixes. For example:

- fixes #1234

## TODO

Feel free to open the PR and ask for help

- [ ] New (API-)documentation for new features exist (Note: API-docs are enough, additional docs are in `help/markdown`)
- [ ] unit or integration test exists (or short reasoning why it doesn't make sense)
  
  > Note: Consider using the `CreateProcess` API which can be tested more easily, see https://github.com/fsharp/FAKE/pull/2131/files#diff-4fb4a77e110fbbe8210205dfe022389b for an example (the changes in the `DotNet.Testing.NUnit` module)
  
- [ ] boy scout rule: "leave the code behind in a better state than you found it" (fix warnings, obsolete members or code-style in the places you worked in)     
- [ ] (if new module) the module has been linked from the "Modules" menu, edit `help/templates/template.cshtml`, linking to the API-reference is fine.
- [ ] (if new module) the module is in the correct namespace
- [ ] (if new module) the module is added to Fake.sln (`dotnet sln Fake.sln add src/app/Fake.*/Fake.*.fsproj`)
- [ ] Fake 5 [API guideline](https://fake.build/contributing.html#API-Design-Guidelines) is honored
