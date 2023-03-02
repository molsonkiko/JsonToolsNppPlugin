# Change Log
All [notable changes](#4101---2023-03-02) to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).
 
## [Unreleased] - yyyy-mm-dd
 
### To Be Added

1. Show multiple schema validation problems. 
2. Add a thing that maps schema filenames to filename patterns, so that files with certain name patterns are automatically validated a la VSCode.
3. Add parsing of unquoted strings when linter is active.
	(would this cause too much of a performance hit?)
 
### To Be Changed

- Make it so that RemesPath assignment queries like `@.foo = @ + 1` only change the parts of the tree viewer that were affected by the assignment. Would greatly reduce latency because that's the slowest operation.

### To Be Fixed

- Fix bugs in YamlDumper.cs:
	- fails when key contains quotes and colon
	- fails when value contains quotes and newline
	- fails when value contains quotes and colon
	- fails when key contains singlequotes and doublequotes
- The tree view doesn't automatically reset when the user does an undo or redo action. You have to close and reopen the treeview or hit the `Refresh` button for the changes to be reflected. This is annoying, but I can't seem to get my [Main.OnNotification](/JsonToolsNppPlugin/Main.cs) method to respond to undo and redo actions.
- Improve how well the caret tracks the node selected in the query tree, after a query that selects a subset of nodes. The iterables have their line number set to 0.
- Get rid of __ALL__ dinging sounds from the forms, including the `TreeView` control in the TreeViewer.
- When a tree viewer is refreshed using JSON from a file with a different name, the title of the docking form that the user sees doesn't change to reflect the new file. For example, a tree viewer is opened up for `foo.json` and then refreshed with a buffer named `bar.json`, and the title of the docking form still reads `Json Tree View for foo.json`.
	- This is also true if a file with a tree viewer is renamed, e.g., the file `foo.json` is renamed to `bar.json`, but the tree viewer still says `Json Tree View for foo.json`.
- Linter doesn't work on *empty* arrays or objects with no close bracket (e.g., `[1` is parsed as `[1]` but `[` raises an error)

## [4.10.1] - 2023-03-02

### Fixed

1. Hopefully eliminated crash bug that sometimes seems to happen because a file that had a tree viewer associated with it was renamed. This bug is really unpredictable, so it may not be gone.
2. Fixed bug in [Main](/JsonToolsNppPlugin/Main.cs#L360) (based on failure to read [SCI_GETTEXT documentation](https://www.scintilla.org/ScintillaDoc.html#SCI_GETTEXT)) that caused this plugin to be incompatible with versions of Notepad++ older than 8.4.1.

### Changed
1. Changed [RemesPath `s_sub` function](/docs/RemesPath.md#vectorized-functions) so that it either does regex-replace or simple string-replace, depending on the type of the second parameter.

#### Added
1. [DSON emitter and UDL](/docs/README.md#dson) takes this plugin to the moon!

## [4.10.0] - 2023-02-15

### Added

1. Numbers (including `Infinity`) with leading `+` signs can now be parsed if linting is turned on. If linting is not active, they will still raise an error.

### Changed

2. For performance reasons, the plugin no longer automatically turns on the JSON lexer for very long JSON files when pretty-printing, compressing, or showing query results. This is configurable via `Settings->max_size_full_tree_MB`.
3. Arrays and objects with a very large number of direct children no longer result in a tree view with pointers to every direct child. Instead, you just get pointers to a few evenly spaced children. Read more [here](/docs/README.md#changing-how-much-json-tree-is-displayed).

## [4.9.2] - 2023-02-06

### Fixed

1. Previously if you used the `Save query result` button in the tree viewer, and the JSON contained non-ascii characters like 😀, the JSON would cut off early. The `JSON from files and APIs` form had the same problem when viewing results in a buffer.

## [4.9.1] - 2023-01-25

### Fixed

1. Prior to this release, JsonTools didn't track the active view/instance if there were multiple views/instances open. Now the plugin will track which view/instance you are currently editing and perform plugin actions accordingly.

## [4.9.0.1] - 2023-01-12

### Fixed

1. Problems with `stringify iterables` strategy of JSON->CSV with objects mapping to arrays where some of the arrays have unequal length.

## [4.9.0] - 2023-01-11

### Changed

1. Adding an integer to any non-integer, non-float type will now raise an error.
2. true and false will be treated as 1 and 0 respectively for the purposes of comparison operations.
3. Any comparison of an integer, boolean, or float to anything not of one of those types will raise an error.

## [4.8.2] - 2023-01-09

### Added

1. Validation of `patternProperties` keyword in [JSON Schema validation](/docs/README.md#validating-json-against-json-schema).

### Fixed

1. Bugs with queries generated by [find/replace form](/docs/README.md#find-and-replace-form).

## [4.8.1] - 2022-12-26

### Added

1. [Random JSON generation](/docs/README.md#generating-random-json-from-a-schema) from a non-schema file.
2. Tests for random JSON generation.

### Fixed

1. Bug with random JSON generation where *all keys* (including *non-required keys*) in an object schema would always be included in random JSON. Now each object will have a random set of keys that is a superset of the required keys and a subset of all validated keys.

## [4.8.0] - 2022-12-22

### Added

1. [JSON Schema generation](/docs/README.md#generating-json-schema-from-json) is back!

## [4.7.0] - 2022-12-20

### Added

1. Support for extended ASCII characters (i.e., anything from `0x7f` (⌂) to `0xff` (ÿ)) in random JSON.

### Changed

1. Non-ASCII characters (e.g., 😀, Я) are now displayed normally rather than being converted to ASCII using the `\u` notation (e.g., `\ud83d\ude00`, `\u042f`). Resolve [Issue #25](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/25).

### Fixed

1. Fix bug where [find/replace form](/docs/README.md#find-and-replace-form) advanced controls would not be toggled if the user manually resized the form.

## [4.6.1] - 2022-12-11

### Fixed

1. Fixed bugs where renaming a tree view (including the GrepperForm tree view) would cause problems, including a plugin crash upon closing Notepad++.
	- Such crash bugs still seem to appear under conditions that I cannot reliably replicate, but they are rare and innocuous.

## [4.6.0] - 2022-12-09

### Added

1. [JSON schema validation](/docs/README.md#validating-json-against-json-schema).
2. [Generation of random JSON from a schema](/docs/README.md#generating-random-json-from-a-schema).

### Fixed

1. Bugs with handling of binops in RemesPath. Also cleaned up the tests of `log` and `log2`. The longest-standing known bugs in RemesPath are finally squashed!

## [4.5.0] - 2022-12-06

### Added

1. Memory of past JSON from files+apis form directories chosen.

### Fixed

2. When files are removed from the grepper form, the text in the associated buffer is refreshed with the pruned JSON.

## [4.4.0] - 2022-11-23

### Added

1. `any` and `all` [RemesPath functions](/docs/RemesPath.md#non-vectorized-functions) for quickly determining if any or all of the values in a boolean array are true.
2. [UI tests](/ui_tests.py), implemented in Python with [pyautogui](https://pyautogui.readthedocs.io/en/latest/roadmap.html?highlight=window#roadmap). These should be used *carefully* because they use GUI automation.
3. Double-clicking on the body of a tree viewer form (not on any of the buttons, just the empty space) now opens the file associated with that tree viewer.

### Fixed

1. The arbitrary limit on the number of arguments for [RemesPath functions](/docs/RemesPath.md#non-vectorized-functions) like `zip` and `append` has been lifted. Fellow developers: to create a new function with any number of optional args, simply specify input_types up to min_args as normal, then the last input_type is the type for all optional args.
3. The default JSON->CSV algorithm now handles empty arrays by adding a row where all the parent keys of empty arrays are associated with an empty string. Basically, empty arrays are now treated as nulls, rather than being completely ignored.

### Changed

1. There can now be multiple tree viewer forms. Each is associated with a single file. You can still see a JSON tree for a different file than the one that you have open.

## [4.3.0] - 2022-11-07

### Added

1. Button for case-insensitive matching in find/replace form.

### Changed

1. Changed the [API request tool](/docs/README.md#sending-rest-api-requests) to send asynchronous (not multithreaded) requests. This should result in better performance. The `max_api_request_threads` option in [Settings](/docs/README.md#parser-settings) has been removed because it is no longer needed.
2. A recursion limit of 512 has been added for JSON parsing to ensure that the plugin fails gracefully rather than causing Notepad++ to crash because of stack overflow. This is not configurable.
	- This limit also applies to the number of unclosed parentheses in RemesPath, because a query like 2000 * `(` + `1` + 2000 * `)` could also cause stack overflow.

### Fixed

1. If a number is represented as an integer with absolute value `>= 2**63`, it will now be parsed as a double rather than throwing an overflow exception. Any number represented as a 64-bit integer will still be represented internally as an integer.
	- At present, RemesPath queries that might produce integers greater than `2**63` (e.g., `int(2**63)`) do not have such checks in place, so they will cause overflow errors rather than auto-convert to floating point.
2. Clicking the `Find/replace` button on the tree view when a find/replace form already exists now focuses the existing one rather than creating a new one.

## [4.2.0] - 2022-10-29

### Added

1. About form, containing information about the plugin. Resolves [Issue 21](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/21)

### Fixed

1. Test files (the testfiles directory) are now included along with the DLL. This should address [Issue 17](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/17) in which the plugin crashed while running the JSON grepper tests.

## [4.1.0] - 2022-10-26

### Added

1. The tree viewer's color scheme will now match the editor window. You can turn this off in the settings.

### Fixed

1. The `Stringify iterables` strategy of the [JSON to CSV form](/docs/json-to-csv.md) and the `to_records` RemesPath function now correctly works with objects as well as arrays. 

## [4.0.0] - 2022-10-24

### Fixed

1. Changed queries produced by [find/replace form](/docs/README.md#find-and-replace-form) to make it more robust and easy to use.
2. Eliminated potential errors when using the [remove files](/docs/README.md#clearing-selected-files) button on the JSON from files and APIs form.
3. Resolve [Issue #17](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/17) with parsing of floating-point numbers in cultures where the decimal separator is `,` and not the `.` used in the USA.

### Changed

__[.NET Framework 4.8](https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/versions-and-dependencies#net-framework-48) must now be installed on the computer to run the plugin.__ As a practical matter, this means that any Windows OS older than [Windows 10 May 2019 Update](https://blogs.windows.com/windowsexperience/2019/05/21/how-to-get-the-windows-10-may-2019-update/) may be unable to use this plugin going forward.

## [3.7.2.1] - 2022-10-20

### Added

1. With the linter turned on, can now parse unterminated arrays and objects like `{"a": [1, {"b": 2`
	- This currently only works for non-empty arrays and objects (e.g., `[` and `{` will still raise errors).

### Changed

1. Removed `Search Up?` button from find/replace form because it does nothing.

### Fixed

1. The [API request tool](/docs/README.md#sending-rest-api-requests) can now send more than one request per thread.
2. Improved tab-stop behavior on the find/replace form.

## [3.7.1.1] - 2022-10-10

### Changed

1. Parsed JSON is now removed from memory when the associated file is closed.

### Fixed

1. The find/replace form is automatically closed when its associated tree viewer is closed.
	- Unfortunately, the implementation of this feature is such that temporarily hiding the tree view (which can happen when using the `JSON from files and APIs` form) will permanently close its find/replace form. Other solutions were considered and rejected as being even worse.
2. Closing a file other than the temporary buffer associated with the `JSON from files and APIs` tree view no longer closes that tree view. Closing the buffer associated with that tree view still deletes the tree.

## [3.7.0] - 2022-10-08

### Added

1. [GUI form](/docs/README.md#find-and-replace-form) for finding and replacing in JSON.

### Changed

1. `..*` ([recursive search](/docs/RemesPath.md#recursively-find-all-descendents) with the star indexer) now returns an array containing all the scalar descendants of a JSON node, no matter their depth.
2. Replaced the `Current Path` button with a button for opening the [find/replace form](/docs/README.md#find-and-replace-form).
3. Recursive search always returns an array even if no keys match. In that case, an empty array is returned rather than an empty object.

### Fixed

1. When a key contains singlequotes and double quotes (e.g. `"a'\""`), the Python and JavaScript keystyles now correctly formats that key.
2. Eliminated possibility of null dereferencing when using the `Path to current line` command on an unparsed file.

## [3.6.1] - 2022-09-28

### Added

1. Hotkeys for performing plugin commands when the `Plugins->JsonTools` submenu is open (resolve #14).
2. `to_records` [RemesPath function](/docs/RemesPath.md#non-vectorized-functions) for converting JSON to an array of objects. This essentially gives a JSON document with the same contents as the CSV file generated by the JSON to CSV form.
3. `pivot` [RemesPath function](/docs/RemesPath.md#non-vectorized-functions) for taking long data and pivoting it into wide data.
4. Benchmark test for floating-point arithmetic. Currently Remespath appears to be somewhat slower than Pandas (with default column dtypes) for string operations and faster for floating-point arithmetic.

### Fixed

1. Annoying dinging sounds when pressing `Enter`, `Tab`, or `Escape` with the forms. Dinging sounds seem impossible to remove from the `TreeView` control (see #11), but I will keep trying.
2. Make sure that changes to settings are propagated to the `JSON from files and APIs` form if it exists.
3. More useful error messages from RemesPath.
4. `JSON from files and APIs` form's resources are disposed at cleanup.

## [3.6.0] - 2022-09-25

### Added

1. Add [form for getting JSON from files and APIs](/docs/README.md#get-json-from-files-and-apis).

### Fixed

1. Bug with the `Refresh` button where editing a file with RemesPath would not allow Refresh to properly reflect the text in the file.
2. Bug where clicking on a node would not always snap the caret to the node's line if that node was the only one in the tree. 

## [3.5.0] - 2022-09-24

### Added

1. `Refresh` button for resetting the form with the JSON in the currently active buffer (resolves Issue #13).
2. __Tree view enhancements__ (resolves Issue #10):
	- Clicking on a tree node to expand it also changes the displayed node path and snaps the caret.
	- `Tab` can be used to switch between controls on the tree form.
	- Drop-down menu option for expanding/collapsing all subtrees when right-clicking on a tree node.
	- Query box is auto-selected when tree is opened.
	- `Ctrl+Enter` while query box is selected submits the query.
	- `Enter` while any button selected is equivalent to clicking it.
	- `Enter` while tree is selected toggles the selected node between expanded/collapsed.
	- `Escape` key takes focus from the tree back to the text editor (resolves Issue #11).

### Changed

1. Minimizing the tree view closes it completely.

## [3.4.1] - 2022-09-22

### Fixed

1. Resolved [Issue #9](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/9). Navigating through the tree using the arrow keys instead of the mouse now correctly updates the line number and the current path displayed in the box below the tree.

## [3.4.0] - 2022-09-22

### Added

1. [Menu command](/docs/README.md#path-to-current-line) for getting path to first node in current line.
2. Right-clicking on tree nodes lets you get the current node's value, key/index in parent iterable, or path.
3. [key_style](/docs/README.md#key-style) option in settings for customizing how the path is formatted (e.g., dot syntax for JavaScript vs. obligatory square brackets and quotes for Python)
4. Automatic resizing of the query box and the tree view when the docking box is resized.
5. A text box containing the path to the currently selected tree node (in the default key style) and a [button for copying it to the clipboard](/docs/README.md#get-info-about-tree-nodes).

### Changed

1. Settings now persist between sessions. They are saved to an ini file in the Notepad++ config directory.

## [3.3.0] - 2022-09-21

### Added

1. New [RemesPath functions](/docs/RemesPath.md#non-vectorized-functions):
	- `concat` function in Remespath for concatenating arrays or merging objects.
	- `append` function for adding scalars to arrays
	- `add_items` function for adding key-value pairs to objects
2. Menu command for generating a [JSON Lines](/docs/README.md#json-lines-documents) document from a JSON array.
3. [JSON formatting options](/docs/README.md#json-formatting):
	- `sort_keys`, whether to sort the keys of objects
	- `minimal_whitespace_compression`, whether to remove ALL whitespace when compressing JSON or leave one space after each array/object item and after the colon in an object key-value pair, as is the standard style in Python.
	- `indent_pretty_print`, how many spaces of indentation to use per level of JSON.
	- `pretty_print_style`, the style of pretty-printing to use.

### Changed

1. You can no longer have more than one tree viewer open. The `Open JSON tree viewer` command closes the current tree viewer if one is already open.

## [3.2.0] - 2022-09-19

### Added

1. [Checkbox in tree view for toggling full-tree view of the JSON](/docs/README.md#changing-how-much-json-tree-is-displayed).
2. [JSON Lines documents can now be parsed](/docs/README.md#json-lines-documents).

### Changed

1. Got rid of keyboard shortcut for `Run Tests`, since that's really only for people who are debugging the plugin.

## [3.1.0] - 2022-09-17

### Added

1. New setting (`use_tree`) for disabling the tree altogether. Since populating the tree is generally slower than parsing JSON or executing queries, this can provide a significant responsiveness boost.
2. New setting (`max_size_full_tree_MB`) for the maximum size, in megabytes, of a JSON file (default 4) such that the entire JSON tree will be recursively added to the tree view. Populating the full tree could lead to massive latency and memory consumption. Any file above this size will only have the __direct children__ of the root added to the tree, to provide some minimal quality of life without greatly compromising performance.
	- This setting also applies to queries, although I will attempt to add some code to determine if the query resultset is small enough that populating the query's full tree would not be too expensive.

### Bugfixes

1. Fully eliminated early cutoff of CSV files produced by the JSON->CSV form containing non-ascii characters by using `Encoding.UTF8.GetByteCount` instead of my own bespoke byte-counting algorithm.
2. For both the ToString method of string JNodes and the JsonParser, implemented the algorithm used by Python's JSON encoder and decoder for handling surrogate pairs of Unicode characters that represent characters greater than 0xffff.

### Changed

1. As noted above, the default behavior is now to only display the top-level nodes of the JSON for 4+ MB files in the tree view. This can be changed in settings.

## [3.0.0] - 2022-08-30

### Added

1. New 0-arg constructor for JNode that makes a JNode with null value, Dtype.NULL, line_num 0.
2. New 0-arg constructors for JArray and JObject that create instances with no children.

### Bugfixes

1. Fixed some bugs in JsonSchema, but it's still kind of a mess.

### Changed

1. The Make Schema button has been removed, and will not be reintroduced until JsonSchema.cs is debugged. There are enough *known* bugs with JsonSchema.cs on top of the *unknown* bugs that users should not be shown the feature at all, lest they believe it is robust enough to consistently give them a valid schema.

## [2.0.0] - 2022-08-28

### Added

1. Assignment operator in RemesPath. Now you can __edit__ documents with RemesPath, not just query them!
2. Menu option for converting a JSON document to YAML. Until I fix the bugs, it will throw up a message box warning the user that this option has some known bugs.

### Bugfixes

- So it turns out that `Convert.ToDouble(null)`, `Convert.ToInt64(null)`, and `Convert.ToBoolean(null)` all return `0`, which meant that before I added a bunch more runtime type checking, there were stupid bugs with vectorized arithmetic where you could get something like querying `@ - 1` with input `[[]]` returning `[-1.0]`. That has been fixed, and tests have been added to make sure it works.

### Changed
- `RemesParser.Search` and `RemesPathLexer.Compile` now have a required `out bool` parameter that indicates whether the query was an assignment expression. This is a __backwards-incompatible change__.
	- I may try to come up with a better solution, but it seems important for the parser to give some indication of whether the input JSON was mutated.

## [1.2.0] - 2022-08-27

### Added

- RemesPath querying to the tree viewer
- Conversion of RemesPath query results to CSV files
- Creation of JSON schemas for query results

### Changed

- Because RemesPath querying has been added, the tree now tracks the query result rather than the underlying JSON. The caret tracking still mostly works, but only if the query is identity (`@`) or purely indexing-based (i.e., selecting indices from arrays, keys from objects, and applying boolean indices).

## [1.1.1] - 2022-08-27

### Bugfixes

- JsonParser will no longer accept invalid JSON that has anything other than whitespace at the end of a valid document.
	- For example `[1, 2, 3] d` would previously have been accepted (ignoring the `d` at the end) and now the parser will reject it.

### Changed

- Longs (type used for integers in our JSON) are always converted to doubles before comparison to other numbers.
	- Previously, if x was an long and y was a double, the comparison `x > y` would involve converting y to a long, but an identical comparison with different operand order `y < x` would involve converting x to a double.
	- For example, `3.5 < 4` would previously (and still will) return `true` because 4 would be converted to 4.0, but `4 > 3.5` would previously have returned `false` because 3.5 would be converted to 4.
	- The only downside of this approach is that integers between 4.5036e15 (2 ^ 52) and 9.2234e18 (2 ^ 63)
		can be precisely represented by longs but not by doubles, so those integers will have a loss of precision.
- Parsing multiple files found by grepping or multiple API request responses is now multithreaded.

## [1.1.0] - 2022-08-26

### Added

- JSON tree viewer

### Bugfixes

- All known bugs with RemesPath, except the weird issue with uminus'd functions of CurJson as the second argument to the `range(x,y,z)` function.
- Got rid of issue where RemesPath query compilation time was incorrectly calculated.


## [1.0.0] - 2022-08-26

### Added

- Contains only the minimum functionality required to be useful. Linting syntax errors, changing parser settings to allow comments and whatnot, etc.