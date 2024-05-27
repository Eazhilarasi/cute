﻿
using Spectre.Console.Cli;
using Cut.Services;
using Spectre.Console;
using Cut.Constants;

namespace Cut.Commands;

public class InfoCommand : LoggedInCommand<InfoCommand.Settings>
{
    public InfoCommand(IConsoleWriter console, IPersistedTokenCache tokenCache) 
        : base(console, tokenCache)
    { }

    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        _console.WriteBlankLine();
        
        var result = await base.ExecuteAsync(context, settings);

        if (result != 0 || _contentfulClient == null) return result;

        var spaceTable = new Table()
            .RoundedBorder()
            .BorderColor(Globals.StyleDim.Foreground);

        spaceTable.AddColumn("Space");
        spaceTable.AddColumn("Id");
        spaceTable.AddColumn("Content Types");
       
        var typesTable = new Table()
            .RoundedBorder()
            .BorderColor(Globals.StyleDim.Foreground);

        typesTable.AddColumn("Type");
        typesTable.AddColumn("Id");
        typesTable.AddColumn("Fields").RightAligned();
        typesTable.AddColumn("Display Field");


        AnsiConsole.Status()
            .Spinner(Spinner.Known.Aesthetic)
            .Start("Getting info...", ctx =>
            {
                var space = _contentfulClient.GetSpace(_spaceId).Result;

                var contentTypes = (_contentfulClient.GetContentTypes(spaceId: _spaceId).Result)
                    .OrderBy(t => t.Name);

                foreach (var contentType in contentTypes)
                {
                    typesTable.AddRow(
                        new Markup(contentType.Name),
                        new Markup(contentType.SystemProperties.Id, Globals.StyleAlertAccent),
                        new Markup(contentType.Fields.Count.ToString()).RightJustified(),
                        new Markup(contentType.DisplayField)
                    );
                }

                spaceTable.AddRow(new Markup(space.Name, Globals.StyleAlert), new Markup(_spaceId), typesTable);
            });
       
        AnsiConsole.Write(spaceTable);

        _console.WriteBlankLine();

        return 0;
    }

}