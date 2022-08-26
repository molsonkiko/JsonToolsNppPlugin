# JsonToolsNppPlugin

[![Continuous Integration](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/workflows/CI_build.yml/badge.svg)](https://github.com/molsonkiko/JsonToolsNppPlugin/actions/workflows/CI_build.yml)

Miscellaneous tools for working with JSON in Notepad++.

Any issues, feel free to email me at mjolsonsfca@gmail.com.

## Features ##
1. Pretty-print JSON so that it's spread out over multiple lines.
2. Compress JSON so that it has very little unnecessary whitespace.
3. Change the settings to enable the parsing of documents that have various syntax errors (using the `linting` setting in settings):
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

[Read the docs.](/docs/README.md) Many of the features in the docs are not yet implemented, though.

[View past changes.](/CHANGELOG.md)

![JSON file with syntax errors before and after use of JSON tools](/jsontools%20before%20after.PNG)

## Downloads ##

Go to the [Releases page](https://github.com/molsonkiko/JsonToolsNppPlugin/releases) to see past releases.

[Download latest 32-bit version](https://github.com/molsonkiko/JsonToolsNppPlugin/raw/main/JsonToolsNppPlugin/Release_x86.zip)

[Download latest 64-bit version](https://github.com/molsonkiko/JsonToolsNppPlugin/raw/main/JsonToolsNppPlugin/Release_x64.zip)