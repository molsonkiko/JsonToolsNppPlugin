# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).
 
## [Unreleased] - yyyy-mm-dd
 
### To Be Added

This project has many features that were implemented in a [standalone app](https://github.com/molsonkiko/JSON-Tools) requiring .NET 6.0. Those features will be rolled out in the plugin over the next couple of weeks.

1. A tree view for JSON (see the docs for what it will look like)
2. RemesPath query language for pandas-style querying of JSON. Similar in concept to [JMESPath](https://jmespath.org/), but with added functionality including regular expressions and recursive search. Query execution speed appears to be comparable to Python's pandas.
    * See the [language specification.
    * See RemesPath.cs, RemesPathFunctions.cs, RemesPathLexer.cs
3. A tool for searching directories for many documents and using RemesPath to query them all in parallel. The query results can then be written to separate files.
    * This tool will also probably have a feature for sending API requests and getting the JSON directly for querying. No more writing scripts!
4. Function (see JsonTabularize.BuildTable) for converting JSON into tabular (arrays of objects) form, and outputting JSON in this form as a delimiter-separated-variables (e.g., CSV, TSV) file 
(see [JsonTabularize.TableToCsv](/JsonToolsNppPlugin/JSONTools/JsonTabularize.cs)).
5. A YAML dumper that dumps valid equivalent YAML for *most* (but *not all*) JSON (see [YamlDumper.cs](/JsonToolsNppPlugin/JSONTools/YamlDumper.cs)). Most likely to have trouble with keys that contain colons or double quotes, and also values that contain newlines.
 
### To Be Changed

- Reduce slowness of removing selected files by not refreshing
	entire TreeViews when they're removed.
- Add parsing of unquoted strings when linter is active.
	(would this cause too much of a performance hit?)
- Add RemesPath functions:
	- for converting JSON to tabular form
	- for dates and datetimes (e.g., a `datediff` function that creates
	somthing like a Python TimeDelta that you can add to DateTimes and Dates)
- Add multithreading for parsing of JSON strings returned by API requests and files found by grepping.
 
### To Be Fixed

- A whole bunch of problems with RemesPath that were caused by refactoring the code from .NET 6.0 to .NET 4.0.
- PrettyPrintAndChangeLineNumbers doesn't currently give the right line numbers
- Some error messages incorrectly give the length of the document as the position of the error rather than where the error actually occurred.
- JsonSchema has some bugs in the ordering of types. Non-impactful, I think.
- JsonTabularizer has several bugs originating from the porting from .NET 4.0.
- Remove bug in determination of "required" keys for JsonSchema
- Fix bugs in YamlDumper.cs:
	- fails when key contains quotes and colon
	- fails when value contains quotes and newline
	- fails when value contains quotes and colon
	- fails when key contains singlequotes and doublequotes
- Fix occasional bug where sometimes writing trying to overwrite an existing file when writing errors to file from the LintForm throws an unhandled exception because the old process was not closed.
- Make the GitHub action work.
- Fix bug with the range() function where if the first or second argument is a uminus'd function of the CurJson there's an error because the uminus somehow maybe turned the int into a float(???). Error is raised on line 1706 of RemesPath.cs. E.g., `range(-len(@))` and `range(0, -len(@))`) will throw errors.


## [1.0.0] - 2022-08-26

### Added

- Contains only the minimum functionality required to be useful. Linting syntax errors, changing parser settings to allow comments and whatnot, etc.