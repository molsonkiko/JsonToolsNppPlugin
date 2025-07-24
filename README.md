# JsonToolsNppPlugin (big integer version)

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
5. [Get the path to the current position](/docs/README.md#path-to-current-position)
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

[Download latest 32-bit version](https://github.com/molsonkiko/JsonToolsNppPlugin/raw/big_integer_for_Dtype_INT/JsonToolsNppPlugin/Release_x86.zip)

You can unzip the 32-bit download to `.\Program Files (x86)\Notepad++\plugins\JsonTools\JsonTools.dll`.

[Download latest 64-bit version](https://github.com/molsonkiko/JsonToolsNppPlugin/raw/big_integer_for_Dtype_INT/JsonToolsNppPlugin/Release_x64.zip)

You can unzip the 64-bit download to `C:\Program Files\Notepad++\plugins\JsonTools\JsonTools.dll`.

Alternatively, you can follow these [installation instructions](https://npp-user-manual.org/docs/plugins/) to install the latest version of the plugin from Notepad++.

### Downloading unreleased versions ###

You can also download recently committed but unreleased versions of JsonTools by downloading the appropriate GitHub artifact in the following way:
1. Go to the [commit history](https://github.com/molsonkiko/JsonToolsNppPlugin/commits/big_integer_for_Dtype_INT/) of JsonTools.
2. Most commits will have a green checkmark and the text `4/4` next to their commit message. Click on it.
3. A dropdown menu showing the CI tasks will appear. Click on one of the `Details` links. [Here's an example of a page that this leads to.](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/runs/9767448644/job/26962739908).
4. Click the `Summary` link near the top-left corner of the page. [Here's an example of the page this leads to.](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/runs/9767448644)
5. If you chose a commit that was made in the last 90 days, at the bottom of this page you will find links to download `plugin_dll_x64` (a zip archive containing 64-bit `JsonTools.dll`) or `plugin_dll_x86` (a zip archive containing 32-bit `JsonTools.dll`). Download the appropriate binary for your current Notepad++ installation.
6. Unzip the zip archive you downloaded into a folder.
7. Close Notepad++, if it was open, because you can't modify the JsonTools DLL while it's in use.
8. Copy the `JsonTools.dll` inside the extracted folder into the `Notepad++\plugins\JsonTools` folder under your Notepad++ installation, overwriting your old version of JsonTools (or rename the old version so that you can switch back to it)

If you also want to download the most recent [translation to another language](#translating-jsontools-to-another-language), you will need to also download the most up-to-date translation file for that language from [the `translation` folder of this repo](https://github.com/molsonkiko/JsonToolsNppPlugin/tree/big_integer_for_Dtype_INT/translation). To do that:
1. Click on one of the files in the list.
2. Click the download icon near the top of the page.
3. Put the downloaded raw `{yourLanguage}.json5` file into the `translation` folder of your JsonTools plugin directory, as discussed in [the documentation on translating JsonTools](#translating-jsontools-to-another-language).

### Alternate 64-bit integer version ###

This is not the [main version of JsonTools](https://github.com/molsonkiko/JsonToolsNppPlugin/) available on the plugin manager. This version sacrifices performance to allow the parsing of arbitrarily large integers.

## System Requirements ##

Every version of the plugin works on Notepad++ 8.4.1 onward, although [some versions of Notepad++ have problems](#problematic-notepad-versions).

Versions of the plugin from [4.10.0.3](https://github.com/molsonkiko/JsonToolsNppPlugin/commit/e2ffde3a5e529d94f018930dc5ba5e0b077e793c) onward are compatible with older Notepad++ (tested for [7.3.3](https://notepad-plus-plus.org/downloads/v7.3.3/), may be compatible with even older).

Every version up to and including [3.7.2.1](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/CHANGELOG.md#3721---2022-10-20) should work natively on Windows 8 or later (note: this is untested), or you must install [.NET Framework 4.0](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net40). Every version beginning with [4.0.0](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/CHANGELOG.md#400---2022-10-24) works on [Windows 10 May 2019 update](https://blogs.windows.com/windowsexperience/2019/05/21/how-to-get-the-windows-10-may-2019-update/) or later, or you must install [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48).

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

[JSMinNPP](https://github.com/sunjw/jstoolnpp) is a reasonable alternative, although as of `v5.0.0`, JsonTools has comparable parsing ability. This version of JsonTools has worse performance, but the main version has comparable performance.

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

## Translating JsonTools to another language ##

If you are interested in helping users of JsonTools who don't speak English, JsonTools can be translated to other languages beginning in [v8.0](/CHANGELOG.md#800---2024-06-29).

JsonTools infers your preferred language and attempts to translate in the following way:
1. JsonTools checks your [Notepad++ `nativeLang.xml` config file](https://npp-user-manual.org/docs/binary-translation/#creating-or-editing-a-translation) (at [XPath path](https://www.w3schools.com/xml/xml_xpath.asp) `/NotepadPlus/Native-Langue/@name`) to determine what language you prefer to use, and sets `lowerEnglishName` to the appropriate value. For example, if this file says `galician`, we will attempt to translate JsonTools to `galician`.
2. JsonTools then does one of the following:
    - If `lowerEnglishName` is `english`, it does nothing (because JsonTools is naturally in English)
    - Otherwise, it looks in the `translation` subdirectory of the `JsonTools` plugin folder (where `JsonTools.dll` lives) for a file named `{lowerEnglishName}.json5`
    - __NOTE:__ because the translation files are in a subdirectory of the plugin folder, *translation does not work for versions of Notepad++ older than [version 8](https://notepad-plus-plus.org/downloads/v8/),* since those older versions do not have separate folders for each plugin.
3. If JsonTools found `translation\{lowerEnglishName}.json5`, it attempts to parse the file. If parsing fails, a message box will appear warning the user of this.
    - If no translation file was found, or if parsing failed, the default English will be used.
4. If parsing was successful, JsonTools will use the translation file as described below.

JsonTools only attempts to find translation files once, when Notepad++ is starting up. If you change the UI language of Notepad++, you will have to close Notepad++ and reopen it before this change will apply to JsonTools.

If you decide that you don't like the translation and prefer the original English, I recommend appending `_disabled` to the name of the translation file. For example, if your translation was based on `german.json5`, renaming that file to `german_disabled.json5` would cause JsonTools to default to English when the Notepad++ UI was in German.

To be clear, *JsonTools may not be in the same language of the Notepad++ UI.* The steps described above represent my best effort to automatically translate JsonTools into a language that the user will find useful, without requiring the user to select their language from a list of available languages in the settings form.

To translate JsonTools to another language, just look at [`english.json5` in the translations directory of this repo](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/english.json5) and follow the instructions in that file.

Currently JsonTools has been translated into the following languages:
| Language | First version with translation | Translator(s) | Translator is native speaker?  |
|----------|--------------------------------|---------------|--------------------------------|
| [Italian](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/italian.json5)  |   [v8.0](/CHANGELOG.md#800---2024-06-29) |  [conky77](https://github.com/conky77), [molsonkiko](https://github.com/molsonkiko) (only for `messageBoxes` and `fileComments` sections)  | Only conky77 |
| [Arabic](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/arabic.json5)  |   [v8.4](/CHANGELOG.md#840---2025-05-04) |  [hydy100](https://github.com/hydy100) | Yes |
| [Chinese (simplified)](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/chineseSimplified.json5)  |   [v8.4](/CHANGELOG.md#840---2025-05-04) |  [hydy100](https://github.com/hydy100) | Yes |
| [French](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/french.json5)  |   [v8.4](/CHANGELOG.md#840---2025-05-04) |  [hydy100](https://github.com/hydy100) | Yes |
| [German](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/german.json5)  |   [v8.4](/CHANGELOG.md#840---2025-05-04) |  [hydy100](https://github.com/hydy100) | Yes |
| [Japanese](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/japanese.json5)  |   [v8.4](/CHANGELOG.md#840---2025-05-04) |  [hydy100](https://github.com/hydy100) | Yes |
| [Korean](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/korean.json5)  |   [v8.4](/CHANGELOG.md#840---2025-05-04) |  [hydy100](https://github.com/hydy100) | Yes |
| [Taiwanese Mandarin](https://github.com/molsonkiko/JsonToolsNppPlugin/blob/big_integer_for_Dtype_INT/translation/taiwaneseMandarin.json5)  |   [v8.4](/CHANGELOG.md#840---2025-05-04) |  [hydy100](https://github.com/hydy100) | Yes |


The following aspects of JsonTools __can__ be translated:
- Forms (including all controls and items in drop-down menus) (see the `forms` field of the translation `json5` file)
- Items in the JsonTools sub-menu of the Notepad++ Plugins menu (see the `menuItems` field)
- The descriptions of settings in the [`JsonTools.ini` config file](/docs/README.md#customizing-settings) (see the `settingsDescriptions` field)
- The descriptions of settings in the [settings form](/docs/README.md#customizing-settings) (*only for versions since [v8.1](/CHANGELOG.md#810---2024-08-23)*) (also controlled by `settingsDescriptions`)
- [JSON syntax errors and JSON schema validation errors](/docs/README.md#error-form-and-status-bar) (*only for versions since [v8.1](/CHANGELOG.md#810---2024-08-23)*) (see the `jsonLint` field)
- Message boxes (includes warnings, errors, requests for confirmation) (*only for versions since [v8.1](/CHANGELOG.md#810---2024-08-23)*) (see the `messageBoxes` field)

The following aspects of JsonTools __may eventually__ be translated:
- This documentation
- Generic modal dialogs (for example, file-opening dialogs, directory selection dialogs)
- Error messsages (other than JSON syntax errors, which are already translated)

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