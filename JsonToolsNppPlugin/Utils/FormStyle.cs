using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace JSON_Tools.Utils
{
    public static class FormStyle
    {
        public static Color SlightlyDarkControl = Color.FromArgb(
            3 * SystemColors.Control.R / 4 + SystemColors.ControlDark.R / 4,
            3 * SystemColors.Control.G / 4 + SystemColors.ControlDark.G / 4,
            3 * SystemColors.Control.B / 4 + SystemColors.ControlDark.B / 4
        );

        
        /// <summary>
        /// Changes the background and text color of the form
        /// and any child forms to match the editor window.<br></br>
        /// Fires when the form is first opened
        /// and also whenever the style is changed.<br></br>
        /// Heavily based on CsvQuery (https://github.com/jokedst/CsvQuery)
        /// </summary>
        public static void ApplyStyle(Control ctrl, bool use_npp_style, bool darkMode = false)
        {
            if (ctrl == null || ctrl.IsDisposed) return;
            int[] version = Npp.notepad.GetNppVersion();
            if (version[0] < 8)
                use_npp_style = false; // trying to follow editor style looks weird for Notepad++ 7.3.3
            if (ctrl is Form form)
            {
                foreach (Form childForm in form.OwnedForms)
                {
                    ApplyStyle(childForm, use_npp_style, darkMode);
                }
            }
            Color back_color = Npp.notepad.GetDefaultBackgroundColor();
            if (!use_npp_style || (
                back_color.R > 240 &&
                back_color.G > 240 &&
                back_color.B > 240))
            {
                // if the background is basically white,
                // use the system defaults because they
                // look best on a white or nearly white background
                ctrl.BackColor = SystemColors.Control;
                ctrl.ForeColor = SystemColors.ControlText;
                foreach (Control child in ctrl.Controls)
                {
                    if (child is GroupBox)
                        ApplyStyle(child, use_npp_style, darkMode);
                    // controls containing text
                    if (child is TextBox || child is ListBox || child is ComboBox || child is TreeView)
                    {
                        child.BackColor = SystemColors.Window; // white background
                        child.ForeColor = SystemColors.WindowText;
                    }
                    else if (child is LinkLabel llbl)
                    {
                        llbl.LinkColor = Color.Blue;
                        llbl.ActiveLinkColor = Color.Red;
                        llbl.VisitedLinkColor = Color.Purple;
                    }
                    else
                    {
                        // buttons should be a bit darker but everything else is the same color as the background
                        child.BackColor = (child is Button) ? SlightlyDarkControl : SystemColors.Control;
                        child.ForeColor = SystemColors.ControlText;
                    }
                }
                return;
            }
            Color fore_color = Npp.notepad.GetDefaultForegroundColor();
            ctrl.BackColor = back_color;
            foreach (Control child in ctrl.Controls)
            {
                child.BackColor = back_color;
                child.ForeColor = fore_color;
                if (child is LinkLabel llbl)
                {
                    llbl.LinkColor = fore_color;
                    llbl.ActiveLinkColor = fore_color;
                    llbl.VisitedLinkColor = fore_color;
                }
            }
        }
    }
}
