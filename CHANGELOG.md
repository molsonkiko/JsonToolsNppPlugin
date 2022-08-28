# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).
 
## [Unreleased] - yyyy-mm-dd
 
### To Be Added

This project has many features that were implemented in a [standalone app](https://github.com/molsonkiko/JSON-Tools) requiring .NET 6.0. Those features will be rolled out in the plugin over the next couple of weeks.

1. RemesPath query language for pandas-style querying of JSON. Similar in concept to [JMESPath](https://jmespath.org/), but with added functionality including regular expressions and recursive search. Query execution speed appears to be comparable to Python's pandas.
    * See the [language specification.
    * See RemesPath.cs, RemesPathFunctions.cs, RemesPathLexer.cs
2. A tool for searching directories for many documents and using RemesPath to query them all in parallel. The query results can then be written to separate files.
    * This tool will also probably have a feature for sending API requests and getting the JSON directly for querying. No more writing scripts!
3. Function (see JsonTabularize.BuildTable) for converting JSON into tabular (arrays of objects) form, and outputting JSON in this form as a delimiter-separated-variables (e.g., CSV, TSV) file 
(see [JsonTabularize.TableToCsv](/JsonToolsNppPlugin/JSONTools/JsonTabularize.cs)).
4. A YAML dumper that dumps valid equivalent YAML for *most* (but *not all*) JSON (see [YamlDumper.cs](/JsonToolsNppPlugin/JSONTools/YamlDumper.cs)). Most likely to have trouble with keys that contain colons or double quotes, and also values that contain newlines.
 
### To Be Changed

- Add parsing of unquoted strings when linter is active.
	(would this cause too much of a performance hit?)
- Add RemesPath functions:
	- for converting JSON to tabular form
	- for dates and datetimes (e.g., a `datediff` function that creates
	somthing like a Python TimeDelta that you can add to DateTimes and Dates)
 
### To Be Fixed

- JsonSchema has some bugs in the ordering of types. Non-impactful, I think. For example, a type list might come out as `["string", "integer"]` rather than `["integer", "string"]`.
- Remove bug in determination of "required" keys for JsonSchema
- Fix bugs in YamlDumper.cs:
	- fails when key contains quotes and colon
	- fails when value contains quotes and newline
	- fails when value contains quotes and colon
	- fails when key contains singlequotes and doublequotes
- Fix bug with the range() function where if the first or second argument is a uminus'd function of the CurJson there's an error because the uminus somehow maybe turned the int into a float(???). Error is raised on line 1706 of RemesPath.cs. E.g., `range(-len(@))` and `range(0, -len(@))`) will throw errors.


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