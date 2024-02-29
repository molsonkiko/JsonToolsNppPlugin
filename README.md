# JsonToolsNppPlugin

[![Continuous Integration](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/workflows/CI_build.yml/badge.svg)](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/workflows/CI_build.yml)

Miscellaneous tools for working with JSON in Notepad++. Includes a general-purpose tree view with powerful search capabilities.

If you have any issues, see if [updating to the latest release](https://github.com/molsonkiko/JsonToolsNppPlugin/releases) helps, and then feel free to raise an [issue](https://github.com/molsonkiko/JsonToolsNppPlugin/issues) on GitHub. Please be sure to include diagnostic information about your system, Notepad++ version, and plugin version (go to `?->Debug Info...` from the Notepad++ main menu).

## Features ##
1. [Pretty-print JSON](/docs/README.md#pretty_print_style) so that it's spread out over multiple lines.
2. [Compress JSON](/docs/README.md#minimal_whitespace_compression) so that it has little or no unnecessary whitespace.
3. Open a [drop-down tree view](/docs/README.md#the-basics) of the document. [Selecting a node](/docs/README.md#get-info-about-tree-nodes) in the tree navigates to the corresponding line in the document.
4. [Able to parse documents that have many different syntax errors](/docs/README.md#parser-settings), including but not limited to:
    * [The full JSON5 specification](https://json5.org/)
    * [Python-style comments](/CHANGELOG.md#4120---2023-03-28)
    * Missing commas and colons
    * Unterminated strings, arrays, and objects
5. [Get the path to the current line](/docs/README.md#path-to-current-line)
6. Query and edit JSON with:
    * a [find/replace form](/docs/README.md#find-and-replace-form)
    * an [array sorting form](/docs/README.md#sort-form)
    * the [RemesPath](/docs/RemesPath.md) query language.
7. A [regex search form](/docs/README.md#regex-search-form) for viewing and editing CSV files, or doing find/replace operations that involve math. 
8. Parse [JSON Lines](/docs/README.md#json-lines-documents) documents.
9. [A form for gettting JSON from APIs or many different local files](/docs/README.md#get-json-from-files-and-apis).
10. [JSON schema validation](/docs/README.md#validating-json-against-json-schema), including [automatic validation based on filenames](/docs/README.md#automatic-validation-of-json-against-json-schema).
11. [Generation of random JSON](/docs/README.md#generating-random-json-from-a-schema)
12. [Generation of JSON schema from JSON](/docs/README.md#generating-json-schema-from-json)
13. [Automatic error checking after editing](/docs/README.md#automatically-check-for-errors-after-editing)
14. [Select all JSON in a non-JSON file](/docs/README.md#selecting-all-valid-json)
15. [Quickly convert between JSON strings and raw text](/CHANGELOG.md#550---2023-08-13)

[Read the docs.](/docs/README.md)

[View past changes.](/CHANGELOG.md)

![JSON file with syntax errors before and after use of JSON tools](/jsontools%20before%20after.PNG)

## Downloads and Installation ##

Go to the [Releases page](https://github.com/molsonkiko/JsonToolsNppPlugin/releases) to see past releases.

[Download latest 32-bit version](https://github.com/molsonkiko/JsonToolsNppPlugin/raw/main/JsonToolsNppPlugin/Release_x86.zip)

You can unzip the 32-bit download to `.\Program Files (x86)\Notepad++\plugins\JsonTools\JsonTools.dll`.

[Download latest 64-bit version](https://github.com/molsonkiko/JsonToolsNppPlugin/raw/main/JsonToolsNppPlugin/Release_x64.zip)

You can unzip the 64-bit download to `C:\Program Files\Notepad++\plugins\JsonTools\JsonTools.dll`.

Alternatively, you can follow these [installation instructions](https://npp-user-manual.org/docs/plugins/) to install the latest version of the plugin from Notepad++.

## System Requirements ##

Every version of the plugin works on Notepad++ 8.4.1 onward, although [some versions of Notepad++ have problems](#problematic-notepad-versions).

Versions of the plugin from [4.10.0.3](https://github.com/molsonkiko/JsonToolsNppPlugin/commit/e2ffde3a5e529d94f018930dc5ba5e0b077e793c) onward are compatible with older Notepad++ (tested for [7.3.3](https://notepad-plus-plus.org/downloads/v7.3.3/), may be compatible with even older).

Every version up to and including [3.7.2.1](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/main/CHANGELOG.md#3721---2022-10-20) should work natively on Windows 8 or later (note: this is untested), or you must install [.NET Framework 4.0](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net40). Every version beginning with [4.0.0](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/main/CHANGELOG.md#400---2022-10-24) works on [Windows 10 May 2019 update](https://blogs.windows.com/windowsexperience/2019/05/21/how-to-get-the-windows-10-may-2019-update/) or later, or you must install [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48).

### Problematic Notepad++ versions ###

This is *not* a complete list of known issues with JsonTools. For that, see the [issue tracker](https://github.com/molsonkiko/JsonToolsNppPlugin/issues) and the [Changelog](/CHANGELOG.md). This is a list of issues that emerged in recent versions of Notepad++.

* Ctrl+C and Ctrl+X do not work inside of forms for all versions of JsonTools earlier than [v7.0.0](/CHANGELOG.md#700---2024-02-09) on the following Notepad++ versions:
    * Notepad++ 8.6 and 8.6.1
    * Notepad++ 8.6.2 onward, when [the new Notepad++ setting `Enable Copy/Cut Line without selection`](https://npp-user-manual.org/docs/editing/#context-awareness) is turned *off*.
* Using the [Notepad++ find/replace form](https://npp-user-manual.org/docs/searching/#dialog-based-searching) can cause problems with [remembered selections](/docs/README.md#working-with-selections), and may cause issues with [RemesPath queries](/docs/README.md#remespath),*under the following circumstances:*
    * Notepad++ 8.6.3 and 8.6.4, for all versions of JsonTools.
    * Notepad++ 8.6.5, for JsonTools earlier than [v7.1.0](/CHANGELOG.md#710---2024-02-28).

## Alternative tools ##

### Notepad++ ###

This plugin may consume huge amounts of memory when working with really huge JSON files (say 50+ megabytes). As of version `0.3.0`, my [HugeFiles](https://github.com/molsonkiko/HugeFiles) plugin can break a JSON file up into chunks such that every chunk is syntactically valid JSON. This way you don't need to read the entire file into the text editor, and you can look at one chunk at a time. You can also use this plugin to perform simple find/replace operations on the entire file (maybe eventually allowing control over which sections of the file to edit). Finally, the plugin allows a very large JSON file to be broken up into separate syntactically valid JSON files, which may be easier to use.

[JSMinNPP](https://github.com/sunjw/jstoolnpp) is a reasonable alternative, although as of `v5.0.0`, JsonTools has comparable performance and parsing ability.

While not a JSON plugin per se, [PythonScript](https://github.com/bruderstein/PythonScript) enables Notepad++ users to customize the editor with Python scripts. Since Python has an excellent native JSON library, you could easily use this plugin to create custom scripts for working with JSON.

Because Python has good 3rd-party packages for working with [YAML](https://pypi.org/project/PyYAML/) and [TOML](https://pypi.org/project/toml/) (two good alternatives to JSON), PythonScript could potentially be used to allow this package to work with YAML and TOML.

### VSCode ###

[Visual Studio Code](https://code.visualstudio.com/) has native support for JSON with comments. Just go down to where the language is listed in the right side of the taskbar, and select `JSON with Comments` from the drop-down menu that appears at the top of the screen.

VSCode also has a built-in JSON tree viewer and some support for searching for keys and indices in JSON.

VSCode has many useful tools for working with JSON Schema. VSCode's JSON Schema validation is much more robust than what this plugin currently offers. You can also configure the editor to [automatically use a certain JSON schema to validate JSON with a certain file path](https://code.visualstudio.com/Docs/languages/json#_json-schemas-and-settings). For example, you could configure the editor to always parse files with names like `*tweet*.json` with the schema `tweet_schema.json`. *Note: as of version [4.11](/CHANGELOG.md#4110---2023-03-15), a similar feature exists in this plugin.*

The [JSON Tools](https://marketplace.visualstudio.com/items?itemName=eriklynd.json-tools) plugin provides the same pretty-print and minify functionalities as this plugin.

Finally, the [Encode/Decode](https://github.com/mitchdenny/ecdc) plugin allows fast interconversion of YAML and JSON, among other things.

### Emacs ###

Consult [this list](https://github.com/emacs-tw/awesome-emacs). One Emacs plugin, [JSON mode](https://github.com/joshwnj/json-mode), inspired the `Path to current line` feature of this plugin.

### Python ###

Python's standard library [JSON](https://docs.python.org/3/library/json.html) module is excellent, albeit limited to syntactically correct JSON according to the original JSON standard.

Python's package ecosystem is incredibly rich, so I can't possibly list all the useful tools here. Three that I've enjoyed working with are:
1. [Pandas](https://pandas.pydata.org/). *The* Python tool for working with pretty much any kind of data. 'Nuff said.
2. [GenSON](https://github.com/wolverdude/GenSON). A really user-friendly tool for JSON schema generation. Includes a CLI tool for JSON schema generation and a programmatic API. Has some tools for, e.g., merging two schemas, but I haven't used those tools as much.
3. [DeepDiff](https://zepworks.com/deepdiff/current/index.html). This is super cool! It allows fast (and I mean REALLY FAST) comparison of two JSON documents to find how they differ. For instance, DeepDiff would correctly show that
```json
{"a": [1, 2, {"b": [3, 4]}]}
```
and
```json
{"a": [1, 2, {"b": [4, 4]}]}
```
differ in that the first element of `root['a'][2]['b']` was changed from 3 to 4.

### Websites ###

[This website](https://codebeautify.org/jsonviewer) offers (limited) JSON->CSV conversion, pretty-printing (appears to use the same algorithm as me), minifying, JSON->XML conversion, and a pretty good tree viewer.

I expect you could find plenty of other good websites if you did some research.


## Acknowledgments ##

* [Kasper B. Graverson](https://github.com/kbilsted) for creating the [plugin pack](https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net) that this is based on.
* [Bas de Reuver](https://github.com/BdR76) for making the excellent [CSVLint](https://github.com/BdR76/CSVLint) plugin that I've consulted extensively in writing the code.
* [jokedst](https://github.com/jokedst) for making the [CsvQuery plugin](https://github.com/jokedst/CsvQuery) to which I owe the original ideas behind my settings form and my adaptive styling of forms.
* Everyone who took the time to raise issues and help improve the plugin, especially [vinsworldcom](https://github.com/vinsworldcom).
* And of course, Don Ho for creating [Notepad++](https://notepad-plus-plus.org/)!

### Note on how JsonTools sorts strings ###

__*JsonTools sorts strings more or less the same way as other Windows applications like Microsoft Word, while Notepad++ sorts strings based on Unicode code points.*__ This is more intuitive in some cases, and less intuitive in others.

The list below shows which things in JsonTools are case-sensitive and which are case-insensitive.

__Case-*sensitive* sorters:__
* [`sorted` and `sort_by` RemesPath functions](/docs/RemesPath.md#non-vectorized-functions)
* [sort form](/docs/README.md#sort-form) using `Sort method` = `Default`.
* anything else in RemesPath that sorts things, unless specifically noted otherwise.


__Case-*insensitive* sorters:__
* sorting of object keys when the [`sort_keys`](/docs/README.md#sort_keys) global setting is `true`
* sorting of object keys when the `sort_keys` argument to the [`stringify` RemesPath function](/docs/RemesPath.md#non-vectorized-functions) is `true`
* [sort form](/docs/README.md#sort-form) using `Sort method` = `As strings (ignoring case)`

Consider this input: `["1","-2","3","o","P","ö","p"]`

__JsonTools case-*sensitive* order:__

`["1","-2","3","o","ö","p","P"]`

__JsonTools case-*insensitive* order:__

`["1","-2","3","o","ö","P","p"]` (the order of the `P` and the `p` is unstable)

__Notepad++ case-*sensitive* order:__

`["-2","1","3","P","o","p","ö"]`

__Notepad++ case-*insensitive* order:__

`["-2","1","3","o","P","p","ö"]` (the order of the `P` and the `p` is unstable)

A summary of some major differences between Notepad++ and JsonTools in string sorting:
1. The sort form ignores the leading minus sign when ordering the numbers; Notepad++ does not.
2. The sort form orders `ö` between `o` and `p` (because culturally that makes sense), but Notepad++ puts `ö` last, because it compares the strings by Unicode code points, and non-ASCII characters like `ö` come after all ASCII characters.
3. In case-*sensitive* sorts, JsonTools puts *upper-case letters after lower-case letters*, but Notepad++ does the opposite.
4. In all sorts, JsonTools respects alphabetical order (e.g., `P` comes after `o` whether case-sensitive or not), but Notepad++ puts *all* upper-case letters before *all* lower-case letters when in case-sensitive mode.

There are *many, many rules for string comparison* (and I know very few of them), and I cannot possibly cover them all here. But hopefully this warning will help you not get caught off guard.