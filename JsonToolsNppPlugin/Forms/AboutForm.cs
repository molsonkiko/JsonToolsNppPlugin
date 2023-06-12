using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Forms
{
    public partial class AboutForm : Form
    {
        private static int[] nppVersion = Npp.notepad.GetNppVersion();

        public AboutForm()
        {
            InitializeComponent();
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
            ThanksWowLinkLabel.LinkColor = ThanksWowLinkLabel.ForeColor; // hidden!
            Title.Text = Title.Text.Replace("X.Y.Z.A", Npp.AssemblyVersionString());
            string nppVersionString = string.Join(".", nppVersion.Select((x) => x.ToString()));
            DebugInfoLabel.Text = DebugInfoLabel.Text.Replace("X.Y.Z", nppVersionString);
        }

        /// <summary>
        /// open GitHub repo with the web browser
        /// </summary>
        private void GitHubLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string help_url = "https://github.com/molsonkiko/JsonToolsNppPlugin";
            try
            {
                var ps = new ProcessStartInfo(help_url)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(),
                    "Could not open documentation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// sneakily conceal the DSON easter egg where only the most curious users will find it
        /// </summary>
        private void ThanksWowLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Dogeify();
        }

        /// <summary>
        /// Escape key exits the form.<br></br>
        /// Also, an alternate way to access the DSON emitter:<br></br>
        /// hit the W key (for "wow") or the S key (for "such") while the About form is open
        /// </summary>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.W || keyData == Keys.S)
            {
                Dogeify();
                this.Close();
                return true;
            }
            if (ModifierKeys == Keys.None && keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        /// <summary>
        /// Open a new DSON document containing the dogeified version of the current JSON.<br></br>
        /// Also copies the DSON UDL from the plugins directory into the UDL folder in AppData,
        /// so the user can start seeing DSON styling as soon as they restart.
        /// </summary>
        static void Dogeify()
        {
            (bool fatal, JNode json) = Main.TryParseJson();
            if (fatal || json == null) return;
            if (nppVersion[0] >= 8)
            {
                // add the UDL file to the userDefinedLangs folder so that it can colorize the new file
                DirectoryInfo userDefinedLangPath = new DirectoryInfo(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Notepad++",
                    "userDefineLangs"));
                if (userDefinedLangPath.Exists)
                {
                    FileInfo dsonUDLPath = new FileInfo(Path.Combine(
                        Npp.notepad.GetNppPath(),
                        "plugins",
                        "JsonTools",
                        "DSON UDL.xml"
                    ));
                    string targetPath = Path.Combine(userDefinedLangPath.FullName, "dson.xml");
                    if (dsonUDLPath.Exists && !File.Exists(targetPath))
                    {
                        dsonUDLPath.CopyTo(targetPath);
                    }
                }
            }
            try
            {
                string dson = Dson.Dump(json);
                Npp.notepad.FileNew();
                Npp.editor.SetText(dson);
                Npp.RemoveTrailingSOH();
                Npp.editor.AppendText(2, "\r\n");
                Npp.RemoveTrailingSOH();
                string newName = Npp.notepad.GetCurrentFilePath() + ".dson";
                Npp.notepad.SetCurrentBufferInternalName(newName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not convert JSON to DSON. Got exception:\r\n{RemesParser.PrettifyException(ex)}",
                    "such error very sad",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
