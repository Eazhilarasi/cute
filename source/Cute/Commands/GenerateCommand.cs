﻿using Azure;
using Azure.AI.OpenAI;
using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Models;
using Contentful.Core.Search;
using Cute.Constants;
using Cute.Lib.Exceptions;
using Cute.Services;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using Scriban;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Dynamic;
using System.Text;
using Text = Spectre.Console.Text;

namespace Cute.Commands;

public class GenerateCommand : LoggedInCommand<GenerateCommand.Settings>
{
    public GenerateCommand(IConsoleWriter console, IPersistedTokenCache tokenCache)
        : base(console, tokenCache)
    {
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-c|--prompt-content-type")]
        [Description("The id of the content type containing prompts. Default is 'metaPrompts'.")]
        public string PromptContentType { get; set; } = "metaPrompt";

        [CommandOption("-f|--prompt-id-field")]
        [Description("The id of the field that contains the prompt key/title/id. Default is 'key'.")]
        public string PromptIdField { get; set; } = "key";

        [CommandOption("-i|--prompt-id")]
        [Description("The id of the Contentful prompt entry to generate prompts from.")]
        public string PromptId { get; set; } = default!;

        [CommandOption("-o|--output-content-type")]
        [Description("The id of the Contentful content type to generate content for.")]
        public string OutputContentType { get; set; } = "promptOutputContentType";

        [CommandOption("-t|--output-content-field")]
        [Description("The target field of the Contentful content type to generate content for.")]
        public string OutputContentField { get; set; } = "promptOutputContentField";

        [CommandOption("-s|--system-message-field")]
        [Description("The field containing the system prompt for the LLM.")]
        public string SystemMessageField { get; set; } = "systemMessage";

        [CommandOption("-p|--prompt-field")]
        [Description("The field containing the prompt template for the LLM.")]
        public string PromptField { get; set; } = "prompt";

        [CommandOption("-e|--temperature-field")]
        [Description("The field containing temperature setting for the LLM.")]
        public string TemperatureField { get; set; } = "temperature";

        [CommandOption("-a|--frequency-penalty-field")]
        [Description("The field containing frequency penalty setting for the LLM.")]
        public string FrequencyPenaltyField { get; set; } = "frequencyPenalty";

        [CommandOption("-l|--limit")]
        [Description("The total number of entries to generate content for before stopping. Default is five.")]
        public int Limit { get; set; } = 5;

        [CommandOption("-k|--skip")]
        [Description("The total number of entries to skip before starting. Default is zero.")]
        public int Skip { get; set; } = 0;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var result = await base.ExecuteAsync(context, settings);

        if (result != 0 || _contentfulClient == null || _appSettings == null) return result;

        var defaultLocale = (await _contentfulClient.GetLocalesCollection())
            .First(l => l.Default)
            .Code;

        var promptQuery = new QueryBuilder<Dictionary<string, object?>>()
             .ContentTypeIs(settings.PromptContentType)
             .Limit(1)
             .FieldEquals($"fields.{settings.PromptIdField}", settings.PromptId)
             .Build();

        var promptEntries = await _contentfulClient.GetEntriesCollection<Entry<JObject>>(promptQuery);

        if (!promptEntries.Any())
        {
            throw new CliException($"No prompt with title '{settings.PromptId}' found.");
        }

        var promptEntry = promptEntries.First();

        var promptContentTypeId = promptEntry.Fields[settings.OutputContentType]?[defaultLocale]?.Value<string>()
            ?? throw new CliException($"Prompt '{settings.PromptId}' does not contain a valid contentTypeId");

        var promptContentFieldId = promptEntry.Fields[settings.OutputContentField]?[defaultLocale]?.Value<string>()
            ?? throw new CliException($"Prompt '{settings.PromptId}' does not contain a valid contentFieldId");

        var promptSystemMessage = promptEntry.Fields[settings.SystemMessageField]?[defaultLocale]?.Value<string>()
            ?? throw new CliException($"Prompt '{settings.PromptId}' does not contain a valid systemMessage");

        var promptMainPrompt = promptEntry.Fields[settings.PromptField]?[defaultLocale]?.Value<string>()
            ?? throw new CliException($"Prompt '{settings.PromptId}' does not contain a valid prompt");

        var promptTemperature = promptEntry.Fields[settings.TemperatureField]?[defaultLocale]?.Value<float>()
            ?? throw new CliException($"Prompt '{settings.PromptId}' does not contain a valid temperature");

        var promptFrequencyPenalty = promptEntry.Fields[settings.FrequencyPenaltyField]?[defaultLocale]?.Value<float>()
            ?? throw new CliException($"Prompt '{settings.PromptId}' does not contain a valid frequency penalty");

        var contentType = await _contentfulClient.GetContentType(promptContentTypeId);

        if (contentType.Fields.FirstOrDefault(f => f.Id.Equals(promptContentFieldId)) == null)
        {
            throw new CliException($"{promptContentFieldId} does not exist in content type {contentType.SystemProperties.Id}");
        }

        AzureOpenAIClient client = new(
          new Uri(_appSettings.OpenAiEndpoint),
          new AzureKeyCredential(_appSettings.OpenAiApiKey));

        var chatClient = client.GetChatClient(_appSettings.OpenAiDeploymentName);

        var chatCompletionOptions = new ChatCompletionOptions()
        {
            Temperature = promptTemperature,
            MaxTokens = 800,
            FrequencyPenalty = promptFrequencyPenalty,
            PresencePenalty = 0,
        };

        var cfoptions = new ContentfulOptions()
        {
            DeliveryApiKey = _appSettings.ContentfulDeliveryApiKey,
            PreviewApiKey = _appSettings.ContentfulPreviewApiKey,
            SpaceId = _appSettings.ContentfulDefaultSpace,
            Environment = _appSettings.ContentfulDefaultEnvironment,
            ResolveEntriesSelectively = true,
        };
        var cfclient = new ContentfulClient(new HttpClient(), cfoptions);

        var entries = Entries(cfclient, contentType.SystemProperties.Id, contentType.DisplayField);

        var skipped = 0;
        var limit = 0;

        await foreach (var (entry, _) in entries)
        {
            var fieldValue = GetPropertyValue(entry, promptContentFieldId)?.ToString();

            if (!string.IsNullOrEmpty(fieldValue))
            {
                continue;
            }

            if (skipped < settings.Skip)
            {
                skipped++;
                continue;
            }

            _console.WriteBlankLine();

            _console.WriteHeading(GetPropertyValue(entry, contentType.DisplayField)?.ToString() ?? string.Empty);

            _console.WriteBlankLine();

            var prompt = ReplaceFields(promptMainPrompt, entry);

            _console.WriteDim(prompt);

            _console.WriteBlankLine();
            _console.WriteBlankLine();

            List<ChatMessage> messages = [
                new SystemChatMessage(promptSystemMessage),
                new UserChatMessage(prompt),
            ];

            _console.WriteBlankLine();

            var sb = new StringBuilder();

            await foreach (var part in chatClient.CompleteChatStreamingAsync(messages, chatCompletionOptions))
            {
                if (part == null || part.ToString() == null) continue;

                foreach (var token in part.ContentUpdate)
                {
                    sb.Append(token.Text);
                    AnsiConsole.Write(new Text(token.Text, Globals.StyleNormal));
                    await Task.Delay(20);
                }
            }

            var id = GetPropertyValue(entry, "$id")?.ToString();

            var objToUpdate = await _contentfulClient.GetEntry(id);

            var fieldDict = (JObject)objToUpdate.Fields;

            if (fieldDict[promptContentFieldId] == null)
            {
                fieldDict[promptContentFieldId] = new JObject(new JProperty(defaultLocale, sb.ToString()));
            }
            else if (fieldDict[promptContentFieldId] is JObject existingValues)
            {
                if (existingValues[defaultLocale] == null)
                {
                    existingValues.Add(new JProperty(defaultLocale, sb.ToString()));
                }
                else
                {
                    existingValues[defaultLocale] = sb.ToString();
                }
            }

            var updatedEntry = await _contentfulClient.CreateOrUpdateEntry<dynamic>(objToUpdate.Fields,
                id: objToUpdate.SystemProperties.Id,
                version: objToUpdate.SystemProperties.Version);

            _ = await _contentfulClient.PublishEntry(objToUpdate.SystemProperties.Id,
                objToUpdate.SystemProperties.Version!.Value + 1);

            _console.WriteBlankLine();
            _console.WriteBlankLine();
            _console.WriteRuler();

            if (++limit >= settings.Limit)
            {
                break;
            }
        }

        return 0;
    }

    private static object? GetPropertyValue(ExpandoObject obj, params string[] path)
    {
        if (obj is null) return null;

        if (path.Length == 0) return null;

        var dict = (IDictionary<string, object?>)obj;

        if (!dict.TryGetValue(path[0], out var value)) return null;

        if (value == null) return null;

        if (path.Length > 1 && value is ExpandoObject expando)
        {
            return GetPropertyValue(expando, path[1..]);
        }

        return value;
    }

    private static string ReplaceFields(string prompt, ExpandoObject entry)
    {
        var template = Template.Parse(prompt);

        var result = template.Render(new { entry }, member => member.Name);

        return result;
    }

    private static async IAsyncEnumerable<(ExpandoObject, ContentfulCollection<ExpandoObject>)> Entries(ContentfulClient client, string contentType, string orderByField)
    {
        var skip = 0;
        var page = 100;

        while (true)
        {
            var query = new QueryBuilder<ExpandoObject>()
                .ContentTypeIs(contentType)
                .Include(2)
                .Skip(skip)
                .Limit(page)
                .OrderBy($"fields.{orderByField}")
                .Build();

            var entries = await client.GetEntries<ExpandoObject>(queryString: query);

            if (!entries.Any()) break;

            foreach (var entry in entries)
            {
                yield return (entry, entries);
            }

            skip += page;
        }
    }
}