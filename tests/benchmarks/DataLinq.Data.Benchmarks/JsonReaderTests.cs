using DataLinq.Data;
using DataLinq.Data.Tests.Utilities;
using Xunit;

namespace DataLinq.Data.Tests.Json;

public class JsonReaderTests
{
    record JRec(int id, double amount, bool ok);

    [Fact]
    public async Task Reads_Array_FastPath()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "[{\"id\":1,\"amount\":5.2,\"ok\":true},{\"id\":2,\"amount\":7.1,\"ok\":false}]");
        var opts = new JsonReadOptions<JRec> { RequireArrayRoot = true };
        var list = new List<JRec>();
        await foreach (var item in Read.Json<JRec>(path, opts))
            list.Add(item);
        Assert.Equal(2, list.Count);
        Assert.Equal(2, opts.Metrics.RawRecordsParsed);
        Assert.Equal(2, opts.Metrics.RecordsEmitted);
        Assert.Equal(0, opts.Metrics.ErrorCount);
    }

    [Fact]
    public async Task Single_Object_FastPath()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "{\"id\":1,\"amount\":7.7,\"ok\":true}");
        var opts = new JsonReadOptions<JRec> { RequireArrayRoot = true, AllowSingleObject = true };
        var list = new List<JRec>();
        await foreach (var item in Read.Json<JRec>(path, opts))
            list.Add(item);
        Assert.Single(list);

        Assert.Equal(1, opts.Metrics.RawRecordsParsed);
        Assert.Equal(1, opts.Metrics.RecordsEmitted);
        Assert.Equal(0, opts.Metrics.ErrorCount);
    }


    [Fact(DisplayName = "[KnownIssue] Validation single-object metrics discrepancy")]
    public async Task Single_Object_Validation_Path_Metrics()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "{\"id\":1,\"amount\":2.0,\"ok\":true}");
        var opts = new JsonReadOptions<JRec>
        {
            RequireArrayRoot = true,
            AllowSingleObject = true,
            ValidateElements = true,
            ElementValidator = e => true
        };
        var list = new List<JRec>();
        await foreach (var item in Read.Json<JRec>(path, opts))
            list.Add(item);

        Assert.Single(list);
        Assert.Equal(1, opts.Metrics.RawRecordsParsed);
        Assert.Equal(1, opts.Metrics.RecordsEmitted);
        Assert.Equal(0, opts.Metrics.ErrorCount);
    }
}