# JSON-Tools
Miscellaneous tools for working with JSON in Notepad++.

Any issues, feel free to email me at mjolsonsfca@gmail.com.

[Read the docs.](https://github.com/molsonkiko/JSON-Tools/blob/main/docs/README.md)

Includes:
1. a JSON parser that includes the line number of each JSON node (see JsonParser.cs, JNode.cs). 2-4x slower than Python's standard library JSON parser.
2. The JNode objects produced by JsonParser can be pretty-printed or compactly printed. This can also change the line numbers of each child JNode in-place to reflect the new format.
2. A YAML dumper that dumps valid equivalent YAML for *most* (but *not all*) JSON (see YamlDumper.cs). Most likely to have trouble with keys that contain colons or double quotes, and also values that contain newlines.
3. The JSON parser can also be used as a linter to identify the following errors in JSON:
    * string literals terminated by newlines
    * string literals enclosed by ' instead of "
    * unterminated string literals (lint shows location of starting quote)
    * invalidly escaped characters in strings
    * numbers with two decimal points
    * multiple consecutive commas in an array or object
    * commas before first or after last element in array or object
    * two array or object elements not separated by a comma
    * no colon between key and value in object
    * non-string key in object
    * wrong char closing object or array
4. RemesPath query language for pandas-style querying of JSON. Similar in concept to [JMESPath](https://jmespath.org/), but with added functionality including regular expressions and recursive search. Query execution speed appears to be comparable to Python's pandas, maybe between 50% slower and 50% faster.
    * See the [language specification.
    * See RemesPath.cs, RemesPathFunctions.cs, RemesPathLexer.cs
5. Function (see JsonTabularize.BuildTable) for converting JSON into tabular (arrays of objects) form, and outputting JSON in this form as a delimiter-separated-variables (e.g., CSV, TSV) file 
(see JsonTabularize.TableToCsv).

*There is not currently a working executable for this project. You will have to build it in Visual Studio in order to use it. I am working on a way to get around this.*

![preview screenshot](/json_viewer_preview.PNG?raw=true "JSON Viewer plug-in preview")

## Requirements

The algorithms in this package are derived from [my standalone .NET 6.0 Json-Tools app](https://github.com/molsonkiko/JSON-Tools). That app is perfectly functional and indeed more performant than this plugin, but you can't run it on your computer without the .NET 6.0 runtime installed.