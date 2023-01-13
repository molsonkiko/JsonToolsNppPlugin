# JsonToolsNppPlugin

[![Continuous Integration](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/workflows/CI_build.yml/badge.svg)](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/workflows/CI_build.yml)

Miscellaneous tools for working with JSON in Notepad++.

Any issues, feel free to email me at mjolsonsfca@gmail.com or raise an [issue](https://github.com/molsonkiko/JsonToolsNppPlugin/issues) on GitHub. Please be sure to include diagnostic information about your system and plugin version.

## Features ##
1. [Pretty-print JSON](/docs/README.md#pretty_print_style) so that it's spread out over multiple lines.
2. [Compress JSON](/docs/README.md#minimal_whitespace_compression) so that it has little or no unnecessary whitespace.
3. Change the settings to [enable the parsing of documents that have various syntax errors](/docs/README.md#parser-settings) (using the `linting` setting in settings):
    * string literals containing newlines
    * string literals enclosed by ' instead of "
    * unterminated string literals (lint shows location of starting quote)
    * invalidly escaped characters in strings
    * numbers with two decimal points
    * multiple consecutive commas in an array or object
    * commas before first or after last element in array or object
    * two array or object elements not separated by a comma
    * no colon between key and value in object
    * non-string key in object
    * `]` closing an object or `}` closing an array
    * Unterminated arrays and objects (e.g. `{"a": [1`)
4. Get a report of all the syntax errors in the document (`linting` must be active).
5. Open a [drop-down tree view](/docs/README.md#the-basics) of the document. [Selecting a node](/docs/README.md#get-info-about-tree-nodes) in the tree navigates to the corresponding line in the document.
6. [Get the path to the current line](/docs/README.md#path-to-current-line)
7. Query and edit JSON with a [find/replace form](/docs/README.md#find-and-replace-form) and the [RemesPath](/docs/RemesPath.md) query language.
8. Parse [JSON Lines](/docs/README.md#json-lines-documents) documents.
9. [A form for gettting JSON from APIs or many different local files](/docs/README.md#get-json-from-files-and-apis).
10. [JSON schema validation](/docs/README.md#validating-json-against-json-schema)
11. [Generation of random JSON](/docs/README.md#generating-random-json-from-a-schema)
12. [Generation of JSON schema from JSON](/docs/README.md#generating-json-schema-from-json)

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

Every version of the plugin works on Notepad++ 8.4.1 onward.

Every version up to and including [3.7.2.1](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/main/CHANGELOG.md#3721---2022-10-20) should work natively on Windows 8 or later (note: this is untested), or you must install [.NET Framework 4.0](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net40). Every version beginning with [4.0.0](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/main/CHANGELOG.md#400---2022-10-24) works on [Windows 10 May 2019 update](https://blogs.windows.com/windowsexperience/2019/05/21/how-to-get-the-windows-10-may-2019-update/) or later, or you must install [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48).

## Alternative tools ##

### Notepad++ ###

[JSMinNPP](https://github.com/sunjw/jstoolnpp) is a good plugin with prettier pretty-printing and a much faster tree viewer. This plugin also features JavaScript minification.

While not a JSON plugin per se, [PythonScript](https://github.com/bruderstein/PythonScript) enables Notepad++ users to customize the editor with Python scripts. Since Python has an excellent native JSON library, you could easily use this plugin to create custom scripts for working with JSON.

Because Python has good 3rd-party packages for working with [YAML](https://pypi.org/project/PyYAML/) and [TOML](https://pypi.org/project/toml/) (two good alternatives to JSON), PythonScript could potentially be used to allow this package to work with YAML and TOML.

### VSCode ###

[Visual Studio Code](https://code.visualstudio.com/) has native support for JSON with comments. Just go down to where the language is listed in the right side of the taskbar, and select `JSON with Comments` from the drop-down menu that appears at the top of the screen.

VSCode also has a built-in JSON tree viewer and some support for searching for keys and indices in JSON.

VSCode has many useful tools for working with JSON Schema. VSCode's JSON Schema validation is much more robust than what this plugin offers. You can also configure the editor to [automatically use a certain JSON schema to validate JSON with a certain file path](https://code.visualstudio.com/Docs/languages/json#_json-schemas-and-settings). For example, you could configure the editor to always parse files with names like `*tweet*.json` with the schema `tweet_schema.json`.

The [JSON Tools](https://marketplace.visualstudio.com/items?itemName=eriklynd.json-tools) plugin provides the same pretty-print and minify functionalities as this plugin.

Finally, the [Encode/Decode](https://github.com/mitchdenny/ecdc) plugin allows fast interconversion of YAML and JSON, among other things.

### Emacs ###

Consult [this list](https://github.com/emacs-tw/awesome-emacs). One Emacs plugin, [JSON mode](https://github.com/joshwnj/json-mode) inspired the `Path to current line` feature of this plugin.

### Websites ###

[This website](https://codebeautify.org/jsonviewer) offers (limited) JSON->CSV conversion, pretty-printing (appears to use the same algorithm as me), minifying, JSON->XML conversion, schema validation, and a pretty good tree viewer.

I expect you could find plenty of other good websites if you did some research.


## Acknowledgments ##

* [Kasper B. Graverson](https://github.com/kbilsted) for creating the [plugin pack](https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net) that this is based on.
* [Bas de Reuver](https://github.com/BdR76) for making the excellent [CSVLint](https://github.com/BdR76/CSVLint) plugin that I've consulted extensively in writing the code.
* And of course, Don Ho for creating [Notepad++](https://notepad-plus-plus.org/)!