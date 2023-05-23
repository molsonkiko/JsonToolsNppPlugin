using System.ComponentModel;
using JSON_Tools.JSON_Tools;
using CsvQuery.PluginInfrastructure; // for SettingsBase

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
        [Description("The indentation between levels of JSON when pretty-printing"),
            Category("JSON formatting"), DefaultValue(4)]
        public int indent_pretty_print { get; set; }

        [Description("If true, using the 'Compress JSON' plugin command will remove ALL unnecessary whitespace from the JSON. Otherwise, it will leave after the colon in objects and after the comma in both objects and arrays"),
            Category("JSON formatting"), DefaultValue(true)]
        public bool minimal_whitespace_compression { get; set; }

        [Description("Sort the keys of objects alphabetically when pretty-printing or compressing"),
            Category("JSON formatting"), DefaultValue(true)]
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
            "}\r\n\r\n" +
            "Whitesmith style:\r\n" +
            "{\r\n" +
            "\"a\":\r\n" +
            "    [\r\n" +
            "    1,\r\n" +
            "        [\r\n" +
            "        2\r\n" +
            "        ]\r\n" +
            "    ]\r\n" +
            "}"),
            Category("JSON formatting"), DefaultValue(PrettyPrintStyle.Google)]
        public PrettyPrintStyle pretty_print_style { get; set; }
        #endregion

        #region MISCELLANEOUS
        [Description("The style of key to use when getting the path or key/index of a node or line"),
            Category("Miscellaneous"), DefaultValue(KeyStyle.RemesPath)]
        public KeyStyle key_style { get; set; }
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
    }
}
