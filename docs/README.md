JSON Tools Overview
====================

This documentation will walk you through a typical use case of this application.

Consider the following JSON.
```json
[
    {
    "cities": "BUS", "contaminated": true,
    "date": "", "names": "Bluds",
    "nums": NaN, "subzone": "a", "zone": 1
    },
    {
    "cities": "BUS", "contaminated": false,
    "date": "", "names": "Bluds",
    "nums": NaN, "subzone": "c", "zone": 1
    },
    {
    "cities": "FUDG", "contaminated": true,
    "date": "2020-12-13 12:00:00.00",
    "names": "dfsd", "nums": 0.5,
    "subzone": "c", "zone": 2
    },
    {
    "cities": "FUDG", "contaminated": false,
    "date": "2020-12-13 12:00:00.00",
    "names": "dfsd", "nums": 0.5,
    "subzone": "e", "zone": 2
    },
    {
    "cities": "YUNOB", "contaminated": true,
    "date": "2014-10-17 12:00:00.00",
    "names": "Kjond", "nums": 4.6,
    "subzone": "w", "zone": 5
    }
]
```

## The Basics ##
Let's open up this JSON file.

![JSON document before using JsonTools](/docs/json%20document%20initial.PNG)

You might want to reformat this document so it doesn't have so much whitespace. Use the "Compress current JSON file" plugin command (`Ctrl+Alt+Shift+C`) to reformat it like so:

![JSON reformatted with minimal whitespace](/docs/json%20document%20after%20compression.PNG)

If you prefer it the way it was before, you can use the "Pretty-print current JSON file" (`Ctrl+Alt+Shift+P`) to restore it to that formatting.

You can change the way this plugin compresses and pretty-prints JSON in the settings. See the [JSON formatting](#json-formatting) section below.

We can open up the JSON tree viewer in the main menu by navigating Plugins -> JsonTools -> "Open JSON tree viewer". The keyboard shortcut `Ctrl+Alt+Shift+J` will also open the tree.

![JSON tree view immediately after creation](/docs/tree%20first%20view.PNG)

You can click on the nodes in that tree to see the children. When you select a node, the caret will snap to the line of the node you've selected.

__NOTES__
1. If you submit a RemesPath query that is anything other than the default `@`, the JSON tree may no longer send the caret to the correct line.
2. If you [edit your JSON](/docs/RemesPath.md#editing-with-assignment-expressions) with RemesPath queries and then undo your change with `Ctrl+Z` or similar, that will not undo the changes to the JSON. To re-sync the JSON with the document, you will have to close and then re-open the tree view.
    - As of version 3.5.0, you can use the `Refresh` button to refresh the tree with the most recently parsed JSON, rather than closing and re-opening the tree.
3. Keyboard shortcuts (*added in v3.5.0*):
    - `Ctrl+Enter` in the query box submits the query.
    - `Enter` while the tree is selected toggles the selected node between expanded/collapsed.
    - Up and down arrow keys can also navigate the tree.
    - `Escape` takes focus from the tree view back to the editor.

If a node has a `+` or `-` sign next to it, you can click on that button to expand the children of the node, as shown here.

![JSON viewer tree partially expanded](/docs/tree%20partially%20expanded.PNG)


You'll notice that icons appear next to the nodes in the tree. They are as follows:
* <span style="color:blue">Blue</span> square braces: __array__
* <span style="color:green">Green</span> curly braces: __object__
* ☯️ (yin-yang, half-black, half-white circle): __boolean__
* <span style="color:red">123</span>: __integer__ (represented by 64-bit integer)
* <span style="color:red">-3.5</span>: __float__ (represented by 64-bit floating point number)
* abc: __string__
* <span style="color:grey">grey</span> square: __null__

## Parser settings ##

By default, this app can parse a superset of JSON that is very slightly more permissive than the [original JSON specification](https://json.org). This app parses `NaN` as the floating point `Not-A-Number` and `Infinity` as the floating point Infinity.

You can change the settings to make the parser more or less inclusive. For example, the original spec doesn't allow strings to be surrounded in single quotes, nor does it allow comments in the file. Thus, such JSON will cause our parser to throw an error.

![The default parser settings don't allow singlequoted strings or comments](/docs/json%20parser%20error%20due%20to%20singlequotes.PNG)

We can fix that in the settings.

![Change the parser settings to allow singlequotes and comments](/docs/json%20parser%20settings%20allow%20singlequotes%20and%20comments.PNG)

As you can see, you can also make the parser settings *stricter* than the default so that they don't accept the nonstandard NaN and Infinity. Just set `allow_nan_inf` to False.

*NOTE: Python-style comments are first supported in version [4.12.0](/CHANGELOG.md#4120---2023-03-28), while JavaScript comments have always been supported.*

## Viewing syntax errors in JSON ##

The `linting` attribute in Settings enables the built-in linter for the JSON parser, which catches various syntax errors in JSON and logs them.
When you parse a document that contains syntax errors like the one we saw above, you'll be asked if you want to see the syntax errors caught by the linter.

![Linter prompt after parsing error-ridden JSON document](/docs/prompt%20to%20view%20lint.PNG)

If you click "Yes", a new file will open in a separate tab containing details on all the syntax errors that were caught.

![Linter syntax error report](/docs/linter%20syntax%20error%20report.PNG)

**NOTE:** The JSON linter allows the parser to continue parsing even when it encounters syntax errors. That means that the parser will parse some documents that are not valid JSON until the syntax errors are corrected.

## Path to current line ##

*Added in version v3.4.0*

The `Path to current line` menu option lets you fill the clipboard with the path to the first node on the current line. This is most helpful when your JSON is already [pretty-printed](/docs/README.md#pretty_print_style) so no two nodes share a line.

![Getting the path to current line](/docs/path%20to%20current%20line.PNG)

### Key style ###

By default, the path clipped is in RemesPath style (dot syntax and backtick-quoted strings). You can get JavaScript style (dot syntax and c-style quoted strings in square brackets) or Python style (c-style quoted strings in square brackets) in the settings.
For example, the different path styles might look like this:
- Remespath (default): ``[`a b`][0].c``
- Python: `['a b'][0]['c']`
- JavaScript: `['a b'][0].c` 

## RemesPath ##

*Added in version 1.2.0*

The primary utility of this tool comes from the ability to query and edit the JSON using [RemesPath](RemesPath.md), which you can learn more about in the linked docs.

You can submit RemesPath queries in textbox above the tree, which by default has the `@` symbol in it.

![JSON viewer tree with RemesPath query](/docs/json%20viewer%20with%20remespath%20query.PNG)

Once you've submitted a query, you can use several other features of the JSON viewer.
First, you can open a new buffer containing the query result.

![JSON viewer open query result in new buffer](/docs/json%20viewer%20query%20save.PNG)

## Find and Replace Form ##

*Added in version 3.7.0*

If you want to perform some simple search or find-and-replace operations on JSON without worrying about [RemesPath](/docs/RemesPath.md) syntax, you can use the find/replace form.

![Find/replace form simple find](/docs/find%20replace%20form%20simple%20find.PNG)

![Find/replace form simple replace](/docs/find%20replace%20form%20simple%20replace.PNG)

This form provides lets you perform text searches on keys and values in JSON, and also lets you do mathematical find/replace operations on numeric values.

The default behavior of the form is to do a regular expression search on both keys and values, or a text find/replace on values only. You can change that under `Show advanced options`.

![Find/replace form math find](/docs/find%20replace%20form%20math%20find.PNG)

![Find/replace form math replace](/docs/find%20replace%20form%20math%20replace.PNG)

Prior to version [4.11.0](/CHANGELOG.md#4110---2023-03-15), if you didn't do a regular expression search, your search term must match keys/values *exactly*. Substring matching of non-regular-expressions was *not* supported.

Starting in version [4.11.0](/CHANGELOG.md#4110---2023-03-15), non-regular-expression searching does not require strings to match exactly. Thus, you can now match the `"MOO"` in the array `["MOO", "ZOO"]` with the search term `M` with regular expressions turned off.

![Find/replace form non-regex search must match exactly before v4.11.0](/docs/find%20replace%20form%20nonregex%20exact.PNG)

The form has limited functionality. For example, you can't perform a search on keys and a replacement on values. However, the form generates RemesPath queries in the RemesPath query box in the tree viewer, so you can use those queries as a starting point.

## JSON to CSV ##

*Added in version 1.2.0*

Some JSON also has a somewhat __tabular__ format, such that it is amenable to conversion to a CSV file. The JSON in this example is a particularly simple case of this.

This app has a [form](/docs/json-to-csv.md) that allows conversion of such JSON to a tabular format. Remember that even if the JSON file as a whole can't be "tabularized" (or *can*, but you don't *want*  to), you can use a RemesPath query to select the part that you want to tabularize.

![JSON to CSV convertor](/docs/json%20viewer%20csv%20generator.PNG)

At present the __Strategy__ option for the CSV Generation form has four options. You can read more about these strategies in the [docs](/docs/json-to-csv.md).

## Changing how much JSON tree is displayed ##

*Added in version 3.1.0*

Loading the full tree for very large, complex JSON can cause tremendous memory consumption and make Notepad++ unresponsive for a long time. Because of this, only the __direct children of the root__ are displayed by default for JSON files larger than 4 megabytes. This is reflected in the `View all subtrees` checkbox. You can change this in the settings.

Beginning in version *4.10.0*, if a JSON array or object has more than `10,000` direct children (congigurable in `Settings->max_json_length_full_tree`), this setting will automatically be activated, and `10,000` evenly spaced top-level children will be displayed. For example, this would mean that an array with 200,000 children would result in a tree view with pointers to the first element, the 20th element, the 40th element, and so on.

Populating the tree is *much* more expensive than parsing JSON or executing RemesPath queries, which means that rather small JSON files with a very large number of nodes (e.g., an array containing 1e5 instances of the number `1`) may take *much* longer to load than larger files with a smaller number of nodes.

![Only direct children of the root are displayed for a big file](/docs/partial%20tree%20load%20example.PNG)

For best performance, you can disable the tree view completely. If the JSON is a single scalar (bool, int, float, string, null, or date), it will display. For arrays and objects, you will only see the type icon.

The `View all subtrees` checkbox on the JSON viewer form allows you to quickly toggle between viewing the full tree and only the direct children. Some notes on the checkbox:
- If the full tree will not be shown when the tree is loaded, this box is unchecked; otherwise it is checked.
- Checking the box when previously unchecked will load the full tree, but the user must click OK at a message box if the document is 2.5 MB or larger or else the box will remain unchecked. This box warns that loading the full tree for a big document could make Notepad++ responsive for a long time.
![Message box warning of unresponsiveness when loading a big document](/docs/full%20tree%20load%20warning%20msg.PNG)
- This message box for canceling loading of the full tree will now also show up when you try to open the full tree for a document 2.5 MB or larger.
- Unchecking the box when the full tree is loaded will cause only the direct children of root to display.
- This box does not change the global settings. It only changes the settings for that tree view.

## Get info about tree nodes ##

*Added in version 3.4.0*

You can right click on a tree node to copy any of the following to the clipboard:
* Value
* Key/index (customizable via [key style](#key-style))
* Path (see [key style](#key-style))

In versions 3.4.0 through 3.6.1.1, you can also click on the `Current path` button beneath the tree to copy the path of the currently selected tree node to the clipboard. The path will have the style of whatever default style you chose in the settings (shown in the adjacent text box). In versions 3.7.0 and above, this button does not exist, so just select and copy the text in the box.

![Current path button for path to current tree node](/docs/path%20to%20current%20tree%20node%20button.PNG)

## JSON formatting ##

*Added in version 3.3.0*

This plugin can print JSON in a variety of different ways, depending on what settings you use.

The formatting settings are as follows:
### indent_pretty_print ###
Changes how much indentation pretty-printed JSON has per level. With the default of __4__, JSON looks like this:
```json
{
    "a": [
        1
    ]
}
```
With indentation __2__, you get this instead:
```json
{
  "a": [
    1
  ]
}
```
### minimal_whitespace_compression ###
The Python convention for formatting JSON results in compressed JSON with a little bit of whitespace, like so:
```json
{"a": [1, 2]}
```
Notice that there's some unnecessary whitespace after items and between keys and values.

With minimal_whitespace_compression, __all__ unnecessary whitespace is removed:
```json
{"a":[1,2]}
```
### pretty_print_style ###
There are many different [styles](http://astyle.sourceforge.net/astyle.html#_style) for pretty-printing JSON that vary in how they indent and where they put braces.

At present, two different styles are supported:

[Google style](http://astyle.sourceforge.net/astyle.html#_style=google)
```json
{
    "a": [
        1,
        [
            2
        ]
    ]
}
```
[Whitesmith style](http://astyle.sourceforge.net/astyle.html#_style=whitesmith)
```json
{
"a":
    [
    1,
        [
        2
        ]
    ]
}
```
### sort_keys ###
Whether to sort the keys of objects alphabetically.

If this is false, the input order of object keys is preserved when formatting.

```json
{"C": 3, "BA": 2, "a": 1, "A": 2, "ba": 4, "c": 4}
```

If this is true, keys are sorted alphabetically like so:
```json
{"A": 2, "a": 1, "ba": 4, "BA": 2, "c": 4, "C": 3}
```
As you can see, the sort is unstable when comparing two keys that differ only in case. You can't rely on the lower-case key being before the upper-case key or vice versa.

## JSON Lines documents ##

*Added in version v3.2.0*

[JSON Lines](https://jsonlines.org/) documents can contain multiple valid JSON documents, provided that each is on its own line and there is exactly one line per document (with an optional empty line after the last).

JSON Lines docs typically have a `.jsonl` file extension, and if a file has that extension, this plugin will try to parse it as a JSON Lines doc.

This is a *valid* JSON Lines document:
```json
["a", "b"]
{"a": 1, "b": 2, "c": [3]}
"d"
1.5
```
This is *invalid*, because there is an empty line in the middle:
```json
["a", "b"]

1
```
This is also *invalid*, because two documents are on the same line:
```json
[1, 2] [3, 4]
```
And this is *invalid* too, because one document spans multiple lines:
```json
[1,
2]
[3, 4]
```

__NOTES:__
- This plugin parses a JSON Lines doc as an array where the `i^th` element is the JSON document on the `i^th` line.
- The `Array to JSON Lines` command (*added in v3.3.0*) on the plugin menu allows you to convert a normal JSON array into a JSON Lines document. 
- If you query a JSON Lines doc with RemesPath, the query result will be formatted as normal JSON.
- If you have a JSON Lines document that doesn't have the `.jsonl` extension, you can use the `Plugins->JsonTools->Parse JSON Lines document` command in the main menu.

## Running tests ##

The plugin contains a variety of built-in unit tests and performance benchmarks that can be run on demand from the `Run Tests` plugin menu command.

This repository also contains ["most recent errors.txt"](/most%20recent%20errors.txt), which shows the expected output (modulo some variation in the benchmarking times) of these tests. By comparing the output of running the tests on your computer to the expected output, you can determine whether the plugin's code is working as expected.

Prior to version [4.2.0](/CHANGELOG.md#420---2022-10-29), running these tests should cause the plugin to crash after printing the line `Testing JSON grepper's file reading ability` because some tests referenced an absolute file path.

# Get JSON from files and APIs #

*Added in version 3.6.0*

Sometimes it is useful to work with many JSON files at a time. For this purpose, we created a tool for *grepping* (searching for certain kinds of files in a directory, possibly recursively) for JSON files in a local directory, and also for sending [REST API](https://www.redhat.com/en/topics/api/what-is-a-rest-api) requests to multiple URLs.

We can open this tool with the `Plugins->JsonTools->Get JSON from files and APIs` menu command or `Ctrl+Alt+Shift+G`.

The tool looks like this:

![JSON grepper/API requester appearance](/docs/json_from_files_and_apis%20initial.PNG)

## Sending REST API requests ##

Perhaps the most useful attribute of this tool is its ability to connect to APIs and extract useful data without the user needing to write a script. Just enter one URL per line in the box on the left. *Added in version [4.11.2](/CHANGELOG.md#4112---2023-03-21): URLs can also be entered as a JSON array.*

**WARNING!!!** Before sending API requests, make sure you understand the correct way to format the URL, what type of JSON you expect to be getting, etc. *This tool has not been tested on private APIs*, so you should expect it to fail unless you can incorporate your API key and other authentication information into the URL.

Here's an example of what you get with successful request(s):

![JSON grepper/API requester successful API requests](/docs/json_from_files_and_apis%20api%20good%20url%20result.PNG)

The URLs of successful requests show up in the box on the right. I used the `View results in buffer` button at the bottom of the form to open the buffer and tree view shown here.

Of course, sometimes an API request will fail. You can click the [View errors button](#viewing-errors) to see any errors that happened.

## Getting JSON from local directories ##

If you want to open up all the JSON files in a directory, look to the bottom center left. There you can customize what type(s) of [filename search pattern](https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefiles?view=net-6.0#system-io-directoryinfo-enumeratefiles) you want to use (by default files with the `.json` extension), choose whether to recursively search in subdirectories (false by default), and finally search for files using the settings you chose.

For every file that the JSON tries and fails to parse, the exception will be caught and saved so you can view it later with the `View errors button`.

![JSON grepper/API requester search JSON in local directories](/docs/json_from_files_and_apis%20get%20json%20in%20directory.PNG)

### Viewing results in a buffer ###

If you want to see the JSON found by this tool, just click the `View results in buffer` button. This will open a new buffer in Notepad++ with an object mapping filenames and URLs to the JSON associated with them.

This form has its own tree viewer associated with this buffer. You can use this plugin's normal tree viewer for other buffers. If you close the buffer, the tree viewer is destroyed.

## Clearing selected files ##

If you like most of the JSON documents you've found but you don't want to keep *all* of the files, you can select some of them, and then click the `Remove selected files` button in the bottom right center.

![JSON grepper/API requester remove selected files button BEFORE removal](/docs/json_from_files_and_apis%20remove%20selected%20files%20BEFORE.PNG)

After you click the button, those JSON documents will be removed, and the buffer and tree view will update to reflect this.

![JSON grepper/API requester remove selected files button AFTER removal](/docs/json_from_files_and_apis%20remove%20selected%20files%20AFTER.PNG)

## Viewing errors ##

Click the `View errors` button to see if any errors happened. If any did, a new buffer will happen with an object mapping from filenames and urls to the associated exception.

![JSON grepper/API requester error report](/docs/json_from_files_and_apis%20error%20report.PNG)

## Validating JSON against JSON schema ##

As of version *4.6.0*, the plugin can validate JSON against a [JSON schema](https://json-schema.org/). If the schema is valid, a message box will tell you if your JSON validates. If it doesn't validate, the plugin will tell you the first location where validation failed.

As of version [4.11.2](/CHANGELOG.md#4112---2023-03-21), the recursion limit for validation is currently 64. Deeper JSON than that can't be validated, period. Very deep or recursive schemas will still compile.

This tool can only validate the following keywords:

### Keywords for all JSON
* type
* [anyOf](https://json-schema.org/draft/2020-12/json-schema-core.html#name-anyof)
* [enum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-enum)
* [`definitions`, `$defs`, and `$ref`](https://json-schema.org/draft/2020-12/json-schema-core.html#name-schema-re-use-with-defs)
    * __Notes:__
    * support added in version [4.11.2](/CHANGELOG.md#4112---2023-03-21)
    * `definitions` and `$defs` keywords are equivalent.

### Keywords for objects
* [properties](https://json-schema.org/draft/2020-12/json-schema-core.html#name-properties)
* [required](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-required)
* [patternProperties](https://json-schema.org/draft/2020-12/json-schema-core.html#name-patternproperties) (*added in version 4.8.2*)

### Keywords for arrays
* [items](https://json-schema.org/draft/2020-12/json-schema-core.html#name-items)
* [minItems](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-minitems)
* [maxItems](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-maxitems)

### Keywords for strings

* [pattern](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-pattern) (*support added in version [4.11.2](/CHANGELOG.md#4112---2023-03-21)*)


### Keywords for numbers
* [minimum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-minimum) and [maximum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-maximum) (*Both added in version [4.12.0](/CHANGELOG.md#4120---2023-03-28)*)

![Example of successful JSON schema validation](/docs/json%20schema%20validation%20succeeded.PNG)

![Example of failed JSON schema validation](/docs/json%20schema%20validation%20failed.PNG)

## Generating random JSON from a schema ##

The plugin can also generate random JSON from a schema. The default minimum and maximum array lengths (for schemas where the `minItems` and `maxItems` keywords are omitted) are `0` and `10` respectively.

*Added in version 4.8.1:* You can also use a non-schema file to generate random JSON. A schema will be generated on the fly, and that schema will be used to make the random JSON.

![randomly generated JSON from a schema](/docs/random%20json%20from%20schema.PNG)

The following keywords are supported for random JSON generation:

### Keywords for all JSON
* type
* [anyOf](https://json-schema.org/draft/2020-12/json-schema-core.html#name-anyof)
* [enum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-enum)
* [`definitions`, `$defs`, and `$ref`](https://json-schema.org/draft/2020-12/json-schema-core.html#name-schema-re-use-with-defs)
    * __Notes:__
    * support added in version [4.11.2](/CHANGELOG.md#4112---2023-03-21)
    * `definitions` and `$defs` keywords are equivalent.

### Keywords for objects
* [properties](https://json-schema.org/draft/2020-12/json-schema-core.html#name-properties)
* [required](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-required)

### Keywords for arrays
* [items](https://json-schema.org/draft/2020-12/json-schema-core.html#name-items)
* [minItems](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-minitems)
* [maxItems](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-maxitems)
* [contains](https://json-schema.org/draft/2020-12/json-schema-core.html#name-contains)
* [minContains](https://json-schema.org/draft/2020-12/json-schema-core.html#name-contains)
* [maxContains](https://json-schema.org/draft/2020-12/json-schema-core.html#name-contains)

### Keywords for numbers
* [minimum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-minimum) and [maximum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-maximum) (*Both added in version [4.12.0](/CHANGELOG.md#4120---2023-03-28)*)

## Generating JSON schema from JSON ##

You can also generate a [JSON schema](https://json-schema.org/) from a JSON document.

This JSON schema generator only produces schemas with the following keywords:

### Keywords for all JSON
* type
* [anyOf](https://json-schema.org/draft/2020-12/json-schema-core.html#name-anyof)
* [enum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-enum)

### Keywords for objects
* [properties](https://json-schema.org/draft/2020-12/json-schema-core.html#name-properties)
* [required](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-required)

### Keywords for arrays
* [items](https://json-schema.org/draft/2020-12/json-schema-core.html#name-items)

![JSON schema generator](/docs/json%20viewer%20schema%20generator.PNG)

## Automatic validation of JSON against JSON schema ##

As of version [4.11.0](/CHANGELOG.md#4110---2023-03-15), you can set up this plugin to *automatically validate* JSON files with certain filenames whenever you open them. (*starting in version [4.11.2](/CHANGELOG.md#4112---2023-03-21), auto-validation also occurs when files are saved or renamed*)

Let's try out this feature! We can use the plugin command `Choose schemas to automatically validate filename patterns`, which will open up a file that looks like this.

![schemas to filename patterns file new](/docs/schemasToFnamePatterns%20with%20no%20fname%20patterns.PNG)

Read the comments at the beginning of this file; it's self-documenting.

Now let's map a schema file to a filename pattern.

![schemas to filename patterns file successful validation](/docs/schemasToFnamePatterns%20example%20schema%20to%20example%20fname%20success.PNG)

This configuration means that whenever I open a `.json` file with the substring `example` in its name (other than `example_schema.json` itself), the file will automatically be validated against `example_schema.json` (shown in the left of the bottom instance). We see in the right tab in the bottom instance that `example.json` does not have a pop-up message, because it validates against that schema.

Below we can see an example of what happens when a file with a name that matches the pattern *does not validate* under the schema. We get a pop-up message indicating that the schema expected this JSON to be an array, but the JSON in this file is an object.

![schemas to filenames patterns file failed validation](/docs/schemasToFnamePatterns%20example%20schema%20to%20example%20fname%20failure.PNG)

*Note*: the first release where this feature was implemented without causing potential crashes at startup is [4.11.1](/CHANGELOG.md#4111---2023-03-17).

## DSON ##

JSON is not sufficiently [Doge-friendly](https://dogeon.xyz/index.html). This plugin aims to help correct that.

Currently the plugin only has a DSON emitter. Later I may add a DSON parser.

![DSON example](/docs/DSON%20example.PNG)

Where is DSON generator? Don't know. Not on main plugin menu. So scared. Very confuse. 🐕🤷

DSON is first found in version [4.10.1](/CHANGELOG.md#4101---2023-03-02). molsonkiko such dedicated, much commitment to users, wow.