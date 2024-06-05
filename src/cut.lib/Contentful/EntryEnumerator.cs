﻿using Contentful.Core.Models.Management;
using Contentful.Core.Models;
using Contentful.Core.Search;
using Newtonsoft.Json.Linq;
using Contentful.Core;

namespace cut.lib.Contentful;

public static class EntryEnumerator
{
    public static IEnumerable<(Entry<JObject>, ContentfulCollection<Entry<JObject>>)> Entries(ContentfulManagementClient client, string contentType, string orderByField)
    {
        var skip = 0;
        var page = 100;

        while (true)
        {
            var query = new QueryBuilder<Dictionary<string, object?>>()
                .ContentTypeIs(contentType)
                .Skip(skip)
                .Limit(page)
                .OrderBy($"fields.{orderByField}")
                .Build();

            var entries = client.GetEntriesCollection<Entry<JObject>>(query).Result;

            if (!entries.Any()) break;

            foreach (var entry in entries)
            {
                yield return (entry, entries);
            }

            skip += page;
        }
    }
}