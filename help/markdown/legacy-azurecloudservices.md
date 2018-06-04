# Packaging Azure Cloud Services

**Note:  This documentation is for FAKE before version 5 (or the non-netcore version). The new documentation can be found [here](apidocs/v5/fake-azure-cloudservices.html)**

FAKE can be used to create a Azure Cloud Service package (.cspkg) for use with e.g. Worker Roles.

Currently it does not support publishing, and has several restrictions: -

   1. The service definition file must be called ServiceDefinition.csdef.
   2. It only works for single-role workers.
   3. It does not perform a build of the worker role project - this should be done beforehand.
   4. Always packages from the Release outputs.

Assume a solution with the following structure: -

* MyCloudService.ccproj
    * Contains a single role, "MyRole".
    * Contains a ``ServiceDefinition.cscfg`` file.
* MyWorkerRole.fsproj
    * Referenced by MyCloudService as a Role.
    * Contains a typical ``RoleEntryPoint`` class.

You can package this cloud service thus: -

    Target "PackageCloudService" (fun _ ->
        PackageRole
            { CloudService = "MyCloudService"
              WorkerRole = "MyWorkerRole"
              SdkVersion = None
              OutputPath = None })

This will build ``MyCloudService.cspkg`` in the current directory (override using ``OutputPath``).

When packaging occurs, the task will scan the local machine for installed Azure SDK versions; if none is specified,
it will pick the highest version installed. You can override this with the SdkVersion property e.g. ``Some 2.4``.
You can plug this into a regular FAKE pipeline as follows: -

    "Clean"
      ==> "BuildApp"
      ==> "PackageCloudService"
