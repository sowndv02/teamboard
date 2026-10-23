using Xunit;

// Hai kịch bản docker dùng chung cổng host 6060 và tên dự án compose, nên không được chạy song song.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
