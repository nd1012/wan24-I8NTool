# wan24-I8NTool

This is a small dotnet tool for extracting stings to translate from source 
code and writing the result in the PO format to a file or STDOUT. It can also 
be used to create i8n files, which are easy to use from any app.

It's pre-configured for use with the 
[`wan24-Core`](https://github.com/WAN-Solutions/wan24-Core) translation 
helpers for C#, but it can be customized easily for any environment and any 
programming language by customizing the used regular expressions in your own 
configuration file (find available presets in the `config` folder of this 
repository).

## Usage

### Where to get it

This is a dotnet tool and can be installed from the command line:

```bash
dotnet tool install -g wan24-I8NTool
```

The default installation folder is 

- `%USER%\.dotnet\tools` for Windows
- `~/.dotnet/tools` for Linux (or MAC)

**NOTE**: Please ensure that your global .NET tool path is in the `PATH` 
environment variable (open a new Windows terminal after adding the path using 
_Settings_ -> _System_ -> _Extended system settings_ -> _Extended_ -> 
_Environment variables_).

### Simple usage

With pre-configuration for .NET:

```bash
wan24I8NTool --input ./ > keywords.po
```

You can find other pre-configurations in the `config/*.json` files in the 
GitHub repository:

```bash
wan24I8NTool --config file.json --input ./ > keywords.po
```

If you want to use advanced options, you can display help like this:

```bash
wan24I8NTool help (--api API (--method METHOD)) (-details)
```

For individual usage support, please 
[open an issue here](https://github.com/nd1012/wan24-I8NTool/issues).

**NOTE**: The `wan4-Core` CLI configuration (`CliConfig`) will be applied, so 
advanced configuration is possible using those special command line arguments.

### Default keyword extraction process

Per default keywords will be found by the regular expressions you can find in 
`config/dotnet.json`. They'll then be post-processed by the replacement 
expressions, until the final quoted keyword was extracted from a line.

These default translation method names can be parsed from source code:

- `_`
- `gettext(n)("..."`
- `Translate(Plural)("..."`
- `GetTerm("..."`

There are also some attributes, which can define translated text:

- `StdIn("..."`
- `StdOut("..."`
- `StdErr("..."`
- `Description("..."`
- `DisplayText("..."`

And these attribute peoperty strings will also be parsed:

- `Example = "..."`
- `ErrorMessage = "..."`

Finally, there are also attributes, which get the text to translate as a 2nd 
argument after a numeric argument:

- `ExitCode(N, "..."`

To force including any string (from a constant definition, for example), 
simply add a comment `wan24I8NTool:include` at the end of the line - example:

```cs
public const string NAME = "Any PO included keyword";// wan24I8NTool:include
```

**NOTE**: (Multiline) concatenated string value definitions (like 
`@"Part \ a" + $"Part b {variable}"`) or interpolations can't be parsed. The 
matched string literal must be JSON style escaped.

### Custom parser configuration

In the `config/dotnet.json` file of this repository you find the default 
configuration. You can download and modify it for your needs, and use it with 
the `--config` parameter.

Example parser JSON configuration:

```json
{
	"SingleThread": false,// (optional) Set to true to disable multithreading (may be overridden by -singleThread)
	"Encoding": "UTF-8",// (optional) Source encoding to use (default is UTF-8; may be overridden by --encoding)
	"Patterns": [// (optional)
		{
			// Matching only pattern
			"Pattern": "Regular expression",// Pattern for use with RegEx
			"Options": "None"// RegexOptions enumeration
		},
		{
			// Matching and replacement pattern
			"Pattern": "Regular expression",// Pattern for use with RegEx
			"Options": "None",// RegexOptions enumeration
			"Replacement": "$1"// Replacement expression
		},
		{
			// Replacement-only pattern
			"Pattern": "Regular expression",// Pattern for use with RegEx
			"Options": "None",// RegexOptions enumeration
			"Replacement": "$1",// Replacement expression
			"ReplaceOnly": true// Disable use for matching
		}
		...
	],
	"FileExtensions": [// (optional) File extensions to include when walking through a folder tree (may be overridden by --ext)
		".ext",
		...
	],
	"MergeOutput": true,// (optional) Merge the extracted keywords to the existing output PO file
	"FailOnError": true,// (optional) To fail thewhole process on any error
	"Merge": false// (optional) Set to true to merge your custom configuration with the default configuration
}
```

When loading the configuration, the pattern property `Options` will be 
extended by the `SingleLine` and `Compiled` default options.

**TIPP**: You may use the variable `%{rxStringLiteral}` to match a double 
quoted string literal.

The parser looks for any matching non-replacement-only expression, then 
applies all matching replacement expressions to refer to the keyword to use, 
finally.

**NOTE**: The final keyword must be a valid JSON string literal in single or 
double quotes!

During merging lists will be combined, and single options will be overwritten.

There are some more optional keys for advanced configuration:

- `Core`: [`wan24-Core`](https://github.com/WAN-Solutions/wan24-Core) 
configuration using a `AppConfig` structure
- `CLI`: [`wan24-CLI`](https://github.com/nd1012/wan24-CLI) configuration 
using a `CliAppConfig` structure

### Build, extract, display and use an i8n file

i8n files contain optional compressed translation terms. They can be created 
from PO/MO and/or JSON dictionary (keyword as key, translation as an array of 
strings as value) input files like this:

```bash
wan24I8NTool i8n -compress --poInput /path/to/input.po --output /path/to/output.i8n
```

An i8n file can be embedded into an app, for example.

To convert all `*.json|po|mo` files in the current folder to `*.i8n` files:

```bash
wan24I8NTool i8n buildmany -compress -verbose
```

To display some i8n file informations:

```bash
wan24I8NTool i8n display --input /path/to/input.i8n
```

To extract some i8n file to a JSON file (prettified):

```bash
wan24I8NTool i8n extract --input /path/to/input.i8n --jsonOutput /path/to/output.json
```

To extract some i8n file to a PO file:

```bash
wan24I8NTool i8n extract --input /path/to/input.i8n --poOutput /path/to/output.po
```

To extract some i8n file to a wan24-I8NLws file:

```bash
wan24I8NTool i8n extract --input /path/to/input.i8n --kwsOutput /path/to/output.po
```

**NOTE**: For more options and usage instructions please use the CLI API help 
(see below).

**TIPP**: You can use the i8n API for converting, merging and validating the 
supported source formats also.

In a .NET app you can use an i8n file using the `wan24-I8N(-Compressed)` NuGet 
packages and `wan24-Core`:

```cs
// Uncompressed using the wan24-I8N NuGet package
Translation.Current = new(await I8NTranslationTerms.FromStreamAsync(fileStream));

// Compressed using the wan24-I8N-Compressed NuGet package
Translation.Current = new(await I8NCompressedTranslationTerms.FromStreamAsync(fileStream));
```

The `FromStream(Async)` methods also allow to specify, if there's no i8n 
header to read.

Links to used NuGet packages:

- [wan24-Core](https://www.nuget.org/packages/wan24-Core/)
- [wan24-I8N](https://www.nuget.org/packages/wan24-I8N/)
- [wan24-I8N-Compressed](https://www.nuget.org/packages/wan24-I8N-Compressed/)

#### i8n file structure in detail

If you didn't skip writing a header during build, the first byte contains the 
version number and a flag (bit 8), if the body is compressed. The file body is 
a JSON encoded dictionary, having the keyword as ID, and the translations as 
value (an array of strings with none, one or multiple (plural) translations).

If compressed, the `wan24-Compression` default compression algorithm was used. 
This is Brotli at the time of writing. But please note that 
`wan24-Compression` writes a non-standard header before the body, which is 
required for compatibility of newer `wan24-Compression` library versions with 
older compressed contents.

**NOTE**: For using compressed i8n files, you'll have to use the 
[`wan24-Compression`](https://www.nuget.org/packages/wan24-Compression) NuGet 
package in your .NET app for decompressing the body.

Please see the `I8NApi(.Internals).cs` source code in this GitHub repository 
for C# code examples.

**TIPP**: Use compression and the i8n header only, if you're using the i8n 
file from a .NET app. Without a header and compression you can simply 
deserialize the JSON dictionary from the i8n file using any modern programming 
language.

### Steps to i8n

Internationalization (i8n) for apps is a common task to make string used in 
apps translatable. gettext is a tool which have been around for many years now 
and seem to satisfy developers, translators and end users.

The steps to i8n your app are:

1. use l10n methods in your code when you want to translate a term
1. extract keywords (terms) from your source code into a PO file using an 
extractor
1. translate the terms using an editor tool and create a MO file
1. load the MO file using your apps gettext-supporting library

`wan24-I8NTool` is a CLI tool which you can use as extractor to 
automatize things a bit more, and you're also free to use other translation 
file formats.

If you'd like to use the i8n file format from `wan24-I8NTool` in your .NET 
app, the last step is replaced by:

- convert the PO/MO file to an i8n file using `wan24-I8NTool`
- load the i8n file using your .NET app using the `wan24-I8N` library

This is one additional step, but maybe worth it, if you don't want to miss 
features like compressed i8n files ready-to-use i8n `wan24-Core` localization 
(l10n) features. You'll also not need to reference any gettext supporting 
library or do the parsing of the PO/MO format by yourself. You also may not 
need to reference the `wan24-I8N(-Compressed)` NuGet package, if you can 
manage to load the i8n structure by yourself (which is an easy task) - find 
examples in the `wan24-I8N(-Compressed)` projects in this repository.

Links to useful NuGet packages:

- [wan24-Core](https://www.nuget.org/packages/wan24-Core/)
- [wan24-I8N](https://www.nuget.org/packages/wan24-I8N/)
- [wan24-I8N-Compressed](https://www.nuget.org/packages/wan24-I8N-Compressed/)
