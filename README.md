# JsonToolsNppPlugin

[![Continuous Integration](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/workflows/CI_build.yml/badge.svg)](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/workflows/CI_build.yml)

Miscellaneous tools for working with JSON in Notepad++.

Any issues, feel free to email me at mjolsonsfca@gmail.com.

## Features ##
1. [Pretty-print JSON](/docs/README.md#prettyprintstyle) so that it's spread out over multiple lines.
2. [Compress JSON](/docs/README.md#minimalwhitespacecompression) so that it has little or no unnecessary whitespace.
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
4. Get a report of all the syntax errors in the document (`linting` must be active).
5. Open a [drop-down tree view](/docs/README.md#the-basics) of the document. Clicking on a node in the tree navigates to the corresponding line in the document.
6. Query and edit JSON with the [RemesPath](/docs/RemesPath.md) query language.
7. Parse [JSON Lines](/docs/README.md#json-lines-documents) documents.

[Read the docs.](/docs/README.md)

[View past changes.](/CHANGELOG.md)

![JSON file with syntax errors before and after use of JSON tools](/jsontools%20before%20after.PNG)

## Downloads and Installation ##

Go to the [Releases page](https://github.com/molsonkiko/JsonToolsNppPlugin/releases) to see past releases.

[Download latest 32-bit version](https://github.com/molsonkiko/JsonToolsNppPlugin/raw/main/JsonToolsNppPlugin/Release_x86.zip)

You can unzip the 32-bit download to `.\Program Files (x86)\Notepad++\plugins\JsonTools\JsonTools.dll`.

[Download latest 64-bit version](https://github.com/molsonkiko/JsonToolsNppPlugin/raw/main/JsonToolsNppPlugin/Release_x64.zip)

You can unzip the 64-bit download to `C:\Program Files\Notepad++\plugins\JsonTools\JsonTools.dll`.

## Acknowledgments ##

* [Kasper B. Graverson](https://github.com/kbilsted) for creating the [plugin pack](https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net) that this is based on.
* [Bas de Reuver](https://github.com/BdR76) for making the excellent [CSVLint](https://github.com/BdR76/CSVLint) plugin that I've consulted extensively in writing the code.
* And of course, Don Ho for creating [Notepad++](https://notepad-plus-plus.org/)!