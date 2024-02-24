using System.ComponentModel;
using JSON_Tools.JSON_Tools;
using CsvQuery.PluginInfrastructure; // for SettingsBase
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Utils
{
    /// <summary>
    /// Manages application settings
    /// </summary>
    public class Settings : SettingsBase
    {
        #region JSON_PARSER_SETTINGS
        [Description("Suppress logging of errors at or below this level.\r\n" +
            "STRICT: Log all deviations from the original JSON spec.\r\n" + 
            "OK: The original JSON spec plus the following:\r\n" +
            "    * strings can contain characters with ASCII values less than 0x20 (includes '\\t')\r\n" +
            "NAN_INF: Do not log errors when NaN, Infinity, and -Infinity are parsed.\r\n" +
            "JSONC: The following errors are not logged:\r\n" +
            "    * JavaScript single-line '//' and multi-line '/*...*/' comments\r\n" +
            "    * NaN and +/-Infinity\r\n" +
            "JSON5: Everything in the JSONC and NAN_INF levels is not logged, as well as the following:\r\n" +
            "    * singlequoted strings\r\n" +
            "    * commas after the last element of an array or object\r\n" +
            "    * unquoted object keys\r\n" +
            "    * see https://json5.org/ for more."
            ),
            Category("JSON Parser"), DefaultValue(LoggerLevel.NAN_INF)]
        public LoggerLevel logger_level { get; set; }

        [Description("When the document is parsed, show a prompt to see syntax errors in the document."),
            Category("JSON Parser"), DefaultValue(true)]
        public bool offer_to_show_lint { get; set; }

        [Description("Parse \"yyyy-mm-dd dates\" and \"yyyy-MM-dd hh:mm:ss.sss\" datetimes as the appropriate type."),
            Category("JSON Parser"), DefaultValue(false)]
        public bool allow_datetimes { get; set; }
        #endregion
        #region PERFORMANCE
        [Description("Files larger than this number of megabytes have the following slow actions DISABLED by default:\r\n" +
            "* Automatically turning on the JSON lexer.\r\n" +
            "* Automatic parsing of the file on opening and approximately 2 seconds after every edit."),
            Category("Performance"), DefaultValue(4d)]
        public double max_file_size_MB_slow_actions { get; set; }

        [Description("Automatically validate .json, .jsonc, and .jsonl files every 2 seconds, except very large files"),
            Category("Performance"), DefaultValue(false)]
        public bool auto_validate { get; set; }

        [Description("How many seconds of user inactivity before the plugin re-parses the document. Minimum 1."),
            Category("Performance"), DefaultValue(2)]
        public int inactivity_seconds_before_parse { get; set; }
        #endregion
        #region TREE_VIEW_SETTINGS

        [Description("The longest length of a JSON array or object that gets all its children added to the tree view. " +
            "Longer iterables get only some of their children added to the tree."),
            Category("Tree View"), DefaultValue(10_000)]
        public int max_json_length_full_tree { get; set; }

        [Description("Should each node in the tree have an image associated with its type?"),
            Category("Tree View"), DefaultValue(true)]
        public bool tree_node_images { get; set; }
        #endregion

        #region JSON_FORMATTING_SETTINGS
        [Description("The number of spaces between levels of JSON when pretty-printing"),
            Category("JSON formatting"), DefaultValue(4)]
        public int indent_pretty_print { get; set; }

        [Description("Use one horizontal tab ('\\t') instead of spaces between levels of JSON when pretty-printing"),
            Category("JSON formatting"), DefaultValue(false)]
        public bool tab_indent_pretty_print { get; set; }

        [Description("If true, using the 'Compress JSON' plugin command will remove ALL unnecessary whitespace from the JSON. Otherwise, it will leave after the colon in objects and after the comma in both objects and arrays"),
            Category("JSON formatting"), DefaultValue(true)]
        public bool minimal_whitespace_compression { get; set; }

        [Description("Sort the keys of objects alphabetically when pretty-printing or compressing"),
            Category("JSON formatting"), DefaultValue(false)]
        public bool sort_keys { get; set; }

        [Description("How JSON is pretty printed.\r\n" +
            "Google style (default):\r\n" +
            "{\r\n" +
            "    \"a\": [\r\n" +
            "        1,\r\n" +
            "        [\r\n" +
            "            2\r\n" +
            "        ]\r\n" +
            "    ]\r\n" +
            "}\r\n" +
            "Whitesmith style:\r\n" +
            "{\r\n" +
            "\"a\":\r\n" +
            "    [\r\n" +
            "    1,\r\n" +
            "        [\r\n" +
            "        2\r\n" +
            "        ]\r\n" +
            "    ]\r\n" +
            "}\r\n" +
            "PPrint style:\r\n" +
            "{\r\n    \"algorithm\": [\r\n        [\"start\", \"each\", \"child\", \"on\", \"a\", \"new\", \"line\"],\r\n        [\"if\", \"the\", \"line\", \"would\", \"have\", \"length\", \"at\", \"least\", 80],\r\n        [\r\n            \"follow\",\r\n            \"this\",\r\n            \"algorithm\",\r\n            [\"starting\", \"from\", \"the\", \"beginning\"]\r\n        ],\r\n        [\"else\", \"print\", \"it\", \"out\", \"on\", 1, \"line\"]\r\n    ],\r\n    \"style\": \"PPrint\",\r\n    \"useful\": true\r\n}"),
            Category("JSON formatting"), DefaultValue(PrettyPrintStyle.Google)]
        public PrettyPrintStyle pretty_print_style { get; set; }

        [Description("When JSON is pretty-printed or compressed, any comments found when it was last parsed are included.\r\n" +
                     "For logistical reasons, the user-selected pretty_print_style value is ignored if this is true, and Google style will always be used.\r\n" +
                     "When pretty-printing, each comment will have the same relative location to each JSON element as when it was parsed.\r\n" +
                     "When compressing, all comments will come at the beginning of the document."
                    ),
            Category("JSON formatting"), DefaultValue(false)]
        public bool remember_comments { get; set; }

        [Description("Ask before pretty-printing JSON Lines documents, ignore requests to pretty-print, or pretty-print without asking?"),
            Category("JSON formatting"), DefaultValue(AskUserWhetherToDoThing.ASK_BEFORE_DOING)]
        public AskUserWhetherToDoThing ask_before_pretty_printing_json_lines { get; set; }
        #endregion

        #region MISCELLANEOUS
        [Description("The style of key to use when getting the path or key/index of a node or line"),
            Category("Miscellaneous"), DefaultValue(KeyStyle.RemesPath)]
        public KeyStyle key_style { get; set; }

        [Description("When selecting every JSON in the file, start trying to parse only at these characters.\r\n" +
                     "Only JSON valid according to the NAN_INF logger_level is tolerated.\r\n" +
                     "Example: if \"[{ are chosen (default), we consider only potential strings, arrays, and objects.\r\n" +
                     "If \"[{tf are chosen, we consider potential strings, arrays, objects, and booleans."),
            Category("Miscellaneous"), DefaultValue("\"[{")]
        public string try_parse_start_chars { get; set; }

        [Description("When running tests, skip the tests that send requests to APIs and the RemesPath fuzz tests"),
            Category("Miscellaneous"), DefaultValue(true)]
        public bool skip_api_request_and_fuzz_tests { get; set; }

        [Description("Which type of newline to use for generated CSV files."),
            Category("Miscellaneous"), DefaultValue(EndOfLine.LF)]
        public EndOfLine csv_newline { get; set; }

        [Description("Specify one of these chars for each toolbar icon you want to show, in the order you want:\r\n" +
                    "('t' = tree view, 'c' = compress, 'p' = pretty-print, 'o' = path to current position)\r\n" +
                    "This setting will take effect the next time you start Notepad++.\r\n" +
                    "If you want there to be NO toolbar icons, enter a character that does not represent an icon; do NOT leave this field empty."),
            Category("Miscellaneous"), DefaultValue("tcpo")]
        public string toolbar_icons { get; set; }

        [Description("If this setting is true,\r\n" +
            "when the regex search form is opened, or when the \"Parse as CSV?\" checkbox in that form is toggled on,\r\n" +
            "JsonTools will attempt to guess whether the current document is a CSV or TSV file, and how many columns and what newline it has.\r\n" +
            "The regex search form will take slightly longer to open if this is true."),
            Category("Miscellaneous"), DefaultValue(false)]
        public bool auto_try_guess_csv_delim_newline { get; set; }
        #endregion

        #region GREP_API_SETTINGS
        [Description("How many threads to use for parsing JSON files obtained by JsonGrep and API requester"),
            Category("Grep and API requests"), DefaultValue(4)]
        public int max_threads_parsing { get; set; }
        #endregion

        #region STYLING
        [Description("Use the same colors as the editor window for the tree viewer?"),
            Category("Styling"), DefaultValue(true)]
        public bool use_npp_styling { get; set; }
        #endregion

        #region RANDOM_JSON_SETTINGS
        [Description("Minimum length of random arrays, unless otherwise specified by the \"minItems\" keyword"),
            Category("Random JSON"), DefaultValue(0)]
        public int minArrayLength { get; set; }
        
        [Description("Maximum length of random arrays, unless otherwise specified by the \"maxItems\" keyword"),
            Category("Random JSON"), DefaultValue(10)]
        public int maxArrayLength { get; set; }

        [Description("Use extended ASCII characters (e.g., \x0b, \xf1) in strings?"),
            Category("Random JSON"), DefaultValue(false)]
        public bool extended_ascii_strings { get; set; }
        #endregion

        #region JSON_SCHEMA_SETTINGS
        [Description("Maximum number of JSON Schema validation problems to log before the validator stops"),
            Category("JSON Schema"), DefaultValue(64)]
        public int max_schema_validation_problems { get; set; }
        #endregion // JSON_SCHEMA_SETTINGS
    }
}
