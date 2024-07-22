﻿using Contentful.Core.Models;
using Cute.Lib.Enums;
using Cute.Lib.Exceptions;
using Cute.Lib.TypeGenAdapter;
using Cute.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Cute.Commands;

public class TypeGenCommand : LoggedInCommand<TypeGenCommand.Settings>
{
    public TypeGenCommand(IConsoleWriter console, IPersistedTokenCache tokenCache)
     : base(console, tokenCache)
    {
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-c|--content-type")]
        [Description("Specifies the content type to generate types for. Default is all.")]
        public string? ContentType { get; set; } = null!;

        [CommandOption("-o|--output")]
        [Description("The local path to output the generated types to")]
        public string OutputPath { get; set; } = default!;

        [CommandOption("-l|--language")]
        [Description("The language to generate types for (TypeScript/CSharp)")]
        public GenTypeLanguage Language { get; set; } = GenTypeLanguage.TypeScript!;

        [CommandOption("-n|--namespace")]
        [Description("The optional namespace for the generated type")]
        public string? Namespace { get; set; } = default!;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (settings.OutputPath is null)
        {
            settings.OutputPath = Directory.GetCurrentDirectory();
        }
        else if (settings.OutputPath is not null)
        {
            if (Directory.Exists(settings.OutputPath))
            {
                var dir = new DirectoryInfo(settings.OutputPath);
                settings.OutputPath = dir.FullName;
            }
            else
            {
                throw new CliException($"Path {Path.GetFullPath(settings.OutputPath)} does not exist.");
            }
        }

        settings.ContentType ??= "*";

        return base.Validate(context, settings);
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var result = await base.ExecuteAsync(context, settings);

        if (result != 0 || _contentfulManagementClient == null || _appSettings == null) return result;

        List<ContentType> contentTypes = settings.ContentType == "*"
            ? (await _contentfulManagementClient.GetContentTypes()).OrderBy(ct => ct.Name).ToList()
            : [await _contentfulManagementClient.GetContentType(settings.ContentType)];

        ITypeGenAdapter adapter = TypeGenFactory.Create(settings.Language);

        await adapter.PreGenerateTypeSource(contentTypes, settings.OutputPath, null, settings.Namespace);

        foreach (var contentType in contentTypes)
        {
            var fileName = await adapter.GenerateTypeSource(contentType, settings.OutputPath, null, settings.Namespace);

            _console.WriteNormal(fileName);
        }

        await adapter.PostGenerateTypeSource();

        return 0;
    }
}