using Xunit;

// The docker scenarios share host port 6060 and the compose project name, so they must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
