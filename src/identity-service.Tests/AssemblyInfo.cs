using Xunit;

// Docker tests share host port 6061 and the compose project name, so they must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
