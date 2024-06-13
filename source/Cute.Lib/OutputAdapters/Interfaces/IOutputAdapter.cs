﻿using System.Data;

namespace Cute.Lib.OutputAdapters;

public interface IOutputAdapter : IDisposable
{
    string FileName { get; }

    void AddHeadings(IEnumerable<string> headings);

    void AddRow(IDictionary<string, object?> row);

    void Save();
}