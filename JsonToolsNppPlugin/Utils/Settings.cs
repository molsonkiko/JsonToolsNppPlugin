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
        [Description("Parse NaN and Infinity in JSON. If false, those raise an error."),
            Category("JSON Parser"), DefaultValue(true)]
        public bool allow_nan_inf { get; set; }

        [Description("Ignore comments ('#' Python-style comments or JavaScript-style '//' single-line and '/* */' multi-line) in JSON.\r\nIf false, comments cause the parser to error out."),
            Category("JSON Parser"), DefaultValue(false)]
        public bool allow_comments { get; set; }

        [Description("Allow use of ' as well as \" for quoting strings."),
            Category("JSON Parser"), DefaultValue(false)]
        public bool allow_singlequoted_str { get; set; }

        [Description("Parse \"yyyy-mm-dd dates\" and \"yyyy-MM-dd hh:mm:ss.sss\" datetimes as the appropriate type."),
            Category("JSON Parser"), DefaultValue(false)]
        public bool allow_datetimes { get; set; }

        [Description("Track the locations where any JSON syntax errors were found"),
            Category("JSON Parser"), DefaultValue(false)]
        public bool linting { get; set; }
        #endregion

        #region TREE_VIEW_SETTINGS
        [Description("The largest size in megabytes of a JSON file that gets its full tree added to the tree view. " +
            "Larger files get only the direct children of the root added to the tree. " +
            "Also, files bigger than this limit don't get colorized automatically."),
            Category("Tree View"), DefaultValue(4d)]
        public double max_size_full_tree_MB { get; set; }

        [Description("Whether or not to use the tree view at all."),
            Category("Tree View"), DefaultValue(true)]
        public bool use_tree { get; set; }

        [Description("The longest length of a JSON array or object that gets its full tree added to the tree view. " +
            "Larger files get only some of the direct children of the root added to the tree."),
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
            Category("JSON formatting"), DefaultValue(false)]
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
