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

You can click on the nodes in that tree to see the children. When you select a node, the caret will snap to the line of the node you've selected. *New in [version 5](/CHANGELOG.md#500---2023-05-26): snaps to position instead.*

__NOTES__
1. __*JsonTools only works with UTF-8 encoded JSON.*__
2. If you submit a RemesPath query that is anything other than the default `@`, the JSON tree may no longer send the caret to the correct position.
3. If you [edit your JSON](/docs/RemesPath.md#editing-with-assignment-expressions) with RemesPath queries *and then undo your change with `Ctrl+Z` or similar, the undo action will not undo the changes to the JSON*. To re-sync the JSON with the document, you will have to close and then re-open the tree view.
    - As of version 3.5.0, you can use the `Refresh` button to refresh the tree with the most recently parsed JSON, rather than closing and re-opening the tree.
4. Keyboard shortcuts (*added in v3.5.0*):
    - `Ctrl+Enter` in the query box submits the query.
    - `Enter` while the tree is selected toggles the selected node between expanded/collapsed.
    - Up and down arrow keys can also navigate the tree.
    - `Ctrl+Up` while in the tree selects the parent of the currently selected node. *Added in [v6.0](/CHANGELOG.md#600---2023-12-13).*
    - `Ctrl+Down` while in the tree selects the last direct child of the currently selected node. *Added in [v6.0](/CHANGELOG.md#600---2023-12-13).*
    - `Escape` takes focus from the tree view back to the editor.
5. Beginning in [v4.4.0](/CHANGELOG.md#440---2022-11-23), you can have multiple tree views open.

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

Starting in [v5.0.0](/CHANGELOG.md#500---2023-05-26), the JSON parser can always parse any document with any allowed syntax errors, such as singleuqoted keys, comments, missing commas, and so forth.

Error reporting can be customized with the `logger_level` setting, which has 5 levels, each a superset of the previous:
1. __STRICT__: Parse only JSON that complies with the original JSON spec.
2. __OK__: Anything allowed with `STRICT`, plus unescaped control characters (e.g., `\t`, `\f`) in strings.
3. __NAN_INF__: Everything at the `OK` level, plus the `NaN`, `Infinity`, and `-Infinity` literals.
4. __JSONC__: Everything in the `NAN_INF` level is allowed, as well as JavaScript `//` and `/*...*/` comments.
5. __JSON5__: Everything in the `JSONC` level is allowed, as well as the following:
    * singlequoted strings
    * commas after the last element of an array or object
    * unquoted object keys
    * see https://json5.org/ for more.
* There are two other states that *cannot be chosen* for `logger_level`, because they *always* lead to errors being logged.
5. __BAD__: Everything on the `JSON5` level is allowed, as well as the following:
    * Python-style '#' comments
    * Python constants `None`, *`nan`, and `inf` (starting in [5.4.0](/CHANGELOG.md#540---2023-07-04))*.
    * missing commas between array members
    * missing ']' or '}' at the ends of arrays and objects (supported for a long time, but *JsonTools got much better at this beginning in [v6.0](/CHANGELOG.md#600---2023-12-13), allowing proper handling of e.g. `[{"a": 1, "b": "a", {"a": 2, "b": "b"}]`*)
    * a bunch of other common syntax errors
6. __FATAL__: These errors always cause *immediate failure* of parsing. Examples include:
    * unquoted string literals other than `true`, `false`, `null`, `NaN`, `Infinity`, `None`, `True`, `False`, `nan`, `inf` and `undefined`.
    * Something other than a JavaScript comment after `/`

When you parse a document that contains syntax errors, you may be asked if you want to see the syntax errors caught by the linter. Starting in [v5.1.0](/CHANGELOG.md#510---2023-06-02), this prompt can be suppressed with the `offer_to_show_lint` setting.

![Linter prompt after parsing error-ridden JSON document](/docs/prompt%20to%20view%20lint.PNG)

In [v5.3.0](/CHANGELOG.md#530---2023-06-10), a form was added to display errors. Prior to that, errors were shown as text in a new buffer.

Beginning in [v7.1](/CHANGELOG.md#710---2024-02-28), if there is a fatal error such that JsonTools cannot parse a document, the caret is moved to the location of the fatal error.

### Document type box *(added in v6.0)* ###

*Beginning in version [v6.0](/CHANGELOG.md#600---2023-12-13),* the tree view has a document type box just above the tree itself.

This box has four options (auto):
* `JSON mode`: parse document (or each selection) as JSON
* `JSONL mode`: parse document (or each selection) as [JSON lines](#json-lines-documents)
* `INI mode`: parse document (or each selection) as an [INI file](#parsing-ini-files)
* `REGEX mode`: the document (or each selection) is converted to a JSON string containing its text, which can then be [searched and edited with regex and RemesPath](#regex-search-form).

Observe the three images below to see how the selected box causes the same document to be interpreted in three different ways (`INI mode` not shown, because that's not a valid INI file).

![Document type box - JSON mode example](/docs/document%20type%20box%20example%20-%20JSON%20mode.PNG)

![Document type box - JSON Lines mode example](/docs/document%20type%20box%20example%20-%20JSONL%20mode.PNG)

![Document type box - REGEX mode example](/docs/document%20type%20box%20example%20-%20REGEX%20mode.PNG)

### Working with selections ###

[Starting in version v5.5](/CHANGELOG.md#550---2023-08-13), you can work with one or more selections rather than treating the entire file as JSON.

Let's see how this works with an example log file.
```
Error 1:    [1,2,3]
Warning 2:  {"a":3}
Info 3:     [[6,{"b":false}],-7]
```
We can make a rectangular selection:

![Make rectangular selection of JSON parts of log file lines](/docs/multi%20selections%20logfile%20make%20rectangular%20selection.PNG)

Now we can pretty-print the JSON in these selections. This has no effect on the document outside the selections.

![Pretty-print multiple JSON selections](/docs/multi%20selections%20pretty%20print.PNG)

__Note that your JSON selections (or lack thereof) are only remembered until you do one of the following:__
* overwrite or delete all the text in the file
* perform a JsonTools action while you have any multi-character selection other than the remembered selections.
    - *NOTE: starting in [v5.7](/CHANGELOG.md#570---2023-09-08), only multi-character selections that begin with a parse-able JSON document will cause the previous selections/lack thereof to be forgotten.*
    - For example, if you had the text `foo` selected, any version since 5.7 would ignore that selection because it does not begin with a valid JSON document.
    - However, the selection `[ blah` *would override old selections even though it's not valid JSON* because the JSON parser will parse it as an unterminated empty array.
* For JsonTools *earlier than [v7.1](/CHANGELOG.md#710---2024-02-28)*:
    - doing a Notepad++ undo/redo action (Ctrl+Z or Ctrl+Y with default keybindings)
    - performing any edit to the document when the number of remembered selections is greater than `max_tracked_json_selections`
* For JsonTools *[v7.1](/CHANGELOG.md#710---2024-02-28) or later*, undoing or redoing a plugin action will still cause remembered selections to be forgotten.

You can move the cursor around, insert and delete characters, and the plugin will move or change the JSON selections accordingly.

For a demo, let's try inserting some more text. We can open the treeview afterwards to see that our changes have been incorporated.

![JSON selections automatically adjust to inserted and deleted text](/docs/multi%20selections%20insert%20delete%20text.PNG)

Also observe the way the treeview is structured. When a document has one or more selections, the JSON is internally represented as a map from `selectionStart,selectionEnd` strings to the JSON in each of those selections.

We can perform RemesPath queries on the selections. __RemesPath queries (including find/replace form operations) are performed on each selection separately.__ This means that unfortunately you cannot write a RemesPath query that only operates on *some* of the selections.

![RemesPath query on file with selections](/docs/multi%20selections%20Remespath%20query.PNG)

Beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), [automatic linting after editing](#automatically-check-for-errors-after-editing) is disabled while in selection-based mode, to avoid unexpectedly changing the user's selections when the document is automatically parsed.

### Selecting all valid JSON ###

Sometimes it's nearly impossible to select every JSON element in the file. Fortunately, [v5.5 introduced](/CHANGELOG.md#550---2023-08-13) a new method for parsing every valid JSON in the file, `Select every valid JSON` (Alt-P-J-I using accelerator keys).

__This method only parses valid JSON according the [`NAN_INF` logger_level](#parser-settings).__ That means no newlines in strings, non singlequoted strings, no comments, etc.

![Select all valid json](/docs/select%20all%20valid%20json.PNG)

As the above example shows, this method *can* handle unmatched quotes/braces, but you shouldn't count on it. If this method is finding less JSON that you expect, unmatched quotes/braces are likely the culprit.

### Error form and status bar ###

If you click "Yes", a docking form will open up at the bottom of the document. Each row in the document will correspond to a different syntax error.

Clicking on or paging to a row in the error form with the arrow keys will move the caret to the location in the document where that error was found.

Hitting `Enter` while in the form refreshes the form with the JSON in the current document. You can also seek the next syntax error with a description that starts with a letter by typing that letter while in the form. For example, typing `P` in the form might select the next `Python-style '#' comments are not part of any well-accepted JSON specification` error.

Beginning in [v6.0](/CHANGELOG.md#600---2023-12-13), you can right-click on this form to gain the option of exporting all errors to JSON or refreshing the form.

In addition to this form, the document type status bar section will show how many errors were logged.

![Error form and description in status bar](/docs/error%20form%20and%20status%20bar%20section.PNG)

For performance reasons, the error form will never have more than 5000 rows. These rows will be roughly evenly spaced throughout the document.

__For pre-[v6.1](/CHANGELOG.md#610---2023-12-28) JsonTools, *do not click `Yes`* on the dialog that warns of slow reload.__ If you click `Yes`, you can expect to wait an *extremely long time.*

Beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), the error form also reports JSON schema validation errors. They are indicated by `SCHEMA` in the `Severity` column as shown below. In addition, if a file was previously validated, hitting `Enter` to refresh the error form re-validates the file using whatever schema was most recently used for that file.

![Error form reporting schema validation errors](/docs/error%20form%20with%20SCHEMA%20errors.PNG)

<details><summary>Pre-v5.3.0 error reporting</summary>

If you click "Yes", a new file will open in a separate tab containing details on all the syntax errors that were caught. Starting in [v5.1.0](/CHANGELOG.md#510---2023-06-02), errors can also be shown for the current document with a new plugin menu option.

![Linter syntax error report](/docs/linter%20syntax%20error%20report.PNG)
</details>

<details><summary>pre-version 5.0.0 system for configuring JSON parser</summary>

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

**NOTE:** The JSON linter allows the parser to continue parsing even when it encounters syntax errors. That means that the parser will parse some documents that are not valid JSON until the syntax errors are corrected.
</details>

## Automatically check for errors after editing ##

*Added in [version 4.13.0](/CHANGELOG.md#4130---2023-04-11)*

About 2 seconds after a not-very-large file (default less than 4 megabytes, configurable in settings) is opened, and after 2 seconds of inactivity following any modification of the file or styling, the plugin can parse the document and performs [JSON Schema validation](#validating-json-against-json-schema) on the document. The user is notified of any errors when this happens, and no further notifications are sent until the user next modifies or re-styles the document.

This is off by default. If desired, this feature can be turned on in the settings (`auto_validate` setting). When turned on, it only applies to files with `json`, `jsonc`, `jsonl`, and `json5` extensions, or files configured for [automatic JSON schema validation](#automatic-validation-of-json-against-json-schema).

Prior to [v6.1](/CHANGELOG.md#610---2023-12-28), this automatic validation forced the file to be parsed as JSON. As of v6.1, the document will be parsed as [JSON Lines](#json-lines-documents) if the file extension is `jsonl` and as JSON otherwise. In addition, if the document is already in [regex mode](#regex-search-form) or [ini file mode](#parsing-ini-files), automatic validation is suspended.

Beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), this automatic validation will only ever attempt to parse the entire document, not [a selection](#working-with-selections), and automatic validation is always disabled in selection-based mode. Prior to v7.0, automatic validation could change the user's selections unexpectedly.

## Path to current position ##

*Added in version v5.0.0*

The `Path to current position` menu option lets you fill the clipboard with the path to the current position in the document. The path is formatted according to the [`key_style` and `path_separator` settings](#key_style-and-path_separator-settings), described below.

This replaced the old `Path to current line` menu option.

![Path to current position example](/docs/path%20to%20current%20position.PNG)

<details><summary>Path to current line, Removed in v5.0.0</summary>

## Path to current line ##

*Added in version v3.4.0*

The `Path to current line` menu option lets you fill the clipboard with the path to the first node on the current line. This is most helpful when your JSON is already [pretty-printed](/docs/README.md#pretty_print_style) so no two nodes share a line.

![Getting the path to current line](/docs/path%20to%20current%20line.PNG)
</details>

### `key_style` and `path_separator` settings ###

If you are using JsonTools [v8](/CHANGELOG.md#800---2024-06-29) or older, you can ignore the below discussion of the `path_separator` setting, as it was added in [v8.1](/CHANGELOG.md#810---unreleased-yyyy-mm-dd).

The `key_style` and `path_separator` settings control the formatting of the `Path to current position` command.

The `key_style` setting has the following options. `RemesPath` style is the default.
- `RemesPath` style (dot syntax and backtick-quoted strings)
- `JavaScript` style (dot syntax and C-style quoted strings in square brackets)
- `Python` style (C-style quoted strings in square brackets)

If you prefer for keys and indices to be separated by a custom character, use the `path_separator` setting. __`path_separator` ignored when it is set to the default `"\u0001"`; otherwise `key_style` is ignored.__ The `path_separator` character *cannot* be any of the characters in the following JSON string: `"\"0123456789"`.

For example, the different path styles might look like this:
- `Remespath` (default): ``[`a b`][0].c``
- `Python`: `['a b'][0]['c']`
- `JavaScript`: `['a b'][0].c`
- `path_separator` set to `/`: `/"a b"/0/c` (the `"a b"` key is in quotes because it does not match the regular expression `^[_a-zA-Z][_a-zA-Z\d]*$`)
- `path_separator` set to `c`: `c"a b"c0c"c"` (the `"c"` key is also in quotes because it contains the `path_separator`)

## RemesPath ##

*Added in version 1.2.0*

The primary utility of this tool comes from the ability to query and edit the JSON using [RemesPath](RemesPath.md), which you can learn more about in the linked docs.

You can submit RemesPath queries in textbox above the tree, which by default has the `@` symbol in it.

![JSON viewer tree with RemesPath query](/docs/json%20viewer%20with%20remespath%20query.PNG)

Once you've submitted a query, you can use several other features of the JSON viewer.
First, you can open a new buffer containing the query result.

Prior to [v6.0](/CHANGELOG.md#600---2023-12-13), submitting a query automatically attempted to parse whatever document was currently open, thus potentially rebinding the tree to a different document. Starting in [v6.0](/CHANGELOG.md#600---2023-12-13), submitting a query only triggers parsing of whatever document the treeview is currently associated with.

![JSON viewer open query result in new buffer](/docs/json%20viewer%20query%20save.PNG)

## Find and Replace Form ##

*Added in version 3.7.0*

If you want to perform some simple search or find-and-replace operations on JSON without worrying about [RemesPath](/docs/RemesPath.md) syntax, you can use the find/replace form.

Below is an example of a simple search for the substring `on` in keys and values. The tree view displays all the *strings that contain `on`*, or the *values associated with a key that contains `on`.*

![Find/replace form simple find](/docs/find%20replace%20form%20simple%20find.PNG)

Below is an example using the find/replace form to replace the regular expression `(s?in)` with the replacement `$1$1$`, which effectively triples every instance of (`sin` or `in`) in values, converting `raisin` into `raisinsinsin` and `wine` into `wininine`. __When using the `Replace all` button, keys are not affected.__

![Find/replace form simple replace](/docs/find%20replace%20form%20simple%20replace.PNG)

This form provides lets you perform text searches on keys and values in JSON, and also lets you do __mathematical__ find/replace operations on numeric values.

The default behavior of the form is to do a regular expression search on both keys and values, or a text find/replace on values only. You can change that under `Show advanced options`.

Below is an example of searching for all *children of the `year` key* (because `Root` is set to `.year`) that are *less than the number `2010`* (because `Find...` is `< 2010` and the `Math expression` option is checked). This means that the tree view shows only the numbers between `2007` and `2009` in the `year` array.

![Find/replace form math find](/docs/find%20replace%20form%20math%20find.PNG)

Below is an example of *replacing* (by clicking `Replace all`) values *in the `year` array* (by setting `Root` to `.year`) by *subtracting `500` from all values less than `2010`* (because the `Find...` is set to `< 2010` and `Replace with...` is set to `- 500`, and the `Math expression` box is checked).

![Find/replace form math replace](/docs/find%20replace%20form%20math%20replace.PNG)

Prior to version [4.11.0](/CHANGELOG.md#4110---2023-03-15), if you didn't do a regular expression search, your search term must match keys/values *exactly*. Substring matching of non-regular-expressions was *not* supported.

Starting in version [4.11.0](/CHANGELOG.md#4110---2023-03-15), non-regular-expression searching does not require strings to match exactly. Thus, you can now match the `"MOO"` in the array `["MOO", "ZOO"]` with the search term `M` with regular expressions turned off.

![Find/replace form non-regex search must match exactly before v4.11.0](/docs/find%20replace%20form%20nonregex%20exact.PNG)

The form has limited functionality. For example, you can't perform a search on keys and a replacement on values. However, the form generates RemesPath queries in the RemesPath query box in the tree viewer, so you can use those queries as a starting point.

Beginning in [v6.0](/CHANGELOG.md#600---2023-12-13), when a `Replace all` query is run, only the values that were replaced are displayed in the tree. Prior to that, the tree would show the entire JSON after a successful `Replace all` query.

Sometimes you may wish to do find/replace operations __only in *direct children or grandchildren* of an object or array,__ which can be done by *unchecking* the `Recursive search?` button under the `Show advanced options` checkbox.

The find/replace form finds *grandchildren as well as children* in non-recursive mode because of some weirdness in RemesPath syntax.

Below is an example of searching *only direct children or grandchildren* (by unchecking `Recursive search?`) that are less than `0` (because `Find...` is `< 0` and the `Math expression` box is checked).

![Find/replace form non-recursive search](/docs/find%20replace%20form%20NOT%20RECURSIVE.PNG)

## JSON to CSV ##

*Added in version 1.2.0*

Some JSON also has a somewhat __tabular__ format, such that it is amenable to conversion to a CSV file. The JSON in this example is a particularly simple case of this.

This app has a [form](/docs/json-to-csv.md) that allows conversion of such JSON to a tabular format. Remember that even if the JSON file as a whole can't be "tabularized" (or *can*, but you don't *want*  to), you can use a RemesPath query to select the part that you want to tabularize.

![JSON to CSV convertor](/docs/json%20viewer%20csv%20generator.PNG)

At present the __Strategy__ option for the CSV Generation form has four options. You can read more about these strategies in the [docs](/docs/json-to-csv.md).

In [v5.8](/CHANGELOG.md#580---2023-10-09), the line terminator for generated CSV files became customizable (default Unix `LF`, can choose from `CRLF`, `CR`, and `LF`). 

## Changing how much JSON tree is displayed ##

Beginning in version [4.13.1](/CHANGELOG.md#4140---2023-04-12), the tree view is loaded on-demand, whenever the user expands a tree node. The tree is thus very responsive and quick to load.

Beginning in version *4.10.0*, if a JSON array or object has more than `10 thousand` direct children (congigurable in `Settings->max_json_length_full_tree`), this setting will automatically be activated, and `10 thousand` evenly spaced children will be displayed. For example, this would mean that an array with 200 thousand children would result in a tree view with pointers to the first element, the 20th element, the 40th element, and so on.

<details>
<summary>How the tree view worked before <a href="/CHANGELOG.md#4131---2023-04-12">4.13.1</a></summary>

Loading the full tree for very large, complex JSON can cause tremendous memory consumption and make Notepad++ unresponsive for a long time. Because of this, only the __direct children of the root__ are displayed by default for JSON files larger than 4 megabytes. This is reflected in the `View all subtrees` checkbox. You can change this in the settings. This was added in *version 3.1.0*.

Populating the tree is *much* more expensive than parsing JSON or executing RemesPath queries, which means that rather small JSON files with a very large number of nodes (e.g., an array containing 1e5 instances of the number `1`) may take *much* longer to load than larger files with a smaller number of nodes.

![Only direct children of the root are displayed for a big file](/docs/partial%20tree%20load%20example.PNG)

For best performance, you can disable the tree view completely. If the JSON is a single scalar (bool, int, float, string, null), it will display. For arrays and objects, you will only see the type icon.

The `View all subtrees` checkbox on the JSON viewer form allows you to quickly toggle between viewing the full tree and only the direct children. Some notes on the checkbox:
- If the full tree will not be shown when the tree is loaded, this box is unchecked; otherwise it is checked.
- Checking the box when previously unchecked will load the full tree, but the user must click OK at a message box if the document is 2.5 MB or larger or else the box will remain unchecked. This box warns that loading the full tree for a big document could make Notepad++ responsive for a long time.

![Message box warning of unresponsiveness when loading a big document](/docs/full%20tree%20load%20warning%20msg.PNG)
- This message box for canceling loading of the full tree will now also show up when you try to open the full tree for a document 2.5 MB or larger.
- Unchecking the box when the full tree is loaded will cause only the direct children of root to display.
- This box does not change the global settings. It only changes the settings for that tree view.
</details>


## Get info about tree nodes ##

*Added in version 3.4.0*

You can right click on a tree node to copy any of the following to the clipboard:
* Value
* Key/index (see the [the `key_style` and `path_separator` settings](#key_style-and-path_separator-settings))
* Path (see the `key_style` and `path_separator` settings)

In versions 3.4.0 through 3.6.1.1, you can also click on the `Current path` button beneath the tree to copy the path of the currently selected tree node to the clipboard. The path will have the style of whatever default style you chose in the settings (shown in the adjacent text box). In versions 3.7.0 and above, this button does not exist, so just select and copy the text in the box.

![Current path button for path to current tree node](/docs/path%20to%20current%20tree%20node%20button.PNG)

### Select tree node's JSON (or its children) *(added in v5.7)* ###

Starting in [v5.7](/CHANGELOG.md#570---2023-09-08), __you can use the tree view to select any JSON element, or select all children of that element.__

![Treeview select all children - JSON document](/docs/treeview%20select%20associated%20json%20children.PNG)

In some cases this feature will fail to select JSON after entering a RemesPath query. This is (usually) expected behavior and not a bug, because:
* Some queries remember position because they select nodes from the document (e.g., indexers like `@[:].b[@ < 3]`)
* Other indexers create new nodes with position 0 that are a function of nodes in the document (e.g., `@ * 3`).

This functionality can also be used to select regex search results or values from a CSV file. For example, the simple query below, followed by `Select all children` on the root, selects all elements in column 3 (index 2 b/c JSON tools uses 0-based indexing) that begin with uppercase `F` or `B` (disregarding enclosing quotes)

![Treeview select all children - CSV document](/docs/tree%20select%20all%20children%20csv.PNG)

Beginning in [v6.1](/CHANGELOG.md#610---2023-12-28), using this option from the root treenode selects all remembered selections when in [multi-selection mode](#working-with-selections), and selects every JSON line in [JSON lines](#json-lines-documents) mode.

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
### tab_indent_pretty_print ###
Use tabs instead of spaces for indentation. When this setting is `true`, the `indent_pretty_print` setting is ignored and one tab is always used per level of depth. __Introduced in [v5.4.0](/CHANGELOG.md#540---2023-07-04).__

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

At present, three different styles are supported:

[`Google` style](http://astyle.sourceforge.net/astyle.html#_style=google)
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
[`Whitesmith` style](http://astyle.sourceforge.net/astyle.html#_style=whitesmith)
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
`PPrint` style (introduced in version [5.0.0](/CHANGELOG.md#500---2023-05-26)): inspired by [Python's pprint module](https://docs.python.org/3/library/pprint.html)
```json
{
    "algorithm": [
        ["start", "each", "child", "on", "a", "new", "line"],
        ["if", "the", "line", "would", "have", "length", "at", "least", 80],
        [
            "follow",
            "this",
            "algorithm",
            ["starting", "from", "the", "beginning"]
        ],
        ["else", "print", "it", "out", "on", 1, "line"]
    ],
    "style": "PPrint",
    "useful": true
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
As you can see, the sort is *unstable* when comparing two keys that differ only in case. You can't rely on the lower-case key being before the upper-case key or vice versa.

See the [general notes on string sorting](/README.md#note-on-how-jsontools-sorts-strings) for more notes on how strings are sorted.

### remember_comments ###
*Added in version [5.6.0](/CHANGELOG.md#560---2023-08-18).*

If this is true, the JSON parser remembers the location and type of any comments it finds while parsing. If any comments are found while parsing, the next time the JSON is pretty-printed or compressed, the comments will be included.

Pretty-printing with comments attempts to keep all comments in approximately the same position relative to other comments and JSON elements as they were in the original document. The only supported algorithm for pretty-printing with comments is Google style, shown above.

Compressing with comments puts all comments at the beginning of the document, followed by the compressed JSON (with non-minimal whitespace).

__EXAMPLE:__

Suppose you start with this document:
```json
# python comments become JavaScript single-line
[1, 2,/* foo */ 3,
 {"a": [ // bar
   1,
   [1.5]
   ] // any comment that begins after the last JSON element
 } // gets moved to the very end of the doc when pretty-printing
]
```
__Pretty-printing while remembering comments produces this__ (although note that beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), this is only true if your [pretty_print_style](#pretty_print_style) is `Whitesmith` or `Google`):
```json
// python comments become JavaScript single-line
[
    1,
    2,
    /* foo */
    3,
    {
        "a": [
            // bar
            1,
            [
                1.5
            ]
        ]
    }
]
// any comment that begins after the last JSON element
// gets moved to the very end of the doc when pretty-printing
```
__Compressing while remembering comments produces this:__
```json
// python comments become JavaScript single-line
/* foo */
// bar
// any comment that begins after the last JSON element
// gets moved to the very end of the doc when pretty-printing
[1, 2, 3, {"a": [1, [1.5]]}]
```

Beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), choosing the `PPrint` setting for [pretty_print_style](#pretty_print_style) causes comments to be remembered as follows:
```json
[
    ["short", {"iterables": "get", "printed": "on", "one": "line"}],
    {
        "but": [
            "this",
            /* has a comment in it */
            "and gets more lines"
        ]
    },
    [
        "array",
        "would be short enough",
        /* but has */
        1,
        "comment",
        true
    ],
    [
        "and this array is too long",
        "so it goes Google-style",
        "even though it has",
        [0.0, "comments"]
    ]
]

```

## Sort form ##

*Added in [v5.2](/CHANGELOG.md#520---2023-06-04)*

This form provides a convenient way to sort or shuffle arrays in-place. You can sort a single array or use RemesPath to identify multiple arrays that will all be sorted.

Consider this JSON document:
```json
[
    ["this", -1, 4.5],
    ["is", 1, 3.5],
    ["sort", 3, 2.5],
    ["form", 5, 1.5],
    ["example", 7, 0.5]
]
```

We will start by shuffling it.

![Sort form; doc after shuffling](/docs/sort%20form%20shuffle.PNG)

Next, let's sort by the first entry in each subarray.

![Sort form; sort by index in elements](/docs/sort%20form%20sort%20by%20index%20in%20elements.PNG)

Let's do something a little more interesting: *we can sort multiple arrays if a query produces an array or object where all the values are arrays.*

We will use the query `[:3]` to sort the first three subarrays in this document, and leave the last two unchanged. *Note that we need to sort as strings since the values are a mix of numbers and strings.*

![Sort form; sort first three subarrays as strings](/docs/sort%20form%20sort%20subarrays%20as%20strings.PNG)

Finally, let's sort the whole document from largest to smallest by a query on each subarray, `@[2] * s_len(@[0])`, which will sort them by the third element multiplied by the string length of the first element.

![Sort form; sort by query on each element](/docs/sort%20form%20sort%20by%20query%20on%20each%20element.PNG)

Of course, there's also the default sort, which can only compare numbers to numbers and strings to strings. Any mixing of types with the default sort results in failure.

See the [general notes on string sorting](/README.md#note-on-how-jsontools-sorts-strings) for more notes on how strings are sorted.

## Regex search form ##

*Added in [v6.0](/CHANGELOG.md#600---2023-12-13)*

The regex search form (`Alt-P-J-X` using accelerator keys) makes the treeview usable for any document!

Opening up a document in regex mode allows __querying and mutating the raw text of a document with [RemesPath](/docs/RemesPath.md).__ Clicking the `Search` button on the regex search form creates a RemesPath query in the treeview for the current document using the [`s_csv` or `s_fa` functions](/docs/RemesPath.md#vectorized-functions). See the documentation for those functions for more information on the allowed regular expression syntax, but *remember that the regular expression syntax used here is not the same as Notepad++'s find-replace form.*

![](/docs/regex%20search%20form%20regex%20example.PNG)

You can view CSV files (any delimiter, quote character, and newline are allowed) with the treeview, providing that they comply with [RFC 4180](https://www.ietf.org/rfc/rfc4180.txt).

Beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), if the new `auto_try_guess_csv_delim_newline` global setting is set to `true`, whenever the regex search form is opened, or the `Parse as CSV?` button is toggled on, the regex search form will check the first 1600 characters of the current document to detect if it is a CSV or TSV file. This makes the regex search form load more slowly, but it makes it easier to parse CSV files.

![Regex search form viewing a CSV file](/docs/regex%20search%20form%20csv%20example.PNG)

If you want to edit your document using RemesPath, the [`s_sub` function](/docs/RemesPath.md#vectorized-functions) may prove useful for regex-replacement, and the [`to_csv` function](/docs/RemesPath.md#non-vectorized-functions) may be useful for CSV editing.

In the below example, we parse the above document as a CSV file, edit it by changing the second column (named `names`) to `CITY NAME HAS U` wherever the third column (named `cities`) contains the uppercase letter `U`, and then replace the text of the document with the edited file formatted as tab-separated variables.

![Regex search form EDITING a CSV file](/docs/regex%20search%20form%20csv%20REPLACE%20example.PNG)

__Remember: the Notepad++ document is not affected unless you assign something to its value__, even if the values shown in the tree are affected. For example, __`@ = s_sub(@, foo, bar)` *would edit* the Notepad++ document__ by replacing every instance of `foo` with `bar`, but __`var x = s_fa(@, foo); x = bar` *would not edit* the document__ because the search results are a separate entity from the document.

## JSON Lines documents ##

*Added in version [v3.2.0](/CHANGELOG.md#320---2022-09-19)*

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
- Beginning in [v5.8](/CHANGELOG.md#580---2023-10-09), the plugin will by default prompt to confirm if you try to pretty-print a document with the `.jsonl` extension, because *pretty-printing a JSON Lines document will probably make it invalid.* This prompt can be disabled in the settings.
- Also beginning in [v5.8](/CHANGELOG.md#580---2023-10-09), [editing a JSON Lines document with RemesPath](/docs/RemesPath.md#editing-with-assignment-expressions) will keep the document in JSON Lines format.

## Running tests ##

The plugin contains a variety of built-in unit tests and performance benchmarks that can be run on demand from the `Run Tests` plugin menu command.

This repository also contains ["most recent errors.txt"](/most%20recent%20errors.txt), which shows the expected output (modulo some variation in the benchmarking times) of these tests. By comparing the output of running the tests on your computer to the expected output, you can determine whether the plugin's code is working as expected.

Prior to version [4.2.0](/CHANGELOG.md#420---2022-10-29), running these tests should cause the plugin to crash after printing the line `Testing JSON grepper's file reading ability` because some tests referenced an absolute file path.

An easy way to figure out which tests are failing is to use Notepad++'s find/replace form to search for __`Failed\s+[^0]`__ (*with regular expressions on*).

## Parsing INI files ##

*Added in version [v5.8](/CHANGELOG.md#580---2023-10-09)*

INI files (which typically have the `ini` file extension) are often used as config files.

JsonTools can parse INI files with a format like the following example:
```
; comments can begin with ';'
# comments can also begin with '#'
[header]
equals can separate keys and values = yes
  ; any indentation of keys within a section is fine
  colon can also separate keys and values : true

[another header]
another key=foo
  ; comment to indicate that the header on the next line isn't part of a multiline value
  [indented section]
  multiline value = first line
    subsequent lines of multiline value
    need to be more indented than the first
    ; comments end the multiline value

[final section]
more about comments: this ';' does not start a comment
  because the ';' is not the first non-whitespace of the line

even more about comments=this is true for '#' as well
```

INI files will always be parsed as objects, with section headers mapped to objects representing sections. __All the values in a section object will be strings.__

Here's the tree view created for the example above:

![ini file tree view](/docs/ini%20file%20tree%20view.PNG)

INI files can be pretty-printed (as reformatted INI files) and edited with RemesPath.

### Notes
1. It is a __fatal error__ for a document has duplicate section names, or two of the same key within a section.
2. Currently the only file extension that will automatically be parsed as an INI file is `.ini`. If you wish to parse a document with another extension as INI, you need to use the `Open tree for INI file` (Alt-P-J-I-I-Enter using accelerator keys) to parse the document as an INI file.
3. After a RemesPath query converting values to non-strings, they're converted back to strings.

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

Of course, sometimes an API request will fail. You can click the [View errors button](#error-form-and-status-bar) to see any errors that happened.

## Getting JSON from local directories ##

If you want to open up all the JSON files in a directory, here's how to do it:
1. Choose whether you want to search in subdirectories.
2. Choose what [filename search pattern(s)](https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefiles?view=net-6.0#system-io-directoryinfo-enumeratefiles) you want to use (by default files with the `.json` extension). Beginning in [v8.1](/CHANGELOG.md#810---unreleased-yyyy-mm-dd), these filename search patterns can include `\` and `/` characters, and `**` can be used to match any number of characters (including `\`).
3. Choose what directory you want to search. *If you are using a version of JsonTools __older than [v8.0](/CHANGELOG.md#800---2024-06-29)__*, you can do this in one of the following ways:
    - Choose a directory using a GUI dialog. To do this, *make sure that the central list box has the default value of `Previously visited directories...`*, then click the `Choose directory...` button, and a dialog will pop up. Once you select a directory and click `OK`, JsonTools will search the chosen directory.
    ![Choose a directory using a modal dialog](/docs/json_from_files_and_apis%20get%20json%20in%20directory%20USING%20DIALOG.PNG)
    - Choose a previously searched directory from the central list box, *then click the `Choose directory...` button.*
    ![Choose a directory using a dropdown list](/docs/json_from_files_and_apis%20get%20json%20in%20directory%20USING%20DROPDOWN%20LIST.PNG)
4. Choosing a directory takes two steps *if you are using JsonTools __[v8.0](/CHANGELOG.md#800---2024-06-29) or newer:__*
    1. Choose a directory in one of the following ways:
         - Open a dialog (as shown in step 3 above)
         - Select a previously searched directory (as shown above)
         - Type a directory name into the list box.
    2. Click the `Search directories` button below the list box.
    ![Choosing a directory in JsonTools v8.0](/docs/json_from_files_and_apis%20get%20json%20in%20directory%20VERSION%208p0.PNG)

For every file that the JSON tries and fails to parse, the exception will be caught and saved so you can view it later with the [`View errors` button](#viewing-errors).

Beginning in version [v8.0](/CHANGELOG.md#800---2024-06-29), this tool will stop reading files and show an error message once the combined size of all the files to be parsed exceeds about 429 megabytes (215 megabytes for 32-bit Notepad++). This change was made to avoid out-of-memory errors that could cause Notepad++ to crash.

### Viewing results in a buffer ###

If you want to see the JSON found by this tool, just click the `View results in buffer` button. This will open a new buffer in Notepad++ with an object mapping filenames and URLs to the JSON associated with them.

Below is an example of the tree view showing JSON searched from a local directory and two APIS, then displayed as a tree view with the `View results in buffer` button.

![Grepper form - View results in buffer button](/docs/json_from_files_and_apis%20view%20results%20in%20buffer%20button.PNG)

This form has its own tree viewer associated with this buffer. You can use this plugin's normal tree viewers for other buffers. If you close the buffer, the tree viewer is destroyed.

If you wish to filter the files shown in the tree view, you may find the [tree view's find/replace form](#find-and-replace-form) useful.

For example, if you wanted to search only files with `foo` (case-sensitive) in their filename, you would do the following:
1. Click `Find/replace` at the bottom of the tree view.
2. Input the settings (`Find...` = `foo`, `Search in keys or values?` = `Keys`, `Recursive search?` = unchecked, `Ignore case?` = unchecked) in the find/replace form.
3. Click `Find all` in the find/replace form.
4. Look at the tree view, and see which files are displayed. 

### Reporting progress when parsing large amounts of JSON ###

When getting JSON from [directories](#getting-json-from-local-directories) or [APIs](#sending-rest-api-requests), it is possible that JsonTools will need to parse very large amounts of JSON. JsonTools can parse approximately 30 megabytes of JSON per second per thread, but its rate of parsing depends on many factors, chiefly the number of documents to be parsed.

Beginning in [v8.1](/CHANGELOG.md#810---unreleased-yyyy-mm-dd), it will report progress while reading documents from the hard drive, and then report progress again when parsing documents. To report progress, JsonTools will open up a progress reporting form with a green progress bar and a short explanation of what is happening.

This progress reporting only takes place if the total amount of text to be parsed/read is at least 50 megabytes, or if there are at least 16 files (64 when reading from hard drive) with a combined total of 8 megabytes of text.

To be clear, __versions of JsonTools earlier than [v8.1](/CHANGELOG.md#810---unreleased-yyyy-mm-dd) did not have progress reporting for this form,__ but the underlying process was still very reliable, and *just because Notepad++ stopped responding doesn't mean that JsonTools had an error or went into an infinite loop.*

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

Beginning in [v7.0](/CHANGELOG.md#700---2024-02-09), validators can catch multiple JSON schema validation problems, not just one. You can use the [error form](#error-form-and-status-bar) to see where all of the schema validation problems are. To avoid very slow performance on files that do not match the schema, the validator will exit after it encounters 64 problems (configurable by the `max_schema_validation_problem` setting). Note that changes to the `max_schema_validation_problem` setting only take effect the next time you start Notepad++.

As of version [4.11.2](/CHANGELOG.md#4112---2023-03-21), the recursion limit for validation is currently 64. Deeper JSON than that can't be validated, period. Very deep or recursive schemas will still compile.

This tool can only validate the following keywords:

### Keywords for all JSON
* type
* [anyOf](https://json-schema.org/draft/2020-12/json-schema-core.html#name-anyof)
* [enum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-enum)
    * beginning in [v6.0](/CHANGELOG.md#600---2023-12-13), the `enum` keyword can be used with mixed-type enums, and can be used without specifying the `type` keyword. 
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
* [minLength](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-minlength) (*added in [5.0.0](/CHANGELOG.md#500---2023-05-26)*)
* [maxLength](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-maxlength) (*added in [5.0.0](/CHANGELOG.md#500---2023-05-26)*)

### Keywords for numbers
* [minimum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-exclusiveMinimum) and [maximum](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-maximum) (*Both added in version [4.12.0](/CHANGELOG.md#4120---2023-03-28), but both bugged until [5.1.0](/CHANGELOG.md#510---2023-06-02)*)
* [exclusiveMinimum and exclusiveMaximum](https://json-schema.org/understanding-json-schema/reference/numeric.html#id7) (*Both added in version [5.1.0](/CHANGELOG.md#510---2023-06-02)*)

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

### Keywords for strings ###

* [minLength](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-minlength) and [maxLength](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-maxlength) (*added in [7.1](/CHANGELOG.md#710---2024-02-28)*)

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

Let's try out this feature! We can use the plugin command `Choose schemas to automatically validate filename patterns` (renamed in [v8.1](/CHANGELOG.md#810---unreleased-yyyy-mm-dd) to `Validate files with JSON schema if name matches pattern`), which will open up a file that looks like this.

![schemas to filename patterns file new](/docs/schemasToFnamePatterns%20with%20no%20fname%20patterns.PNG)

Read the comments at the beginning of this file; it's self-documenting.

Now let's map a schema file to a filename pattern.

![schemas to filename patterns file successful validation](/docs/schemasToFnamePatterns%20example%20schema%20to%20example%20fname%20success.PNG)

This configuration means that whenever I open a `.json` file with the substring `example` in its name (other than `example_schema.json` itself), the file will automatically be validated against `example_schema.json` (shown in the left of the bottom instance). We see in the right tab in the bottom instance that `example.json` does not have a pop-up message, because it validates against that schema.

Below we can see an example of what happens when a file with a name that matches the pattern *does not validate* under the schema. We get a pop-up message indicating that the schema expected this JSON to be an array, but the JSON in this file is an object.

![schemas to filenames patterns file failed validation](/docs/schemasToFnamePatterns%20example%20schema%20to%20example%20fname%20failure.PNG)

*Note*: the first release where this feature was implemented without causing potential crashes at startup is [4.11.1](/CHANGELOG.md#4111---2023-03-17).

## Toolbar icons ##

Starting in [v5.7](/CHANGELOG.md#570---2023-09-08), JsonTools has toolbar icons for [the tree view](#the-basics), [compressing, pretty-printing](#json-formatting), and [path to current position](#path-to-current-position).

The `toolbar_icons` option in settings lets you customize which toolbar icons show up, and their order, according to the case-insensitive mapping `{'t': 'tree view', 'c': 'compress', 'p': 'pretty-print', 'o': 'path to current position'}`.

Thus, `cot` would give the icon sequence `(compress, path to current position, tree view)`, `P` would give only the pretty-print icon, and `A` would give no icons at all.

## Check JSON syntax now ##

*Added in version [7.2](/CHANGELOG.md#720---2024-04-19).*

This command checks JSON syntax and updates the [error form and status bar](/docs/README.md#error-form-and-status-bar). It *will not* validate using JSON schema. If there are any [remembered selections](#working-with-selections), it will only parse those selections.

This command will *always* attempt to parse the document as JSON, unless the file extension is `.jsonl`, in which case it will attempt to parse the document as [JSON Lines](#json-lines-documents). This will override [regex mode](#regex-search-form) and [INI mode](#parsing-ini-files).

## Styling of forms ##

By default, the forms in JsonTools attempt to use the same color scheme as Notepad++. If you would prefer to have the forms always use the system defaults and ignore Notepad++, you can set the `use_npp_styling` setting to `False`.

For example, the image below shows what forms look like by default with the MossyLawn theme:

![Forms appearance use_npp_styling TRUE](/docs/use_npp_styling%20TRUE%20example.PNG)

And below is what the forms look like with `use_npp_styling` set to `False`:

![Forms appearance use_npp_styling FALSE](/docs/use_npp_styling%20FALSE%20example.PNG)

Beginning in [v8.0](/CHANGELOG.md#800---2024-06-29), the font size for nodes in the tree view is also configurable with the `tree_view_font_size` setting. Below is a side-by-side comparison showing the effect of this setting.

![Side-by-side comparison demonstrating tree_view_font_size setting](/docs/tree_view_font_size%20side-by-side%20comparison.PNG)

## Customizing settings ##

The `Settings` menu item opens a dialog that allows you to customize various aspects of JsonTools. If this documentation references a *setting*, it can be customized there.

These settings are stored in a config file in your plugins config folder. As shown below (look at the file path at the top of the image), this file will be in the directory `\AppData\Roaming\Notepad++\plugins\config\JsonTools\` if you have a normal *ProgramFiles* installation, and in the directory `\plugins\Config\JsonTools\` relative to the root of a *portable* installation.

![Settings form and config file](/docs/settings%20form%20and%20config%20file.PNG)

## DSON ##

JSON is not sufficiently [Doge-friendly](https://dogeon.xyz/index.html). This plugin aims to help correct that.

Currently the plugin only has a DSON emitter. Later I may add a DSON parser.

![DSON example](/docs/DSON%20example.PNG)

Where is DSON generator? Don't know. Not on main plugin menu. So scared. Very confuse. 🐕🤷

DSON is first found in version [4.10.1](/CHANGELOG.md#4101---2023-03-02). molsonkiko such dedicated, much commitment to users, wow.