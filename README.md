# Contentful Update Tool & Extractor

![image](https://raw.githubusercontent.com/andresharpe/cute/master/docs/images/cute-logo.png)

## Introduction 

***cute*** is a stand-alone  cross-platform command line interface (CLI) that allows bulk downloads, editing, AI generation and uploads to and from a Contentful space and supports the following :-

- **CSV** - Contentful -> Comma delimeted files -> Contentful
- **TSV** - Contentful -> Tab delimeted files -> Contentful
- **Excel** - Contentful -> Excel xlsx workbook -> Contentful
- **Json** - Contentful -> Json -> Contentful
- **Yaml** - Contentful -> Yaml -> Contentful

You can also generate types for JavaScript or dotnet to keep your project in sync with your content space.

[Contentful](https://www.contentful.com/) is a headless content management system (CMS) that allows teams to store, manage and retrieve content for websites and apps.

## Installation

### Firstly, make sure you have the Dotnet SDK 8.0 package installed.

For windows (cmd or powershell):
```
winget install Microsoft.DotNet.SDK.8
```

Or, on linux and iOS
``` 
sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
```

### Then:
On windows you may have to close and re-open the command line propt (or Windows Terminal).

Install the ***cute*** cli by typing.
```
dotnet tool install -g cute
```

### To test whether the installation worked
Simply type
```
cute
```
This will display the 
cute help. You are ready to go! 🚀

## Getting Help

```
cute --help
```
![cute help screenshot](https://raw.githubusercontent.com/andresharpe/cute/master/docs/images/help-screen.png)

## Logging into Contentful
```
cute auth
```

![cut auth screenshot](https://raw.githubusercontent.com/andresharpe/cute/master/docs/images/auth.png)

## Display space summary
``` 
cute info
```
![cut info screenshot](https://raw.githubusercontent.com/andresharpe/cute/master/docs/images/info.png)

## Downloading data
The default format is 'excel' so the following is equivelent.
```
cute download --content-type <contentType> 

cute download --content-type <contentType> --format excel
```
For comma seperated values:
```
cute download --content-type <contentType> --format csv
```
For tab seperated values:
```
cute download --content-type <contentType> --format tsv
```
For json output:
```
cute download --content-type <contentType> --format json
```
For downloading to Yaml:
```
cute download --content-type <contentType> --format yaml
```
![cute download screenshot](https://raw.githubusercontent.com/andresharpe/cute/master/docs/images/download.png)

Typing `cute download --help` will list all currently available options and usage

```
USAGE:
    cute download [OPTIONS]

OPTIONS:
    -h, --help            Prints help information
    -c, --content-type    Specifies the content type to download data for
    -f, --format          The output format for the download operation (Excel/Csv/Tsv/Json/Yaml)
```

## Uploading/synchronizing data

You can upload content from a local file to contentful. The local file can be a previously downloaded and updated excel, sdv, tsv, json or yaml file.

![cute upload progress screenshot](https://raw.githubusercontent.com/andresharpe/cute/master/docs/images/upload-progress.png)

Changes are only applied to Contentful if `--apply` is specified. By default no changes will be applied so it works a bit like a "what-if" powershel switch without `--apply`.

![cute upload screenshot](https://raw.githubusercontent.com/andresharpe/cute/master/docs/images/upload.png)

Typing `cute upload --help` will sow the full usage and options.

```
USAGE:
    cute upload [OPTIONS]

OPTIONS:
    -h, --help            Prints help information
    -c, --content-type    Specifies the content type to download data for
    -p, --path            The local path to the file containg the data to sync
    -f, --format          The format of the file specified in '--path' (Excel/Csv/Tsv/Json/Yaml)
    -a, --apply           Apply and publish all the calculated changes. The default behaviour is to only list the detected changes
```

## For generating strong Javascript or Dotnet types

You can generate stronly typed classes for both c# and TypeScript usinh `cute`.

```
USAGE:
    cute typegen [OPTIONS]

OPTIONS:
    -h, --help            Prints help information
    -c, --content-type    Specifies the content type to generate types for. Default is all
    -o, --output          The local path to output the generated types to
    -l, --language        The language to generate types for (TypeScript/CSharp)
    -n, --namespace       The optional namespace for the generated type
```

## Content generation using OpenAI

You can generate content using OpenAI in bulk. Prompts are retrieved from your Contentful space. A typical prompt entry has an id, a system message, a prompt, points to a content type and field.  Something like :-

|Title|EntryField|
|-|-|
|title|Short text|
|SystemMessage|Long text|
|MainPrompt|Long text|
|ContentTypeId|Short text|
|ContentFieldId|Short text|

```
DESCRIPTION:
Use generative AI to help build drafts of your content.

USAGE:
    cute generate [OPTIONS]

OPTIONS:
    -h, --help                   Prints help information
    -c, --prompt-content-type    The id of the content type containing prompts. Default is 'prompts'
    -f, --prompt-field           The id of the field that contains the prompt key/title/id. Default is 'title'
    -i, --prompt-id              The title of the Contentful prompt entry to generate content from
    -l, --limit                  The total number of entries to generate content for before stopping. Default is five
    -s, --skip                   The total number of entries to skip before starting. Default is zero
```

