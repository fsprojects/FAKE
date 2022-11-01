namespace Fake.BuildServer

open Fake.Core

/// <summary>
/// Native support for Bitbucket Pipelines specific APIs.
/// </summary>
/// <remarks>
/// The general documentation on how to use CI server integration can be found <a href="/guide/buildserver.html">here</a>.
/// This module does not provide any special APIs please use FAKE APIs and they should integrate into this CI server.
/// If some integration is not working as expected or you have features you would like to use directly please open an issue.
/// </remarks>
[<RequireQualifiedAccess>]
module BitbucketPipelines =
    /// Contains references to Bitbucket Pipeline environment variables
    /// See the <a href="https://support.atlassian.com/bitbucket-cloud/docs/variables-and-secrets/">
    /// official documentation</a> for details.
    type BitbucketPipelinesEnvironment =

        /// The commit hash of a commit that kicked off the build
        static member Commit = Environment.environVar "BITBUCKET_COMMIT"

        /// The branch on which the build was kicked off. This value is only available on branches.
        static member Branch = Environment.environVar "BITBUCKET_BRANCH"

        /// The tag of a commit that kicked off the build. This value is only available on tags.
        static member Tag = Environment.environVar "BITBUCKET_TAG"

        /// The URL-friendly version of a repository name.
        static member RepoSlug = Environment.environVar "BITBUCKET_REPO_SLUG"

        /// The UUID of the repository.
        static member RepoUuid = Environment.environVar "BITBUCKET_REPO_UUID"

        /// The full name of the repository (everything that comes after http://bitbucket.org/).
        static member RepoFullName = Environment.environVar "BITBUCKET_REPO_FULL_NAME"

        /// The absolute path of the directory that the repository is cloned into within the Docker container.
        static member CloneDir = Environment.environVar "BITBUCKET_CLONE_DIR"

        /// Default value is true. Gets set whenever a pipeline runs.
        static member CI = Environment.environVar "CI"

        /// The unique identifier for a build. It increments with each build and can be used to create unique artifact names.
        static member BuildNumber = Environment.environVar "BITBUCKET_BUILD_NUMBER"

        /// The pull request ID Only available on a pull request triggered build.
        static member PrId = Environment.environVar "BITBUCKET_PR_ID"

        /// The URL friendly version of the environment name.
        static member DeploymentEnvironment =
            Environment.environVar "BITBUCKET_DEPLOYMENT_ENVIRONMENT"
