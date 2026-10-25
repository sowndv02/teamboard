using Xunit;

// Docker tests share host port 8080 and the compose project name, so they must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
