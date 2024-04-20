# Change Log
All [notable changes](#720---2024-04-19) to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).
 
## [Unreleased] - yyyy-mm-dd
 
### To Be Added

2. Add configurable startup actions for grepper form. Might look like
```json
// grepperFormStartupActions.json
{
    "query": "@.*[:]{foo: @.foo, len_bar: s_len(@.bar)}", // for each URL queried, get a JSON array, from which you select the foo attribute and the length of the bar attribute of each item
    "urls": [
        "https://foo.gov.api?user=bar",
        "https://foo.gov.api?user=baz",
    ]
}
```
3. Add option for users to choose newline in JSON Lines.
4. Make it so find/replace form can emit Python code that is equivalent to the generated RemesPath query
5. Make RemesPath error messages less confusing
6. Implement a less thread-unsafe way to have ArgFunctions use context:
    * Add `uses_context` field to ArgFunction instances, so that they have JQueryContext appended to their arguments, and they can reference fields of that JQueryContext.
	* This way we don't have to have these methods mutating and referencing a global static variable.
	* Additionally, the presence of a function with `uses_context=true` would serve as a flag that the query cannot be executed in parallel, because doing so would cause race conditions associated with the shared JQueryContext fields.
7. Unit tests that randomly generate text with JSON chars to make sure JSON parser never throws for any reason, since errors aren't caught.

### To Be Changed

- *(Note to future devs)*: Resist the temptation to fool around with the StringBuilder initial capacity for the ToString method of `Dtype.STR` JNodes. I tried, and it doesn't help performance.

### To Be Fixed

- Make sure there aren't any easily-triggered race conditions induced by [automatic parsing and validation after editing](/docs/README.md#automatically-check-for-errors-after-editing).
    - In 6.1.1.18, there is no longer a global shared JsonParser, which was the main potential source of race conditions.
- Fix issue where pretty-printing or compressing causes tree view position tracking to be out of sync with the document until a query is issued or the `Refresh` button is hit.
- Improve Alt-key accelerators *in forms*. They don't seem to work right for some reason.
- When a tree viewer is refreshed using JSON from a file with a different name, the title of the docking form that the user sees doesn't change to reflect the new file. For example, a tree viewer is opened up for `foo.json` and then refreshed with a buffer named `bar.json`, and the title of the docking form still reads `Json Tree View for foo.json`.
	- This is also true if a file with a tree viewer is renamed, e.g., the file `foo.json` is renamed to `bar.json`, but the tree viewer still says `Json Tree View for foo.json`.
- Fix problem where icons disappear when going from light mode to dark mode (until Notepad++ is closed; does not persist after restart)
    - this problem only seems to appear after the user has opened a docking form, and maybe not even every time
- bug with calling arg functions on projections - seems like object projections are treated as arrays when calling arg functions on them in some cases?
- issue with treeview closing when a file with a treeview is moved from one view to another
- `loop()` function used in `s_sub` callbacks is not thread-safe. *This doesn't matter right now* because RemesPath is single-threaded, but it could matter in the future.
- __GrepperForm loses its JSON permanently when the buffer associated with its treeview is deleted.__
- Since v7.0, holding down `Enter` in a multiline textbox (like the [tree viewer query box](/docs/README.md#remespath)) only adds one newline when the key is lifted.
- Maybe use pre-7.1 (dictionary-based rather than indicator-based) [selection remembering](/docs/README.md#working-with-selections) for Notepad++ 8.5.5 and earlier? Indicators are risky with those older NPP's because of the lack of `NPPM_ALLOCATEINDICATOR`.

## [7.2.0] - 2024-04-19

### Added

1. [`Check JSON syntax now` command](/docs/README.md#check-json-syntax-now).

### Changed

1. Made it so that reloading the [error form](/docs/README.md#error-form-and-status-bar) by pressing `Enter` would not cause certain message boxes to appear, to eliminate a potential "infinite" loop where the user would hit `Enter` to close the message box, and that moved focus back to the error form, which then repeated the cycle when they lifted the `Enter` key.
2. [Automatic validation](/docs/README.md#automatically-check-for-errors-after-editing) when `auto_validate` is true no longer opens the prompt asking if user wants to open the error form (if [`offer_to_show_lint`](/docs/README.md#parser-settings) is true), because that could cause Notepad++ to crash or hang forever (see [issue 60](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/60#issuecomment-2065419075)).
3. Made it so that automatic JSON schema validation (that is, any validation not manually invoked by the plugin menu command) no longer causes the caret to move to the location of the first schema validation error.
4. Automatic validation (including non-schema validation) now refreshes the error form.

### Fixed

1. Minor bug in [PPrint remembering comments](/docs/README.md#remember_comments) algorithm implementation that caused some arrays and objects to be compressed when they should have been pretty-printed.
2. Fix bug where tests could crash under some circumstances due to filesystem weirdness making it impossible to find test files.

## [7.1.0] - 2024-02-28

### Added

1. `minLength` and `maxLength` keywords are now considered when making [random JSON from schema](/docs/README.md#generating-random-json-from-a-schema)
2. JsonTools now automatically navigates to the location of the fatal error when it fails to parse a document, unless the attempted parse was auto-triggered (say, by the [automatic parse after editing](/docs/README.md#automatically-check-for-errors-after-editing))
3. [Selection-based mode](/docs/README.md#working-with-selections) now supports any number of remembered selections, and undo and redo actions usually do not cause selections to be forgotten. Performance also appears to be better.

### Changed 

1. If the location of an error is between the `CR` and the `LF` of a `CR LF` newline, JsonTools will now always move before the `CR`, rather than placing the caret in between them, which could cause confusion.
2. [Automatic JSON schema validation after editing](/docs/README.md#automatically-check-for-errors-after-editing) does not happen when the document is in `REGEX` mode.
3. Removed the `max_tracked_json_selections` setting, as it is no longer necessary.

### Fixed

1. Using the [Notepad++ find/replace form](https://npp-user-manual.org/docs/searching/#dialog-based-searching) in Notepad++ versions 8.6.3 and 8.6.4 now appropriately shifts remembered selections when in [selection-based mode](/docs/README.md#working-with-selections).
2. Bug where the space key did not work on the [regex search form](/docs/README.md#regex-search-form).
3. Bug where [automatic JSON schema validation after editing](/docs/README.md#automatically-check-for-errors-after-editing) sometimes caused a deadlock that disabled some functionalities until Notepad++ closed.
4. Possible plugin crash due to index-out-of-bounds error during parsing of [JSON Lines](/docs/README.md#json-lines-documents).

## [7.0.0] - 2024-02-09

### Added

1. [PPrint-style pretty-printing that remembers comments](/docs/README.md#remember_comments)
2. Add [`and` and `or` non-vectorized functions in RemesPath](/docs/RemesPath.md#non-vectorized-functions), which both use conditional excution.
3. Add [`s_format` vectorized RemesPath function](/docs/RemesPath.md#vectorized-functions), for easy reformatting of JSON strings.
4. [Python-style single-line comments in RemesPath](/docs/RemesPath.md#comments-added-in-v62)
5. A [RemesPath user-defined language (UDL) file](/RemesPath%20UDL.xml), providing some very basic syntax highlighting. It is buggy, but that is because the UDL system is inherently buggy, not because I did anything wrong (as far as I know).
6. A `:` character between two key-value pairs in an object no longer causes a fatal error that makes the parser quit.
7. Add new `auto_try_guess_csv_delim_newline` setting. If this is true (default false), [Regex search form](/docs/README.md#regex-search-form) now makes a very basic attempt to "sniff" if the current file is CSV whenever it is opened, or when the `Parse as CSV?` button is toggled on.

### Changed

1. __Support for multiple [JSON schema validation problems](/docs/README.md#validating-json-against-json-schema)__
2. __All [RemesPath regular expressions](/docs/RemesPath.md#regular-expressions) are now multiline__, meaning that `^` and `$` now match the start and end of *lines* respectively, rather than the start and end of the *document.*
3. The [`ifelse` vectorized function in RemesPath](/docs/RemesPath.md#vectorized-functions) now uses conditional execution.
4. Default value for [`sort_keys` setting](/docs/README.md#sort_keys) is now `false`, meaning keys are left in their original order by default. This will not change existing settings. 
5. Add optional arguments [to `stringify` non-vectorized function in RemesPath](/docs/RemesPath.md#non-vectorized-functions), so that users can control the format of the output.
5. Make dark mode icons darker.
6. *This change only affects the code base, not the public API:* changed almost all snake_case variable names to camelCase. [RemesPath functions still use snake_case](/JsonToolsNppPlugin/JSONTools/RemesPathFunctions.cs) (e.g., `s_mul` and `group_by` still have those names), and all the settings in [Settings.cs](/JsonToolsNppPlugin/Utils/Settings.cs) (e.g., `use_npp_styling`) that were previously snake_case are still snake_case.
7. [Automatic linting after edits](/docs/README.md#automatically-check-for-errors-after-editing) is now disabled while in [selection-based mode](/docs/README.md#working-with-selections).
8. [Automatic linting after edits](/docs/README.md#automatic-validation-of-json-against-json-schema) will always attempt to parse the entire document, even if the user has made a selection that could be parsed as JSON.
9. Numbers with unnecessary leading 0's (like `01` or `002.5`) are now [logged at the `BAD` level](/docs/README.md#parser-settings), and numbers with trailing decimal points are now logged at the `JSON5` level.
10. [Error form](/docs/README.md#error-form-and-status-bar) keypress triggers now execute when the key is released, rather than when it is depressed.
11. [Automatic JSON schema validation](/docs/README.md#automatic-validation-of-json-against-json-schema) now ignores the user's selections and always validates the entire document.

### Fixed

1. Fixed issue where [vectorized functions in RemesPath](/docs/RemesPath.md#vectorized-functions) were not vectorized across objects if the first argument was a function of input and at least one of the non-first arguments was also a function of input.
2. Fixed issue where entering invalid text into comboboxes in the [regex search form](/docs/README.md#regex-search-form) and [grepper form](/docs/README.md#get-json-from-files-and-apis) could cause a (mostly harmless) plugin crash.
3. Improved refresh and loading speed for tree viewer on very large files in [regex mode](/docs/README.md#regex-search-form).
4. Fix issue where tree view tracking of capture groups in [regex search mode](/docs/README.md#regex-search-form) was wrong in some cases involving non-ASCII characters.
5. Hitting `Escape` in the error form moves focus to the editor component.
6. When logging errors in [selection-based mode](/docs/README.md#working-with-selections), the [error form](/docs/README.md#error-form-and-status-bar) now navigates to the location of the error in the overall document. Previously it navigated to the location of the error relative to the start of the selection.
7. The regex search form no longer closes the currently active tree viewer when you focus or open it.
8. Register forms (other than the modal AboutForm and JSON-to-CSV form) with Notepad++ using [NPPM_MODELESSDIALOG](https://npp-user-manual.org/docs/plugin-communication/#2036-nppm-modelessdialog). This permanently fixes [the regression in Notepad++ 8.6.1](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/54) where Ctrl+X and Ctrl+C stopped working in textboxes.

## [6.1.1] - 2023-12-28

### Fixed

1. Eliminated potentially unrecoverable plugin crash when JSON parser tries to parse document with `-` or `+` not followed by numeric chars (e.g. `-a`, `+`). Now such badly formatted numbers are parsed as `NaN`.

## [6.1.0] - 2023-12-28

### Added

1. [Python-style f-strings in RemesPath](/docs/RemesPath.md#f-strings-to-easily-glue-together-strings-and-non-strings-added-in-v61).
2. [`s_cat` RemesPath non-vectorized function](/docs/RemesPath.md#non-vectorized-functions).
3. [`s_lines`, `s_lpad`, `s_rpad`, and `zfill` RemesPath vectorized functions](/docs/RemesPath.md#vectorized-functions).
4. [`Select all children` treenode action](/docs/README.md#select-tree-nodes-json-or-its-children-added-in-v57) now works for root treenode in JSON Lines and selection-based documents.

### Changed

1. RemesPath syntax errors (anything caught by the lexer) now use `>>>HERE>>>` before the character where the error occurred, similar to how the Notepad++ find/replace form indicates the location of a regular expression syntax error.
2. *Not part of public-facing API:* Renamed the `JQueryContext.Evaluate` method to `JQueryContext.Operate`, and renamed `JMutator.Mutate` to `JMutator.Operate`.
3. Make it so [automatic validation](/docs/README.md#automatically-check-for-errors-after-editing) does not require the document to be re-parsed as JSON, and is suppressed when in regex or ini mode.

### Fixed

1. Eliminated plugin crash when attempting to open the [regex search form](/docs/README.md#regex-search-form) after it had been closed.
2. Greatly improved [error form](/docs/README.md#error-form-and-status-bar) reloading performance. 
3. Some UI test failures (and probably related weirdness in public API) on older NPP versions
4. Fix bug where [`s_csv` RemesPath function](/docs/RemesPath.md#vectorized-functions) did not properly handle delimiters that were regex metacharacters like `|`

## [6.0.0] - 2023-12-13

### Added

1. Option to customize which [toolbar icons](/docs/README.md#toolbar-icons) are displayed, and their order.
2. __New [regex search form](/docs/README.md#regex-search-form) for using treeview to see regex search results in any file.__
3. New [document type list box in tree view](/docs/README.md#document-type-box-added-in-v60)
4. [For loops in RemesPath](/docs/RemesPath.md#for-loopsloop-variables-added-in-v60)
5. [`bool`, `num`, `s_csv` and `s_fa` RemesPath vectorized arg functions](/docs/RemesPath.md#vectorized-functions)
6. [`randint`, `csv_regex`, `set`, and `to_csv` RemesPath non-vectorized arg functions](/docs/RemesPath.md#non-vectorized-functions)
7. Make second argument of [`s_split` RemesPath function](/docs/RemesPath.md#vectorized-functions) optional; 1-argument variant splits on whitespace.
8. Right-click dropdown menu in [error form](/docs/README.md#error-form-and-status-bar), allowing export of errors to JSON or refreshing the form.
9. The [JSON parser](/docs/README.md#parser-settings) is now much better at recovering when an object is missing its closing `'}'` or an array is missing its closing `']'`.
10. Support for [JSON Schema validation](/docs/README.md#validating-json-against-json-schema) of `enum` keyword where the `type` is missing or an array.
11. `Ctrl+Up` now snaps to parent of currently selected node in tree view. `Ctrl+Down` now snaps to the last direct child of the currently selected node.

### Changed

1. __The internal representation of object keys has changed to allow unescaped strings as keys (*THIS DOES NOT AFFECT THE PUBLIC API EXCEPT IN SOME CORNER CASES IN REMESPATH*).__
    * What I mean by this is that previously the key in the JSON object `{"\"": 3}` was *previously* internally represented as `"\\\""`, with the quote character escaped as shown.
	* This had the advantage of making it slightly faster to display object keys (e.g., when pretty-printing/compressing/dumping), but *made it impossible for certain strings to be valid object keys, in a way that could not be detected except when pretty-printing or compressing.*
	* Under the new system as of this version, all strings are acceptable as object keys. This has positive implications for RemesPath, as it means that users do not need to remember to escape string JNodes before using them as keys in an object.
2. When using the [JSON-to-CSV form](/docs/json-to-csv.md#how-json-nodes-are-represented-in-csv) to create CSV files, newline characters will no longer be escaped in strings. This is one of several changes made in order to acheive compliance with [RFC 4180](https://www.ietf.org/rfc/rfc4180.txt).
3. Made [`offer_to_show_lint` setting](/docs/README.md#parser-settings) (which controls whether a prompt is shown when errors are found) true by default, so that a fresh installation will show the prompt.
4. Change RemesPath indexers to reduce the number of backslash escapes needed to get keys containing special characters like `"a\\b"` or `"\"foo\"\tbar"`. For instance, previously ``@.`\\n\\\\a\"` `` would be required to match the key `"\n\\a\""`, whereas now `` @.`\n\\a"` `` matches it.
5. Running a RemesPath query only causes an attempted re-parsing of the document if the treeview's current file is open.
6. Running a replace query on the [find/replace form](/docs/README.md#find-and-replace-form) now causes the tree to display only the values that were mutated.
7. Changed the tabstop order of some forms, made it so that the find/replace form has complete tabstop coverage, and eliminated some tabstop-related issues associated with combo boxes in some forms.
8. Benchmarks for compiling RemesPath queries now more accurately represent the effect of caching on performance.

### Fixed

1. Fixed plugin crash when attempting to parse too-large hex numbers like `0x100000000000000000000`. Now the parser will fatally fail and add a lint indicating the issue, but the plugin will not actually crash.
2. Fixed some weird issues where mutating a variable in RemesPath could cause re-executing a query on the same input to return a different value. A minimal example: `var x = 1; x = @ + 1; x` would return 1 + (the number of times the query was executed) prior to this fix, but now it will always return `2` as expected. This was also true of a bunch of other things in RemesPath, including [projections and the map operator](/docs/RemesPath.md#projections).
3. Fix issues where running a RemesPath query with a projection that referenced a variable indexing on a compile-time constant would cause an error. For example, `var x = @; 1->x` should return `@` (the input to the query), but prior to this fix, it would instead cause an error.
4. Running tests would previously cause clipboard data to be lost irreversably. Now, if the user's clipboard contained text before running tests, the contents of the clipboard are restored to their pre-test values rather than being hijacked. __Non-text data that was copied to the clipboard is still lost when running tests, and I may try to fix that in the future.__ 
5. `dict` function in RemesPath previously had a bug that could create invalid JSON if the strings to be turned into keys contained special characters (e.g., literal quote chars, `\r`, `\n`).
6. access violations when loading [error form](/docs/README.md#error-form-and-status-bar)
7. unnecessary prompt when manually reloading [error form](/docs/README.md#error-form-and-status-bar)
8. issue with trying to view error form when the error form was already open
9. RemesPath backtick strings now can have a literal `\` character just before the closing backtick. Previously this was impossible because of a regex-writing bug.
10. Eliminated plugin crash when attempting to validate with an invalid JSON schema. Now a message box will show in that situation.

## [5.8.0] - 2023-10-09

### Added

1. __Add [parser for `.ini` files](/docs/README.md#parsing-ini-files)__, allowing them to reformatted, viewed with the tree view, and queried and edited with RemesPath.
1. [Python-style spreading of an array](/docs/RemesPath.md#spreading-function-args-to-fill-multiple-arguments-added-in-v58) with `*` to fill multiple arguments of a function.
2. New RemesPath function(s): [`at` function](/docs/RemesPath.md#non-vectorized-functions) for indexing into array or object at an index or key determined at runtime (since indexing with square braces does not support keys/indices that are functions of input).
3. Made it possible to customize newline for CSV files generated by the [JSON-to-CSV form](/docs/README.md#json-to-csv).

### Changed

1. If the user chooses to parse a document as JSON Lines or INI using one of the commands from the main plugin menu, their choice is remembered when the document is re-parsed (including when the tree is re-opened and when the document is pretty-printed and compressed).
	- For example, previously if you used the `Parse JSON Lines document` to parse a document as JSON Lines, and then closed and re-opened the tree view, the tree would be re-opened as JSON. Now the tree view will be re-opened as JSON Lines.
	- This remembered choice can be overridden by parsing as INI or JSON Lines again, selecting valid JSON, or (sometimes) selecting the entire document (if it is valid JSON).
2. All instances of `\r` (carriage return) in strings are now escaped to `\\r` when generating CSVs with the [JSON to CSV form](/CHANGELOG.md#580---2023-10-09).
3. Prompt user (can disable the prompt if desired) before pretty-printing [JSON Lines documents](/docs/README.md#json-lines-documents).

### Fixed

1. Bug where reformatting some large floating-point numbers would output text that could not subsequently be parsed.
2. Fixed bug where changing the `try_parse_start_chars` setting would cause UI tests to fail.
3. JSON Lines documents can now be edited with RemesPath while keeping them in JSON Lines format.
4. Issue where multiple tabs would be used for indentation when both the `tab_indent_pretty_print` and `remember_comments` settings were true (only on files with comments). 
5. Plugin crashes with single-line comments (both Python-style and JavaScript-style) that had no trailing newline.
6. Bug where one-character comments that went to EOF would be truncated to an empty string.
7. UI test failures when `sort_keys` setting was false.

## [5.7.0] - 2023-09-08

### Added

1. Toolbar icons for [JSON pretty-print, JSON compress](/docs/README.md#json-formatting), treeview, and [path to current position](/docs/README.md#path-to-current-position). Fix [issue 36](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/36).
2. Ability to [select JSON or a JSON's children from the treeview](/docs/README.md#select-tree-nodes-json-or-its-children-added-in-v57).
3. [Variable assignment in RemesPath](/docs/RemesPath.md#assigning-variables-added-in-v57-and-executing-multi-statement-queries).
4. [Negated indexers in RemesPath](/docs/RemesPath.md#negated-indexing-and-excluding-keys-v57-only).
5. Ability to group by multiple variables with the [`group_by` RemesPath function](/docs/RemesPath.md#non-vectorized-functions).
6. Option to skip the API request test when [running tests](/docs/README.md#running-tests).

### Changed

1. Selections that don't begin with a valid JSON document will now be ignored when parsing a document. See [working with selections docs](/docs/README.md#working-with-selections) for more info and examples.

### Fixed

1. [Sort form](/docs/README.md#sort-form) previously did not follow Notepad++ styling on startup.
2. Problems (introduced in [v5.5](#550---2023-08-13)) with running tests multiple times without cleaning up test files.

## [5.6.0] - 2023-08-18

### Added

1. New option for [preserving comments when pretty-printing or compressing](/docs/README.md#remember_comments) (just enable `remember_comments` in settings)
2. Support for [projections of arbitrary type with the `->` operator](/docs/RemesPath.md#projections).

### Changed

1. When a RemesPath query performed on a [file with multiple selections](/docs/README.md#working-with-selections) fails on a selection, that selection is not shown at all in the tree view for the query result (previously it did appear, but the selection `start,end` was mapped to `null`).

### Fixed

1. Fix crashes due to race conditions in the [JSON from files and APIs](/docs/README.md#get-json-from-files-and-apis) form.
2. Eliminated some annoying but harmless plugin crashes.
3. Fix long-standing issue where closing buffer `A` with no associated treeview would not re-open the treeview of the file `B` automatically opened by Notepad++ when `A` was closed.
4. Dramatically reduce the frequency of Notepad++ freezing and crashing while running UI tests.
5. Fix minor issue concerning the position assigned to JSON elements when pretty-printing or compressing JSON that didn't begin at the start of the file before being reformatted.
6. Improved cursor tracking of arrays and objects when a RemesPath query selects a subset of their items.
7. Bug where a RemesPath query that produced an object that was not a function of the input would have a runtime errror and fail to populate the treeview.

## [5.5.0] - 2023-08-13

### Added

1. __Add support for [operating on selections](/docs/README.md#working-with-selections)__ (fix [issue 43](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/43)).
2. Add method for [selecting every valid JSON element in the file](/docs/README.md#selecting-all-valid-json)
3. Add `D&ump text of current document as JSON string` and `Dump JSON string(s) as ra&w text` convenience methods.
4. Add [`parse`](/docs/RemesPath.md#vectorized-functions), [`type` and `stringify`](/docs/RemesPath.md#non-vectorized-functions) RemesPath functions.
5. Added UI tests.

### Changed

1. Improved RemesPath [boolean indices](/docs/RemesPath.md#boolean-indexing) so that they can be more easily chained together.
2. Removed unneeded RemesPath lexer tests.

### Fixed

1. Bug where `s_slice`, `max_by`, `min_by`, `group_by`, and `sort_by` all did not allow Python-style negative indices.
2. Bug where out-of-bounds negative indices when indexing in an array would throw an error rather than returning an empty array (which is the correct behavior, since RemesPath is not supposed to throw errors for indexing out of bounds).
3. Eliminated huge latency when viewing very long JSON strings in the treeview.
4. Eliminated potential access violation during plugin cleanup.
5. Bug in which the plugin would be mistaken about the position of JSON elements after PPrint-style printing of some JSON containing non-ASCII characters in strings.
6. Bug in which multiple Sort forms could be open.
7. Bug in which running tests in Notepad++ versions older than v8 could cause Notepad++ to crash.

## [5.4.0] - 2023-07-04

### Added

1. Added support for tab indentation. Fix [issue #38](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/38).
2. Hitting `Enter` while in the error form causes the form to refresh.
3. In the error form, searching for the next row starting with a character now wraps around.
4. Python constants `nan` and `inf` can now be parsed as `NaN` and `Infinity` respectively.
5. Support for numbers with leading decimal points and hex numbers in RemesPath.
6. Unquoted strings in RemesPath can now contain leading `$`, Unicode escapes, and all the Unicode characters that are allowed in unquoted JSON5 keys.
7. Add caching of RemesPath queries for slightly better performance on repeated execution of the same query.
8. Massively impreoved test coverage.

### Changed

1. All arithmetic operations (`+`, `-`, `*`, `/`, `%`, and `//`) [can now accept booleans as one or both of their operands](/docs/RemesPath.md#binary-operators-unary-operators-and-arithmetic).
2. [`not` is now a unary operator](/docs/RemesPath.md#unary-operators) like `-` (that is, `not not @` is a valid statement, no parens needed). It also now tests "truthiness" like the same operator in Python.

### Fixed

1. Bugs with operator precedence in RemesPath.
2. Bugs with array and string slicing in RemesPath.
3. Uncaught errors when parsing integers too large for the floating point spec (e.g., one followed by 400 zeros)
4. Minor bug when scrolling up with the up arrow on error form.
5. More appropriately silly error handling when dogeifying JSON.

## [5.3.0] - 2023-06-10

### Added

1. New [error form](/docs/README.md#error-form-and-status-bar) for viewing syntax errors.
2. When JSON is parsed, the [document type status bar section changes](/docs/README.md#error-form-and-status-bar) to reflect the document's level of compliance with the JSON standard.
2. Optional `sort_by_count` argument for [`value_counts` function](/docs/RemesPath.md#non-vectorized-functions), which sorts subarrays by count if true.

## [5.2.0] - 2023-06-04

### Added

1. [Sort form](/docs/README.md#sort-form) for sorting and shuffling arrays.
2. New [RemesPath functions](/docs/RemesPath.md#non-vectorized-functions) `rand`, `enumerate`, and `iterable`

### Changed

1. `Alt-P-J-Y` key chord no longer dumps YAML. It instead opens the [Sort form](/docs/README.md#sort-form)

### Fixed

1. Opening a tree viewer no longer sets the document's lexer language to JSON if parsing failed.

## [5.1.0] - 2023-06-02

### Added
1. The `*` multiplication operator in RemesPath now supports multiplication of strings by integers (but not integers by strings). For example, `["a", "b", "c"] * [1,2,3]` returns `["a", "bb", "ccc"]`.
2. Setting, `offer_to_show_lint` to disable the pop-up prompt that asks if the user wants to see syntax errors.
3. Menu option to open a new document with the most recently found syntax errors for the current document.
4. Dramatic improvement in speed of recursive search in RemesPath, which in turn improves the find/replace form.
5. Support for *validation* of the `exclusiveMinimum` and `exclusiveMaximum` keywords for numbers, but not random JSON generation that observes those keywords.
6. Further improvement of error messages from RemesPath queries.

### Changed
1. Whenever the user modifies a document that has an active tree viewer, a flag is set so that the document will be re-parsed the next time the user executes a query (including when the find/replace form is used).

### Fixed
1. Bug where numbers did not have their type validated correctly when using `minimum` and `maximum` keywords.

## [5.0.0] - 2023-05-26

### MAJOR CHANGES

Parsing is completely overhauled in version `5.0.0`. Here are the key changes:
1. __The parser tracks the cursor position of JSON elements instead of the line number.__
    * This is the cursor position of the start of the element in the UTF-8 encoding of the document.
	* This makes navigating the document using the tree view *much better* because it is useful for navigation even when the entire document is one line.
2. __Errors while parsing need not throw errors__
    * Instead the parser has a `state` attribute that tracks how well the JSON string complies with JSON specifications of varying degrees of strictness.
	* The parser also has a `logger_level` attribute that determines how strictly the parser will enforce the JSON spec.
	* If the parser's `state` ever reaches `FATAL`, parsing is immediately aborted and whatever has been parsed so far is returned.
	* *The user can choose to throw an error if the state ever exceeds `logger_level`.*
	* __Advantages of the new approach:__
	    1. The parser can return all the valid parts of invalid JSON up to the point where the fatal error occurred.
		2. The plugin can use the status bar to show how severely the JSON deviates from the JSON standard.
		3. Tracking of errors in the parser's `lint` attribute is now independent of the strictness of the parser.
3. The `allow_comments`, `allow_unquoted_string`, `allow_nan_inf`, and `linting` settings have been eliminated.
    * They have been replaced by the `logger_level` setting, [described here](/docs/README.md#parser-settings)
4. Parsing appears to be about 30% faster.

### Minor changes

1. Changed the default value of `minimal_whitespace_compression` to `true`.
2. Made it so that automatic parsing and schema validation is off by default.

### Added

1. New pretty-printing mode, [PPrint](/docs/README.md#pretty_print_style).
2. Find/replace form now automatically refreshes tree view on use, to ensure most up-to-date JSON is used
3. Slight improvement to parsing performance, major improvement to pretty-print/compression performance
4. Support for `minLength` and `maxLength` keywords in JSON Schema validation of *strings*.
5. Support for the rest of the JSON5 specification, with the following exception(s):
	* Escaped newlines in strings are ignored. *Note that this will not work if you are using newlines other than `\r`, `\n`, or `\r\n`!*
	* Escaped digits are simply treated as digits, no matter what.
5. Support for the `undefined` and `None` literals, which are parsed as `null`.
6. Support for the `True` and `False` literals. Since `None` is now also supported, __Python-style JSON documents are now fully supported.__
7. Forms other than tree views are now colored to match the Notepad++ theme.

### Fixed
1. Remove annoying bug where SOH characters (Ascii code `\x01`) were sometimes added to the end of the document when pretty-printing or compressing.
2. Comments immediately after numbers no longer throw an error
3. Empty unclosed arrays and objects no longer throw an error
4. Paths to treenodes including an empty string key (e.g., `{"": 1}`) no longer throw an error
5. Better handling of comments, especially empty comments
6. Performance bug (introduced in [4.14.0](#4140---2023-04-12)) in `Expand/Collapse all subtreees` tree node right-click menu option
7. Better RemesPath error message when user tries to use a function that doesn't exist
8. RemesPath function names can be used unquoted when they're not being called as functions. For example, `@.items` would have raised an error previously because `items` is a function name, but now there's no problem.
9. Fixed annoying dinging when using Tab key to navigate Find/replace form.

## [4.14.0] - 2023-04-12

### Added

1. __Lazy loading of tree view.__ This translates to a *massive* increase in responsiveness of the application. The tree viewer initially loads very quickly, but there is more latency expanding nodes that have not previously been expanded.
2. Even if not all children of the root get their own tree node, the children that do can be expanded.

### Changed

1. The `use_tree` setting has been removed. It is no longer necessary, because loading of tree nodes is deferred until the user clicks on the tree.
2. Very long arrays and objects besides the root are now affected by the `max_json_length_full_tree` setting. Previously, if the root was an array with one element, but one of its children was an array with 1 million elements, the tree view might have attempted to create a tree node for all 1 million elements, which would have caused insane latency and possibly consumption of all available memory. Now, a hypothetical non-root array with 1 million elements will get 10 thousand tree nodes, same as the root.

## [4.13.0] - 2023-04-11

### Added

1. __Automatic parsing and validation of JSON files__ every time the user stops acting for about 2 seconds after making a modification. This is disabled for large files, and can be disabled altogether if desired.

### Changed

1. `Settings->max_size_full_tree_MB` renamed to `Settings->max_file_size_MB_slow_actions` to better reflect the fact that it configures the maximum file size for *all* slow actions, including full tree, automatic use of the JSON lexer, and automatic parsing and validation.
2. 40-50% speedup in JSON compression and pretty-printing when the `sort_keys` setting is set to `true`. The new sorting algorithm has the side-effect of slightly changing how two keys that are the same ignoring case are sorted, but other than that the behavior is the same.

## [4.12.0] - 2023-03-28

### Added

1. Support for `$defs`, `definitions`, and `$ref` keywords for random JSON generation.
2. When JSON parsing fails, jump to the position where the parsing stopped.
3. Python-style `#` comments now supported with the `allow_comments` setting turned on.
4. Option to turn off images on tree viewer.
5. Non-empty arrays and objects now have `[length]` and `{length}` indicators of their lengths.
6. Support for `contains`, `minContains`, and `maxContains` keywords in JSON Schema *validation* of arrays.
7. Support for `minimum` and `maximum` keywords in JSON Schema validation *and* random generation.

### Changed

1. `allow_javascript_comments` setting name changed to `allow_comments` because Python-style `#` comments are now supported.

### Fixed

1. Removed stupid dinging sounds when using control keys (tab, space, enter, escape) while in the tree view. Address [issues #11](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/11) and [#10](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/10)
2. Making several UI elements on various forms slightly larger and adding auto-sizing where possible, hopefully addressing [issue #12](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/12).

## [4.11.2] - 2023-03-21

### Added

1. Added support for [pattern JSON Schema keyword](https://json-schema.org/draft/2020-12/json-schema-validation.html#name-pattern), allowing validation of strings against a regular expression. 
2. Added support for ["definitions", "$defs", and "$ref" JSON Schema keywords](https://json-schema.org/draft/2020-12/json-schema-core.html#name-schema-re-use-with-defs), allowing schema re-use and even validation of recursive self-referential schemas.
3. If auto-validation has been configured, it will now occur whenever a file is saved or renamed as well as when it is opened.
4. JSON schema validation is significantly faster due to optimizations, pre-compilation of schemas into validation functions, and caching of schemas to avoid unnecessary reads from disk.
5. Improvements to the `JSON from files and APIs` form (address [#32](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/32)):
	- URLs can now be entered into the URLs box as a JSON array or one per line as before. This could be helpful if there is a simple pattern in how the URLs are constructed and you want to use e.g. Remespath to build a list of URLs.
	- The last 10 URLs searched are now remembered, and the URLs box is populated with them at startup.

## [4.11.1] - 2023-03-17

### Fixed

1. [Bug](https://github.com/molsonkiko/JsonToolsNppPlugin/issues/30) where users who didn't have a JsonTools directory in the config folder would get a plugin crash on startup. Now a config subdirectory is automatically created if it doesn't already exist. This bug also existed with the `JSON from files and APIs` form, but now it has been solved in both places.

## [4.11.0] - 2023-03-15

### Fixed

1. Removed auto-installation of DSON UDL for Notepad++ older than `8.0`, because the UDL doesn't work anyway and the installation process causes an error message.

### Changed

1. [Find/replace form](/docs/README.md#find-and-replace-form) no longer requires a perfect match to the string to be found in order to perform a replacement when regular expressions are turned off. For example, find/replace searching for `M` and replacing with `Z` (regular expressions *off*) in the array `["MOO", "BOO"]` would previously have left the array unchanged, but now will change it into `["ZOO", "BOO"]`.
2. [DSON](/docs/README.md#dson) is now sneakily hidden!

### Added

1. [Auto-validation of JSON files against JSON schema](/docs/README.md#automatic-validation-of-json-against-json-schema).
2. To accommodate people who still want exact matching without having to write a regular expression, a `Match exactly?` checkbox has been added to the find/replace form. This is disabled whenever the `regular expression` box is checked, and vice versa.

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