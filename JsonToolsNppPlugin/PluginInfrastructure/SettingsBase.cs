/*
Utilities for storing, viewing, and updating the settings of the plugin.
Modified from https://raw.githubusercontent.com/BdR76/CSVLint/master/CSVLintNppPlugin/PluginInfrastructure/SettingsBase.cs
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace CsvQuery.PluginInfrastructure
{
    public class SettingsBase
    {
        private const int DEFAULT_WIDTH = 400;
        private const int DEFAULT_HEIGHT = 500;

        private static readonly string IniFilePath;

        /// <summary> Delegate for update events </summary>
        public delegate void RepositoryEventHandler(object sender, SettingsChangedEventArgs e);

        /// <summary> Raised before settings has been changed, allowing listeners to cancel the change </summary>
        public event RepositoryEventHandler ValidateChanges;

        /// <summary> Raised after a setting has been changed </summary>
        public event RepositoryEventHandler SettingsChanged;

        /// <summary> Overridable event logic </summary>
        protected virtual void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            SettingsChanged?.Invoke(sender, e);
        }

        /// <summary> Overridable event logic </summary>
        protected virtual bool OnValidateChanges(object sender, SettingsChangedEventArgs e)
        {
            ValidateChanges?.Invoke(sender, e);
            return !e.Cancel;
        }

        static SettingsBase()
        {
            IniFilePath = Path.Combine(Npp.notepad.GetConfigDirectory(), Main.PluginName, Main.PluginName + ".ini");
        }

        /// <summary>
        /// By default loads settings from the default N++ config folder
        /// </summary>
        /// <param name="loadFromFile"> If false will not load anything and have default values set </param>
        public SettingsBase(bool loadFromFile = true)
        {
            // Set defaults
            Translator.LoadTranslations();
            foreach (var propertyInfo in GetType().GetProperties())
            {
                if (propertyInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() is DefaultValueAttribute def)
                {
                    propertyInfo.SetValue(this, def.Value, null);
                }
            }
            if (loadFromFile && !ReadFromIniFile())
                SaveToIniFile();
        }

        /// <summary>
        /// Reads all (existing) settings from an ini-file
        /// </summary>
        /// <param name="filename">File to write to (default is N++ plugin config)</param>
        /// <returns>False if the file did not exist, or if not all values in the file were valid.<br></br>
        /// True otherwise.</returns>
        public bool ReadFromIniFile(string filename = null)
        {
            filename = filename ?? IniFilePath;
            if (!File.Exists(filename))
                return false;

            // Load all sections from file
            var loaded = GetType().GetProperties()
                .Select(x => ((CategoryAttribute)x.GetCustomAttributes(typeof(CategoryAttribute), false).FirstOrDefault())?.Category ?? "General")
                .Distinct()
                .ToDictionary(section => section, section => GetKeys(filename, section));

            //var loaded = GetKeys(filename, "General");
            bool allConvertedCorrectly = true;
            foreach (var propertyInfo in GetType().GetProperties())
            {
                var category = ((CategoryAttribute)propertyInfo.GetCustomAttributes(typeof(CategoryAttribute), false).FirstOrDefault())?.Category ?? "General";
                var name = propertyInfo.Name;
                if (loaded.ContainsKey(category) && loaded[category].ContainsKey(name) && !string.IsNullOrEmpty(loaded[category][name]))
                {
                    var rawString = loaded[category][name];
                    var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                    bool convertedCorrectly = false;
                    Exception ex = null;
                    if (converter.IsValid(rawString))
                    {
                        try
                        {
                            propertyInfo.SetValue(this, converter.ConvertFromInvariantString(rawString), null);
                            convertedCorrectly = true;
                        }
                        catch (Exception ex_)
                        {
                            ex = ex_;
                        }
                    }
                    if (!convertedCorrectly)
                    {
                        allConvertedCorrectly = false;
                        // use the default value for the property, since the config file couldn't be read in this case.
                        SetPropertyInfoToDefault(propertyInfo);
                        if (ex is null)
                        {
                            Translator.ShowTranslatedMessageBox(
                                "While parsing JsonTools config file, expected setting \"{0}\" to be type {1}, but got an error.\r\nThat setting was set to its default value of {2}.\r\nThe given value {3} could not be converted for an unknown reason.",
                                "Unknown error while parsing JsonTools config file",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                4, name, propertyInfo.PropertyType.Name, propertyInfo.GetValue(this, null), rawString
                            );
                        }
                        else
                        {
                            Translator.ShowTranslatedMessageBox(
                                "While parsing JsonTools config file, expected setting \"{0}\" to be type {1}, but got an error.\r\nThat setting was set to its default value of {2}.\r\nThe given value {3} raised the following error:\r\n{4}",
                                "Error while parsing JsonTools config file",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                5, name, propertyInfo.PropertyType.Name, propertyInfo.GetValue(this, null), rawString, ex
                            );
                        }
                    }
                }
            }
            return allConvertedCorrectly;
        }

        /// <summary>
        /// if the PropertyInfo does not have a default value, return false.<br></br>
        /// Otherwise, set the PropertyInfo's value for this object to the default value, and return true.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private bool SetPropertyInfoToDefault(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() is DefaultValueAttribute def)
            {
                propertyInfo.SetValue(this, def.Value, null);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Saves all settings to an ini-file, under "General" section
        /// </summary>
        /// <param name="filename">File to write to (default is N++ plugin config)</param>
        public void SaveToIniFile(string filename = null)
        {
            filename = filename ?? IniFilePath;
            var dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Win32.WritePrivateProfileSection (that NppPlugin uses) doesn't work well with non-ASCII characters. So we roll our own.
            using (var fp = new StreamWriter(filename, false, Encoding.UTF8))
            {
                fp.WriteLine("; {0} settings file", Main.PluginName);

                foreach (var section in GetType()
                    .GetProperties()
                    .GroupBy(x => ((CategoryAttribute)x.GetCustomAttributes(typeof(CategoryAttribute), false)
                                        .FirstOrDefault())?.Category ?? "General"))
                {
                    fp.WriteLine(Environment.NewLine + "[{0}]", section.Key);
                    foreach (var propertyInfo in section.OrderBy(x => x.Name))
                    {
                        string description = Translator.TranslateSettingsDescription(propertyInfo);
                        fp.WriteLine("; " + description.Replace(Environment.NewLine, Environment.NewLine + "; "));
                        var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                        fp.WriteLine("{0}={1}", propertyInfo.Name, converter.ConvertToInvariantString(propertyInfo.GetValue(this, null)));
                    }
                }
            }
        }

        /// <summary>
        /// Read a section from an ini-file
        /// </summary>
        /// <param name="iniFile">Path to ini-file</param>
        /// <param name="category">Section to read</param>
        private Dictionary<string, string> GetKeys(string iniFile, string category)
        {
            var buffer = new byte[8 * 1024];

            Win32.GetPrivateProfileSection(category, buffer, buffer.Length, iniFile);
            var tmp = Encoding.UTF8.GetString(buffer).Trim('\0').Split('\0');
            return tmp.Select(x => x.Split(new[] { '=' }, 2))
                .Where(x => x.Length == 2)
                .ToDictionary(x => x[0], x => x[1]);
        }

        /// <summary>
        /// Opens a window that edits all settings
        /// </summary>
        public void ShowDialog(bool debug = false)
        {
            // We bind a copy of this object and only apply it after they click "Ok"
            var copy = (Settings)MemberwiseClone();

            //// check the current settings
            //var settingsSb = new StringBuilder();
            //foreach (System.Reflection.PropertyInfo p in GetType().GetProperties())
            //{
            //    settingsSb.Append(p.ToString());
            //    settingsSb.Append($": {p.GetValue(this)}");
            //    settingsSb.Append(", ");
            //}
            //MessageBox.Show(settingsSb.ToString());

            var dialog = new Form
            {
                Name = "SettingsForm",
                Text = "Settings - JsonTools plug-in",
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
                        Location = new Point(DEFAULT_WIDTH - 135, DEFAULT_HEIGHT - 36),
                        UseVisualStyleBackColor = true
                    },
                    new Button
                    {
                        Name = "Reset",
                        Text = "&Reset",
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                        Size = new Size(75, 23),
                        Location = new Point(DEFAULT_WIDTH - 232, DEFAULT_HEIGHT - 36),
                        UseVisualStyleBackColor = true
                    },
                    new Button
                    {
                        Name = "Ok",
                        Text = "&Ok",
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                        Size = new Size(75, 23),
                        Location = new Point(DEFAULT_WIDTH - 330, DEFAULT_HEIGHT - 36),
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
            Translator.TranslateForm(dialog);

            dialog.Controls["Cancel"].Click += (a, b) => dialog.Close();
            dialog.Controls["Ok"].Click += (a, b) =>
            {
                // change the settings to whatever the user selected
                var changesEventArgs = new SettingsChangedEventArgs((Settings)this, copy);
                if (!changesEventArgs.Changed.Any())
                {
                    dialog.Close();
                    return;
                }
                copy.SaveToIniFile();
                foreach (var propertyInfo in GetType().GetProperties())
                {
                    var oldValue = propertyInfo.GetValue(this, null);
                    var newValue = propertyInfo.GetValue(copy, null);
                    if (!oldValue.Equals(newValue))
                    {
                        try
                        {
                            propertyInfo.SetValue(this, newValue, null);
                        }
                        catch (Exception ex)
                        {
                            Translator.ShowTranslatedMessageBox(
                                "Could not change setting {0} to value {1}, so it will remain set as {2}.\r\nGot the following exception:\r\n{3}",
                                "Invalid value for setting {0}",
                                MessageBoxButtons.OK, MessageBoxIcon.Error,
                                4, propertyInfo.Name, newValue, oldValue, ex, propertyInfo.Name);
                        }
                    }
                }
                dialog.Close();
            };
            dialog.Controls["Reset"].Click += (a, b) =>
            {
                // reset the settings to defaults
                foreach (var propertyInfo in GetType().GetProperties())
                {
                    SetPropertyInfoToDefault(propertyInfo);
                }
                SaveToIniFile();
                dialog.Close();
            };
            // close dialog on pressing Escape (this doesn't work if a grid cell is selected, but it does work if a button is selected)
            KeyEventHandler keyDownHandler = (a, b) =>
            {
                if (b.KeyCode == Keys.Escape)
                    dialog.Close();
            };
            dialog.KeyDown += keyDownHandler;
            foreach (Control ctrl in dialog.Controls)
                ctrl.KeyDown += keyDownHandler;
            // translate the descriptions of the settings
            var grid = dialog.Controls["Grid"];
            if (Translator.HasTranslations
                && grid.Controls.Count >= 1 && grid.Controls[0] is Control commentPane
                && commentPane.Controls.Count >= 2 && commentPane.Controls[1] is Label descriptionLabel)
            {
                string translatedDescription = "";
                var propGrid = (PropertyGrid)grid;
                propGrid.SelectedGridItemChanged += (object _, SelectedGridItemChangedEventArgs e) =>
                {
                    GridItem selectedItem = e.NewSelection;
                    PropertyDescriptor selectedPropertyDesc = selectedItem?.PropertyDescriptor;
                    if (selectedPropertyDesc is null)
                        return;
                    PropertyInfo selectedProp = GetType().GetProperty(selectedPropertyDesc.Name);
                    translatedDescription = Translator.TranslateSettingsDescription(selectedProp);
                    if (translatedDescription.Length > 0)
                        descriptionLabel.Text = translatedDescription;
                };
                commentPane.SizeChanged += (object _, EventArgs e) =>
                {
                    if (translatedDescription.Length > 0)
                        descriptionLabel.Text = translatedDescription;
                };
            }
            dialog.ShowDialog();
        }

        /// <summary> Opens the config file directly in Notepad++ </summary>
        public void OpenFile()
        {
            if (!File.Exists(IniFilePath)) SaveToIniFile();
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DOOPEN, 0, IniFilePath);
        }
    }

    public class SettingsChangedEventArgs : CancelEventArgs
    {
        public HashSet<string> Changed { get; }
        public Settings OldSettings { get; }
        public Settings NewSettings { get; }

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
    }
}
