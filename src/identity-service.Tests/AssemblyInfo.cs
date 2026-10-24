using Xunit;

// Test docker dùng chung cổng host 6061 và tên dự án compose, nên không chạy song song.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
