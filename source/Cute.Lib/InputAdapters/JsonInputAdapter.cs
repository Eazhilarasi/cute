﻿using Newtonsoft.Json;

namespace Cute.Lib.InputAdapters;

internal class JsonInputAdapter : InputAdapterBase
{
    private readonly JsonInputData? _data;

    public JsonInputAdapter(string contentName, string? fileName) : base(fileName ?? contentName + ".json")
    {
        _data = JsonConvert.DeserializeObject<JsonInputData>(File.ReadAllText(FileName));
    }

    public override IDictionary<string, object?>? GetRecord()
    {
        throw new NotImplementedException();
    }

    public override int GetRecordCount()
    {
        return _data?.Items.Count ?? 0;
    }

    public override IEnumerable<IDictionary<string, object?>> GetRecords(Action<IDictionary<string, object?>, int>? action = null)
    {
        var results = _data?.Items;

        if (results is null) return [];

        var records = GetRecordCount();

        action?.Invoke(results.Last(), records);

        return results ?? [];
    }

    public override void Dispose()
    {
    }
}