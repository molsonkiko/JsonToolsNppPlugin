﻿using System.ComponentModel;
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

        [Description("Ignore JavaScript comments in JSON. If false, comments cause the parser to error out."),
            Category("JSON Parser"), DefaultValue(false)]
        public bool allow_javascript_comments { get; set; }

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
            "Larger files get only the direct children of the root added to the tree."),
            Category("Tree View"), DefaultValue(4d)]
        public double max_size_full_tree_MB { get; set; }

        [Description("Whether or not to use the tree view at all."),
            Category("Tree View"), DefaultValue(true)]
        public bool use_tree { get; set; }
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

        [Description("How many threads to use for requesting JSON from remote APIs?"),
            Category("Grep and API requests"), DefaultValue(8)]
        public int max_api_request_threads { get; set; }
        #endregion
    }
}
