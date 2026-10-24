using Xunit;

// Test docker dùng chung cổng host 8080 và tên dự án compose, nên không chạy song song.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
