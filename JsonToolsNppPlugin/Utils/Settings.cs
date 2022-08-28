using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JSON_Tools.Utils
{
    /// <summary>
    /// Manages application settings
    /// </summary>
    public class Settings
    {
        private const int DEFAULT_WIDTH = 350;
        private const int DEFAULT_HEIGHT = 450;

        // private static readonly string IniFilePath;

        // JSON Parser settings
        [Description("Parse NaN and Infinity in JSON. If false, those raise an error."),
            Category("JSON Parser")] //, DefaultValue(true)]
        public bool allow_nan_inf { get; set; } = true;

        [Description("Ignore JavaScript comments in JSON. If false, comments cause the parser to error out."),
            Category("JSON Parser")] //, DefaultValue(false)]
        public bool allow_javascript_comments { get; set; } = false;

        [Description("Allow use of ' as well as \" for quoting strings."),
            Category("JSON Parser")] //, DefaultValue(false)]
        public bool allow_singlequoted_str { get; set; } = false;

        [Description("Parse \"yyyy-mm-dd dates\" and \"yyyy-MM-dd hh:mm:ss.sss\" datetimes as the appropriate type."),
            Category("JSON Parser")] //, DefaultValue(false)]
        public bool allow_datetimes { get; set; } = false;

        [Description("Track the locations where any JSON syntax errors were found"),
            Category("JSON Parser")] //, DefaultValue(false)]
        public bool linting { get; set; } = false;


        /// <summary>
        /// Opens a window that edits all settings
        /// </summary>
        public void ShowDialog(bool debug=false)
        {
            // We bind a copy of this object and only apply it after they click "Ok"
            var copy = (Settings)MemberwiseClone();
            
            //// check the current settings
            //var settings_sb = new StringBuilder();
            //foreach (System.Reflection.PropertyInfo p in GetType().GetProperties())
            //{
            //    settings_sb.Append(p.ToString());
            //    settings_sb.Append($": {p.GetValue(this)}");
            //    settings_sb.Append(", ");
            //}
            //MessageBox.Show(settings_sb.ToString());

            var dialog = new Form
            {
                Text = "Settings - JSON Viewer plug-in",
                ClientSize = new Size(DEFAULT_WIDTH, DEFAULT_HEIGHT),
                MinimumSize = new Size(250, 250),
                ShowIcon = false,
                AutoScaleMode = AutoScaleMode.Font,
                AutoScaleDimensions = new SizeF(6F, 13F),
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent,
                Controls =
                {
                    new Button
                    {
                        Name = "Cancel",
                        Text = "&Cancel",
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                        Size = new Size(75, 23),
                        Location = new Point(DEFAULT_WIDTH - 115, DEFAULT_HEIGHT - 36),
                        UseVisualStyleBackColor = true
                    },
                    new Button
                    {
                        Name = "Reset",
                        Text = "&Reset",
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                        Size = new Size(75, 23),
                        Location = new Point(DEFAULT_WIDTH - 212, DEFAULT_HEIGHT - 36),
                        UseVisualStyleBackColor = true
                    },
                    new Button
                    {
                        Name = "Ok",
                        Text = "&Ok",
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                        Size = new Size(75, 23),
                        Location = new Point(DEFAULT_WIDTH - 310, DEFAULT_HEIGHT - 36),
                        UseVisualStyleBackColor = true
                    },
                    new PropertyGrid
                    {
                        Name = "Grid",
                        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                        Location = new Point(13, 13),
                        Size = new Size(DEFAULT_WIDTH - 13 - 13, DEFAULT_HEIGHT - 55),
                        AutoScaleMode = AutoScaleMode.Font,
                        AutoScaleDimensions = new SizeF(6F,13F),
                        SelectedObject = copy
                    },
                }
            };

            dialog.Controls["Cancel"].Click += (a, b) => dialog.Close();
            dialog.Controls["Ok"].Click += (a, b) =>
            {
                // change the settings to whatever the user selected
                var changesEventArgs = new SettingsChangedEventArgs(this, copy);
                if (!changesEventArgs.Changed.Any())
                {
                    dialog.Close();
                    return;
                }
                foreach (var propertyInfo in GetType().GetProperties())
                {
                    var oldValue = propertyInfo.GetValue(this, null);
                    var newValue = propertyInfo.GetValue(copy, null);
                    if (!oldValue.Equals(newValue))
                        propertyInfo.SetValue(this, newValue, null);
                }
                dialog.Close();
            };
            dialog.Controls["Reset"].Click += (a, b) =>
            {
                // reset the settings to defaults
                allow_nan_inf = true;
                allow_javascript_comments = false;
                allow_singlequoted_str = false;
                allow_datetimes = false;
                linting = false;
                dialog.Close();
            };

            dialog.ShowDialog();
        }
    }



    public class SettingsChangedEventArgs : CancelEventArgs
    {
        public SettingsChangedEventArgs(Settings oldSettings, Settings newSettings)
        {
            OldSettings = oldSettings;
            NewSettings = newSettings;
            Changed = new HashSet<string>();
            foreach (var propertyInfo in typeof(Settings).GetProperties())
            {
                var oldValue = propertyInfo.GetValue(oldSettings, null);
                var newValue = propertyInfo.GetValue(newSettings, null);
                if (!oldValue.Equals(newValue))
                {
                    Trace.TraceInformation($"Setting {propertyInfo.Name} has changed");
                    Changed.Add(propertyInfo.Name);
                }
            }
        }

        public HashSet<string> Changed { get; }
        public Settings OldSettings { get; }
        public Settings NewSettings { get; }
    }
}
