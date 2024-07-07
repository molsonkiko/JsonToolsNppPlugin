using Kbg.NppPluginNET;
using JSON_Tools.JSON_Tools;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Kbg.NppPluginNET.PluginInfrastructure;
using System.Reflection;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Globalization;

namespace JSON_Tools.Utils
{
    public static class Translator
    {
        private static JObject translations = null;
        public static bool HasTranslations => !(translations is null); 

        public static void LoadTranslations()
        {
            string languageName = "english";
            // TODO: maybe use Notepad++ nativeLang preference to guide translation?
            // Possible references include:
            // * https://github.com/daddel80/notepadpp-multireplace/blob/65411ac5754878bbf8af7a35dba7b35d7d919ff4/src/MultiReplacePanel.cpp#L6347
            // * https://npp-user-manual.org/docs/binary-translation/

            //// first try to use the Notepad++ nativeLang.xml config file to determine the user's language preference
            //string nativeLangXmlFname = Path.Combine(Npp.notepad.GetConfigDirectory(), "..", "nativeLang.xml");
            //if (File.Exists(nativeLangXmlFname))
            //{
            //    try
            //    {
            //        string nativeLangXml;
            //        using (var reader = new StreamReader(File.OpenRead(nativeLangXmlFname), Encoding.UTF8, true))
            //        {
            //            nativeLangXml = reader.ReadToEnd();
            //        }
            //        Match match = Regex.Match(nativeLangXml, "<Native-Langue .*? filename=\"(.*?)\\.xml\"");
            //        if (match.Success)
            //            languageName = match.Groups[1].Value.Trim().ToLower();
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"While attempting to determine native language preference from Notepad++ config XML, got an error:\r\n{ex}");
            //    }
            //}
            if (languageName == "english")
            {
                // as a fallback, try to determine the user's language by asking Windows for their current culture
                CultureInfo currentCulture = CultureInfo.CurrentCulture;
                string languageFullname = currentCulture.EnglishName;
                languageName = languageFullname.Split(' ')[0].ToLower();
                if (languageName == "Unknown")
                    languageName = currentCulture.Parent.EnglishName.Split(' ')[0].ToLower();
            }
            if (languageName == "english")
            {
                //MessageBox.Show("Not loading translations, because english is the current culture language");
                return;
            }
            string translationDir = Path.Combine(Npp.pluginDllDirectory, "translation");
            string translationFilename = Path.Combine(translationDir, languageName + ".json5");
            if (!File.Exists(translationFilename))
            {
                //MessageBox.Show($"Could not find a translation file for language {languageFirstname} in directory {translationDir}");
                return;
            }
            FileInfo translationFile = new FileInfo(translationFilename);
            try
            {
                var parser = new JsonParser(LoggerLevel.JSON5);
                string translationFileText;
                using (var fp = new StreamReader(translationFile.OpenRead(), Encoding.UTF8, true))
                {
                    translationFileText = fp.ReadToEnd();
                }
                translations = (JObject)parser.Parse(translationFileText);
                //MessageBox.Show($"Found and successfully parsed translation file at path {translationFilename}");
            }
            catch/* (exception ex)*/
            {
                //MessageBox.Show($"While attempting to parse translation file {translationFilename}, got an exception:\r\n{RemesParser.PrettifyException(ex)}");
            }
        }

        public static string GetTranslatedMenuItem(string menuItem)
        {
            if (translations is JObject jobj && jobj.children is Dictionary<string, JNode> dict
                && dict.TryGetValue("menuItems", out JNode menuItemsObjNode)
                && menuItemsObjNode is JObject menuItemsObj && menuItemsObj.children is Dictionary<string, JNode> menuItemsDict
                && menuItemsDict.TryGetValue(menuItem, out JNode menuItemNode)
                && menuItemNode.value is string s)
            {
                if (s.Length > FuncItem.MAX_FUNC_ITEM_NAME_LENGTH)
                {
                    MessageBox.Show($"The translation {JNode.StrToString(s, true)} has {s.Length} characters, " +
                        $"but it must have {FuncItem.MAX_FUNC_ITEM_NAME_LENGTH} or fewer characters.\r\n" +
                        $"Because of this, the untranslated command, {JNode.StrToString(menuItem, true)} is being used.",
                        "Too many characters in menu item translation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return menuItem;
                }
                return s;
            }
            return menuItem;
        }

        /// <summary>
        /// Used to translate the settings in <see cref="Settings"/>.<br></br>
        /// If there is no active translation file, return the propertyInfo's <see cref="DescriptionAttribute.Description"/>
        /// (which can be seen in the source code in the Description decorator of each setting).<br></br>
        /// If the propertyInfo's name is in @.settingsDescriptions of the active translation file,
        /// return @.settingsDescriptions[propertyInfo.Name] of the active translation file.
        /// </summary>
        public static string TranslateSettingsDescription(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
                return "";
            if (translations is JObject jobj && jobj.children is Dictionary<string, JNode> dict
                && dict.TryGetValue("settingsDescriptions", out JNode settingsDescNode)
                && settingsDescNode is JObject settingsDescObj
                && settingsDescObj.children.TryGetValue(propertyInfo.Name, out JNode descNode)
                && descNode.value is string s)
            {
                return s;
            }
            if (propertyInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() is DescriptionAttribute description)
                return description.Description;
            return "";
        }

        #region message box and error translation
        public static string TranslateLintMessage(bool translated, JsonLintType lintType, string englishMessage)
        {
            if (translated
                && translations is JObject jobj && jobj.children is Dictionary<string, JNode> dict
                && dict.TryGetValue("jsonLint", out JNode lintTranslations) && lintTranslations is JObject lintTransObj
                && lintTransObj.children.TryGetValue(lintType.ToString(), out JNode problemTypeTransNode)
                && problemTypeTransNode.value is string s)
                return s;
            return englishMessage;
        }

        #endregion

        #region Form translation

        /// <summary>
        /// This assumes that each character in Size 7.8 font is 7 pixels across.<br></br>
        /// In practice, this generally returns a number that is greater than the width of the text.
        /// </summary>
        private static int WidthInCharacters(int numChars, float fontSize)
        {
            return Convert.ToInt32(Math.Round(numChars * fontSize / 7.8 * 7));
        }

        /// <summary>
        /// translate a form and its controls 
        /// according to the entry corresponding to the form's name in the "forms" field of the translations object.
        /// </summary>
        public static void TranslateForm(Form form)
        {
            if (translations is JObject jobj && jobj.children is Dictionary<string, JNode> dict
                && dict.TryGetValue("forms", out JNode formsNode)
                && formsNode is JObject allFormsObj && allFormsObj.children is Dictionary<string, JNode> allFormsDict
                && allFormsDict.TryGetValue(form.Name, out JNode formNode)
                && formNode is JObject formObj && formObj.children is Dictionary<string, JNode> formDict)
            {
                if (formDict.TryGetValue("title", out JNode transTitle) && transTitle.value is string transStr)
                    form.Text = transStr;
                if (formDict.TryGetValue("controls", out JNode controlTransNode)
                    && controlTransNode is JObject controlTranslationsObj
                    && controlTranslationsObj.children is Dictionary<string, JNode> controlTranslations)
                {
                    TranslateControl(form, controlTranslations);
                }
            }
        }

        /// <summary>
        /// Translate a control according to the logic described in "/translations/english.json5" (relative to the repo root)<br></br>
        /// and recursively translate any child controls.
        /// </summary>
        /// <param name="controlTranslations">The translations for all the controls in the form</param>
        private static void TranslateControl(Control ctrl, Dictionary<string, JNode> controlTranslations)
        {
            if (!(ctrl is Form) && controlTranslations.TryGetValue(ctrl.Name, out JNode ctrlNode))
            {
                if (ctrlNode.value is string s)
                    ctrl.Text = s;
                else if (ctrlNode is JObject ctrlObj && ctrlObj.children is Dictionary<string, JNode> ctrlDict)
                {
                    if (ctrl is CheckBox checkBox
                        && ctrlDict.TryGetValue("checked", out JNode checkedNode) && checkedNode.value is string checkedVal
                        && ctrlDict.TryGetValue("unchecked", out JNode uncheckedNode) && uncheckedNode.value is string uncheckedVal)
                    {
                        checkBox.Text = checkBox.Checked ? checkedVal : uncheckedVal;
                        checkBox.CheckedChanged += (object sender, EventArgs e) =>
                        {
                            checkBox.Text = checkBox.Checked ? checkedVal : uncheckedVal;
                        };
                    }
                    else if (ctrl is LinkLabel llbl
                        && ctrlDict.TryGetValue("text", out JNode textNode) && textNode.value is string textVal
                        && ctrlDict.TryGetValue("linkStart", out JNode linkStartNode) && linkStartNode.value is long linkStartVal
                        && ctrlDict.TryGetValue("linkLength", out JNode linkLengthNode) && linkLengthNode.value is long linkLengthVal)
                    {
                        llbl.Text = textVal;
                        llbl.LinkArea = new LinkArea(Convert.ToInt32(linkStartVal), Convert.ToInt32(linkLengthVal));
                    }
                }
                else if (ctrlNode is JArray ctrlArr && ctrlArr.children is List<JNode> ctrlList && ctrl is ComboBox comboBox)
                {
                    int maxTranslationLen = 0;
                    for (int ii = 0; ii < comboBox.Items.Count && ii < ctrlList.Count; ii++)
                    {
                        JNode translationII = ctrlList[ii];
                        if (translationII.value is string translationVal)
                        {
                            comboBox.Items[ii] = translationVal;
                            if (translationVal.Length > maxTranslationLen)
                                maxTranslationLen = translationVal.Length;
                        }
                    }
                    int newDropDownWidth = WidthInCharacters(maxTranslationLen, ctrl.Font.Size);
                    if (newDropDownWidth > comboBox.DropDownWidth)
                        comboBox.DropDownWidth = newDropDownWidth;
                }
            }
            int ctrlCount = ctrl.Controls.Count;
            if (ctrlCount > 0)
            {
                // translate each child of this control
                var childrenByLeft = new List<(Control ctrl, bool textWasMultiline, bool isAnchoredRight)>(ctrlCount);
                foreach (Control child in ctrl.Controls)
                {
                    int childWidthInChars = WidthInCharacters(child.Text.Length, child.Font.Size);
                    bool textWasMultiline = !child.AutoSize && ((child is Label || child is LinkLabel) && childWidthInChars > child.Width);
                    TranslateControl(child, controlTranslations);
                    childrenByLeft.Add((child, textWasMultiline, (child.Anchor & AnchorStyles.Right) == AnchorStyles.Right));
                }
                // the child controls may have more text now,
                //    so we need to resolve any collisions that may have resulted.
                childrenByLeft.Sort((x, y) => x.ctrl.Left.CompareTo(y.ctrl.Left));
                for (int ii = 0; ii < ctrlCount; ii++)
                {
                    (Control child, bool textWasMultiline, bool isAnchoredRight) = childrenByLeft[ii];
                    int textLen = child.Text.Length;
                    int widthIfButtonOrLabel = WidthInCharacters(textLen, child.Font.Size + 1);
                    if (child is Button && textLen > 2 && textLen < 14)
                    {
                        int increase = textLen <= 5 ? 25 : (15 - textLen) * 2 + 7;
                        widthIfButtonOrLabel += increase;
                    }
                    if (!textWasMultiline && (child is Button || child is Label || child is CheckBox) &&
                        !child.Text.Contains('\n') && widthIfButtonOrLabel > child.Width)
                    {
                        child.Width = widthIfButtonOrLabel;
                        if (isAnchoredRight)
                            child.Left -= 5; // to account for weirdness when you resize a right-anchored thing
                        // if there are overlapping controls to the right, move those far enough right that they don't overlap.
                        for (int jj = ii + 1; jj < ctrlCount; jj++)
                        {
                            Control otherChild = childrenByLeft[jj].ctrl;
                            if (otherChild.Left > child.Left && Overlap(child, otherChild))
                            {
                                if (otherChild.Left > child.Left)
                                    otherChild.Left = child.Right + 2;
                            }
                        }
                    }
                    child.Refresh();
                }
                int maxRight = 0;
                foreach (Control child in ctrl.Controls)
                    maxRight = child.Right > maxRight ? child.Right : maxRight;
                // Temporarily turn off the normal layout logic,
                //     because this would cause controls to move right when the form is resized
                //     (yes, even if they're not anchored right. No, that doesn't make sense)
                ctrl.SuspendLayout();
                if (maxRight > ctrl.Width)
                {
                    int padding = ctrl is Form ? 25 : 5;
                    ctrl.Width = maxRight + padding;
                }
                // Resume layout logic, ignoring the pending requests to move controls right due to resizing of form
                ctrl.ResumeLayout(false);
            }
        }

        private static bool VerticalOverlap(Control ctrl1, Control ctrl2)
        {
            return !(ctrl1.Bottom < ctrl2.Top || ctrl2.Bottom < ctrl1.Top);
        }

        private static bool HorizontalOverlap(Control ctrl1, Control ctrl2)
        {
            return !(ctrl1.Right < ctrl2.Left || ctrl2.Right < ctrl1.Left);
        }

        private static bool Overlap(Control ctrl1, Control ctrl2)
        {
            return HorizontalOverlap(ctrl1, ctrl2) && VerticalOverlap(ctrl1, ctrl2);
        }
        #endregion
    }
}
