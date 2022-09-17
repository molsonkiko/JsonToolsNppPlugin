JSON Tools Overview
====================

This documentation will walk you through a typical use case of this application.

Consider the following JSON, hereafter called "silly_example.json".
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

We can open up the JSON tree viewer in the main menu by navigating Plugins -> JsonTools -> "Open JSON tree viewer". The keyboard shortcut `Ctrl+Alt+Shift+J` will also open the tree.

![JSON tree view immediately after creation](/docs/tree%20first%20view.PNG)

You can click on the nodes in that tree to see the children. When you do this, the caret will snap to the line of the node you've selected.

__NOTES__
1. If you submit a RemesPath query that is anything other than the default `@`, the JSON tree may no longer send the caret to the correct line.
2. If you [edit your JSON](/docs/RemesPath.md#assignment-expressions) with RemesPath queries and then undo your change with `Ctrl+Z` or similar, that will not undo the changes to the JSON. To re-sync the JSON with the document, you will have to close and then re-open the tree view.
3. For very large JSON (4+ MB) files, display of the full tree is disallowed by default, so you can only see the direct children of the root. You can change the maximum file size for full tree display in the settings.
4. You can disable the tree completely in the settings as well.

If a node has a `+` or `-` sign next to it, you can click on that button to expand the children of the node, as shown here.

![JSON viewer tree partially expanded](/docs/tree%20partially%20expanded.PNG)


You'll notice that icons appear next to the nodes in the tree. They are as follows:
* <span style="color:blue">Blue</span> square braces: __array__
* <span style="color:green">Green</span> curly braces: __object__
* Yin-yang symbol (half-black, half-white circle): __boolean__
* <span style="color:red">123</span>: __integer__ (represented by 64-bit integer)
* <span style="color:red">-3.5</span>: __float__ (represented by 64-bit floating point number)
* abc: __string__
* <span style="color:grey">grey</span> square: __null__

## Parser settings ##

By default, this app can parse a superset of JSON that is very slightly more permissive than the [original JSON specification](https://json.org). This app parses `NaN` as the floating point `Not-A-Number` and `Infinity` as the floating point Infinity.

You can change the settings to make the parser more or less inclusive. For example, the original spec doesn't allow strings to be surrounded in single quotes, nor does it allow JavaScript comments in the file. Thus, such JSON will cause our parser to throw an error.

![The default parser settings don't allow singlequoted strings or comments](/docs/json%20parser%20error%20due%20to%20singlequotes.PNG)

We can fix that in the settings.

![Change the parser settings to allow singlequotes and comments](/docs/json%20parser%20settings%20allow%20singlequotes%20and%20comments.PNG)

As you can see, you can also make the parser settings *stricter* than the default so that they don't accept the nonstandard NaN and Infinity. Just set `allow_nan_inf` to False.

## Viewing syntax errors in JSON ##

The `linting` attribute in Settings enables the built-in linter for the JSON parser, which catches various syntax errors in JSON and logs them.
When you parse a document that contains syntax errors like the one we saw above, you'll be asked if you want to see the syntax errors caught by the linter.

![Linter prompt after parsing error-ridden JSON document](/docs/prompt%20to%20view%20lint.PNG)

If you click "Yes", a new file will open in a separate tab containing details on all the syntax errors that were caught.

![Linter syntax error report](/docs/linter%20syntax%20error%20report.PNG)

**NOTE:** The JSON linter allows the parser to continue parsing even when it encounters syntax errors. That means that the parser will parse some documents that are not valid JSON until the syntax errors are corrected.

## RemesPath ##

The primary utility of this tool comes from the ability to query and edit the JSON using [RemesPath](RemesPath.md), which you can learn more about in the linked docs.

You can submit RemesPath queries in textbox above the tree, which by default has the `@` symbol in it.

![JSON viewer tree with RemesPath query](/docs/json%20viewer%20with%20remespath%20query.PNG)

Once you've submitted a query, you can use several other features of the JSON viewer.
First, you can open a new buffer containing the query result.

![JSON viewer open query result in new buffer](/docs/json%20viewer%20query%20save.PNG)

## JSON to CSV ##

Some JSON also has a somewhat __tabular__ format, such that it is amenable to conversion to a CSV file. The JSON in this example is a particularly simple case of this.

This app has a [form](/docs/json-to-csv.md) that allows conversion of such JSON to a tabular format. Remember that even if the JSON file as a whole can't be "tabularized" (or *can*, but you don't *want*  to), you can use a RemesPath query to select the part that you want to tabularize.

![JSON to CSV convertor](/docs/json%20viewer%20csv%20generator.PNG)

At present the __Strategy__ option for the CSV Generation form has four options. You can read more about these strategies in the [docs](/docs/json-to-csv.md).

# OTHER FEATURES NOT YET ADDED (COME BACK SOON!) #
======

## JSON Schema ##

You can also generate a [JSON schema](https://json-schema.org/) for your query result. Remember that if you want a schema for the whole file, you can just use the default `@` query to select the whole document.

This JSON schema will not be perfect, until I fix a known bug that causes the "required" attribute of object schemas to include the *union* of all keys in all objects belonging to that schema rather than the *intersection* as it should.

There are other bugs too, but I haven't yet diagnosed what causes them to appear.

![JSON schema generator](/docs/json%20viewer%20schema%20generator.PNG)

# Get JSON from files and APIs #

Sometimes it is useful to work with many JSON files at a time. For this purpose, we created a tool for *grepping* (searching for certain kinds of files in a directory, possibly recursively) for JSON files in a local directory, and also for sending [REST API](https://www.redhat.com/en/topics/api/what-is-a-rest-api) requests to multiple URLs.

We can open this tool with a button in the bottom center.

![Open the JSON grepper/API requester with this button](/docs/json%20viewer%20json_from_files_and_apis%20button.PNG)

The tool looks like this:

![JSON grepper/API requester appearance](/docs/json_from_files_and_apis%20initial.PNG)

## Sending REST API requests ##

Perhaps the most useful attribute of this tool is its ability to connect to APIs and extract useful data without the user needing to write a script.

**WARNING!!!** Before sending API requests, make sure you understand the correct way to format the URL, what type of JSON you expect to be getting, etc. *This tool has not been tested on private APIs*, so you should expect it to fail unless you can incorporate your API key and other authentication information into the URL.

Here's an example of what you get with successful request(s):

![JSON grepper/API requester successful API requests](/docs/json_from_files_and_apis%20api%20good%20url%20result.PNG)

The results automatically populate the tree on the left, and a list of files and urls shows where the tool has gotten JSON from.

Of course, sometimes you'll send a bad request.

![JSON grepper/API requester bad API request](/docs/json_from_files_and_apis%20api%20bad%20url%20error.PNG)

*As long as you keep clicking **OK**,* an error message like the one shown above will show up for every URL where the request failed for whatever reason, until there are no more bad URLs. If you click **Cancel**, no more messages will show up.

## Getting JSON from local directories ##

If you want to open up all the JSON files in a directory, look to the bottom center left. There you can customize what type of [filename search pattern](https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefiles?view=net-6.0#system-io-directoryinfo-enumeratefiles) you want to use (by default files with the `.json` extension), choose whether to recursively search in subdirectories (false by default), and finally search for files using the settings you chose.

![JSON grepper/API requester search JSON in local directories](/docs/json_from_files_and_apis%20get%20json%20in%20directory.PNG)

## Querying searched JSON with RemesPath ##

Just as in the single-file client, we can execute RemesPath queries. When you enter a query and click the `Execute query` button, the query will run on *all* of the JSON documents that the tool has found.

![JSON grepper/API requester remespath query all JSONs](/docs/json_from_files_and_apis%20enter%20remespath%20query.PNG)

You can then save your query results to files. For each query result you have, a prompt will pop up, and you can choose if and how you want to save the file.

![JSON grepper/API requester remespath query save to files](/docs/json_from_files_and_apis%20save%20query%20to%20file.PNG)

Note that the title of the file-saving box tells you which file you're saving the query result for, so that you can decide how to name the new file.

## Clearing previously searched JSON ##

Maybe you've decided that you no longer want to look at the JSON you've already searched, and you want to try a different search. You can do this by clicking the `Clear all files` button in the bottom left.

![JSON grepper/API requester clear all files](/docs/json_from_files_and_apis%20clear%20all%20files.PNG)

This will clear out all memory of files you found, the associated JSON, and any queries you made.

## Clearing only selected files ##

If you like most of the JSON documents you've found but you don't want to keep *all* of the files, you can select some of them, and then click the `Remove selected files` button in the bottom right center.

![JSON grepper/API requester remove selected files button BEFORE removal](/docs/json_from_files_and_apis%20remove%20selected%20files%20BEFORE.PNG)

After you click the button, those JSON documents will be removed.

![JSON grepper/API requester remove selected files button AFTER removal](/docs/json_from_files_and_apis%20remove%20selected%20files%20AFTER.PNG)
