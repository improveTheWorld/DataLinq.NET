using BenchmarkDotNet.Attributes;
using DataLinq.Framework;

namespace DataLinq.Data.Benchmarks;

/// <summary>
/// Benchmarks comparing ObjectMaterializer performance vs raw reflection.
/// </summary>
[Config(typeof(DefaultBenchmarkConfig))]
[MemoryDiagnoser]
public class ObjectMaterializerBenchmarks
{
    private string[] _schema = null!;
    private object?[] _values = null!;
    private CtorMaterializationSession<PersonRecord>? _ctorSession;
    private MaterializationSession<PersonMutable>? _generalSession;

    [GlobalSetup]
    public void Setup()
    {
        _schema = new[] { "Name", "Age", "Salary" };
        _values = new object[] { "John Doe", 30, 75000.50m };

        // Pre-warm sessions (first-call compilation cost excluded from benchmark)
        _ctorSession = new CtorMaterializationSession<PersonRecord>(_schema);
        _generalSession = new MaterializationSession<PersonMutable>(_schema);
    }

    // =====================================================
    // CONSTRUCTOR-BASED MATERIALIZATION (CtorSession)
    // =====================================================

    [Benchmark(Baseline = true)]
    public PersonRecord Reflection_CreateInstance()
    {
        // Baseline: Pure reflection (what most ORMs do)
        var person = new PersonRecord(
            (string)_values[0]!,
            Convert.ToInt32(_values[1]),
            Convert.ToDecimal(_values[2]));
        return person;
    }

    [Benchmark]
    public PersonRecord CtorSession_Create()
    {
        return _ctorSession!.Create(_values);
    }

    // =====================================================
    // PROPERTY-BASED MATERIALIZATION (GeneralSession)
    // =====================================================

    [Benchmark]
    public PersonMutable Reflection_SetProperties()
    {
        var person = new PersonMutable();
        var type = typeof(PersonMutable);
        type.GetProperty("Name")!.SetValue(person, _values[0]);
        type.GetProperty("Age")!.SetValue(person, Convert.ToInt32(_values[1]));
        type.GetProperty("Salary")!.SetValue(person, Convert.ToDecimal(_values[2]));
        return person;
    }

    [Benchmark]
    public PersonMutable GeneralSession_Create()
    {
        return _generalSession!.Create(_values);
    }

    // =====================================================
    // ONE-SHOT MATERIALIZATION (No pre-warmed session)
    // =====================================================

    [Benchmark]
    public PersonMutable ObjectMaterializer_Create_OneShot()
    {
        return ObjectMaterializer.Create<PersonMutable>(_schema, (object[])_values)!;
    }
}

// Test models
public record PersonRecord(string Name, int Age, decimal Salary);

public class PersonMutable
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public decimal Salary { get; set; }
}
