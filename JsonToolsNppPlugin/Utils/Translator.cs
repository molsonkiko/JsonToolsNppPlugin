using JSON_Tools.JSON_Tools;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Kbg.NppPluginNET.PluginInfrastructure;

// TODO:
// 1. Implement translation of settings descriptions.
//     This is surprisingly complicated, due to the PropertyGrid class not allowing public read/write access to the descriptive text for a property.
// 2. Resolve collisions between controls due to the text length changing.
//     This will require some kind of simple collision-detection algorithm in the TranslateControl method.

namespace JSON_Tools.Utils
{
    public static class Translator
    {
        private static JObject translations = null;
        public static bool HasTranslations => !(translations is null); 

        public static void LoadTranslations()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            string languageFullname = currentCulture.EnglishName;
            string languageFirstname = languageFullname.Split(' ')[0].ToLower();
            if (languageFirstname == "Unknown")
                languageFirstname = currentCulture.Parent.EnglishName.Split(' ')[0].ToLower();
            if (languageFirstname == "english")
            {
                //MessageBox.Show("Not loading translations, because english is the current culture language");
                return;
            }
            string translationDir = Path.Combine(Npp.pluginDllDirectory, "translation");
            string translationFilename = Path.Combine(translationDir, languageFirstname + ".json5");
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
                    int newDropDownWidth = maxTranslationLen * 7;
                    if (newDropDownWidth > comboBox.DropDownWidth)
                        comboBox.DropDownWidth = newDropDownWidth;
                }
            }
            foreach (Control child in ctrl.Controls)
                TranslateControl(child, controlTranslations);
        }
    }
}
