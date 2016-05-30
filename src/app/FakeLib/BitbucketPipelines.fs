/// Contains code to configure FAKE for Bitbucket Pipelines integration
module Fake.BitbucketPipelines

/// Bitbucket Pipelines environment variables as [described](https://confluence.atlassian.com/bitbucket/environment-variables-in-bitbucket-pipelines-794502608.html)
type BitbucketPipelinesEnvironment = 
    
    /// The commit hash of a commit that kicked off the build
    static member Commit = environVar "BITBUCKET_COMMIT"

    /// The branch on which the build was kicked off. This value is only available on branches.
    static member Branch = environVar "BITBUCKET_BRANCH"
    
    /// The tag of a commit that kicked off the build. This value is only available on tags.
    static member Tag = environVar "BITBUCKET_TAG"
    
    /// The URL-friendly version of a repository name.
    static member RepoSlug = environVar "BITBUCKET_REPO_SLUG"
    
    /// The name of the account in which the repository lives
    static member RepoOwner = environVar "BITBUCKET_REPO_OWNER"
    
