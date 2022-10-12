# Change Log
All [notable changes](#371-unreleased---2022-mm-dd) to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).
 
## [Unreleased] - yyyy-mm-dd
 
### To Be Added

1. Bring back Json Schema generation (once bugs are fixed)! 
2. Add parsing of unquoted strings when linter is active.
	(would this cause too much of a performance hit?)
3. Add RemesPath functions:
	- for converting JSON to tabular form
	- for dates and datetimes (e.g., a `datediff` function that creates
	somthing like a Python TimeDelta that you can add to DateTimes and Dates)
4. TreeViewer's `fname` attribute should change if the file it is associated with is renamed. Probably need to add a listener for the `NppMsg.NPPN_FILEBEFORERENAME` message.
5. GrepperForm should track the history of folders selected by the user.
 
### To Be Changed

- Consider dropping the `line_num` attrribute of JNodes and replacing it with `position` attribute that tracks position in the JSON string.
	* Pros:
		- allows navigation within a single line; more useful in one-line files
		- faster parsing because the parser doesn't need to track line number
		- cool features like grabbing the path to the location of the caret
	* Cons:
		- backwards-incompatible change
- Replace the folder browser dialog for the grepper form with a text box where the `Tab` key reveals a dropdown with all subfolders of the current folder starting with whatever substring is after the last `\\`. This could be easier to use and more friendly than the current `FolderBrowserDialog`.
- Add a dictionary mapping filenames to TreeViewers. This way each JSON buffer for which a tree viewer was opened can have its own, and the user has a better experience.
	- *Big issue here: files other than the active buffer being closed.* There's no good way using the plugin pack to track any buffers other than the current buffer, because [INotepadPPGateway.GetFilePath](/JsonToolsNppPlugin/PluginInfrastructure/NotepadPPGateway.cs#L111) *can't be used in Win64, because IntPtrs are longs and that function only accepts ints.*
	- This means that if you use the dictionary idea, and you just close the TreeViewer for the active file whenever a file is closed, if you close a file other than the active file, *the file that was closed will keep its TreeViewer and the file that is currently open will have its TreeViewer removed.*
- I am trying to implement lazy loading of subtrees, so that subtrees of the tree view aren't populated until the user clicks on the `+` button. If properly implemented, this would be a marked improvement over the partial tree loading or no-tree option implemented in [version 3.1.0](https://github.com/molsonkiko/JsonToolsNppPlugin/releases/tag/v3.1.0).
- *If lazy loading can't be implemented:*
	- Allow full tree display of small query results even if full tree display is disallowed for the entire JSON.
- Make it so that RemesPath assignment queries like `@.foo = @ + 1` only change the parts of the tree viewer that were affected by the assignment. Would greatly reduce latency because that's the slowest operation.
- Maybe make it so that creating the tree automatically pretty-prints the JSON?
- Consider making it so that trying to get a missing key raises an error instead of returning `{}`. E.g., `@.foo` would raise an exception if the current JSON doesn't have `foo` as a key.
 
### To Be Fixed

- If multiple views are open in the same window pane, the `Save query result` button appends the query result to the current file rather than to the newly opened file.
- JsonSchema has some bugs in the ordering of types. Non-impactful, I think. For example, a type list might come out as `["string", "integer"]` rather than `["integer", "string"]`.
- Remove bug in determination of `required` keys for JsonSchema. As far as I know, this only occurs in very specific cases for the bottom object in an `array->object->array->object->object` hierarchy.
- Fix bugs in YamlDumper.cs:
	- fails when key contains quotes and colon
	- fails when value contains quotes and newline
	- fails when value contains quotes and colon
	- fails when key contains singlequotes and doublequotes
- Fix bug with the range() function where if the first or second argument is a uminus'd function of the CurJson there's an error because the uminus somehow maybe turned the int into a float(???). Error is raised on line 1706 of RemesPath.cs. E.g., `range(-len(@))` and `range(0, -len(@))`) will throw errors.
- The tree view doesn't automatically reset when the user does an undo or redo action. You have to close and reopen the treeview or hit the `Refresh` button for the changes to be reflected. This is annoying, but I can't seem to get my [Main.OnNotification](/JsonToolsNppPlugin/Main.cs) method to respond to undo and redo actions.
- Improve how well the caret tracks the node selected in the query tree, after a query that selects a subset of nodes. The iterables have their line number set to 0.
- Get rid of __ALL__ dinging sounds from the forms, including the `TreeView` control in the TreeViewer.

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

1. `..*` ([recursive search](/docs/RemesPath.md#recursively-find-all-descendents) with the star indexer) now returns an array containing all the scalar descendents of a JSON node, no matter their depth.
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