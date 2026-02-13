using DataLinq;
using System.Collections.Concurrent;

namespace DataLinq.Data.Tests.Utilities;

public sealed class InMemoryErrorSink : IReaderErrorSink
{
    public ConcurrentBag<ReaderError> Errors { get; } = new();
    public void Report(ReaderError error) => Errors.Add(error);
    public void Dispose() { }
}