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
        public static void ApplyStyle(Form form, bool use_npp_style, bool darkMode=false)
        {
            if (form == null || form.IsDisposed) return;
            int[] version = Npp.notepad.GetNppVersion();
            if (version[0] < 8)
                use_npp_style = false; // trying to follow editor style looks weird for Notepad++ 7.3.3
            foreach (Form childForm in form.OwnedForms)
            {
                ApplyStyle(childForm, use_npp_style, darkMode);
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
                form.BackColor = SystemColors.Control;
                form.ForeColor = SystemColors.ControlText;
                foreach (Control ctrl in form.Controls)
                {
                    // controls containing text
                    if (ctrl is TextBox || ctrl is ListBox || ctrl is ComboBox || ctrl is TreeView)
                    {
                        ctrl.BackColor = SystemColors.Window; // white background
                        ctrl.ForeColor = SystemColors.WindowText;
                    }
                    else if (ctrl is LinkLabel llbl)
                    {
                        llbl.LinkColor = Color.Blue;
                        llbl.ActiveLinkColor = Color.Red;
                        llbl.VisitedLinkColor = Color.Purple;
                    }
                    else
                    {
                        // buttons should be a bit darker but everything else is the same color as the background
                        ctrl.BackColor = (ctrl is Button) ? SlightlyDarkControl : SystemColors.Control;
                        ctrl.ForeColor = SystemColors.ControlText;
                    }
                }
                return;
            }
            Color fore_color = Npp.notepad.GetDefaultForegroundColor();
            Color in_between = Color.FromArgb(
                (fore_color.R + back_color.R * 3) / 4,
                (fore_color.G + back_color.G * 3) / 4,
                (fore_color.B + back_color.B * 3) / 4
            );
            form.BackColor = back_color;
            foreach (Control ctrl in form.Controls)
            {
                if (ctrl is Label || ctrl is GroupBox || ctrl is CheckBox || ctrl is LinkLabel)
                    // these look better when then have the same background as the parent form
                    ctrl.BackColor = back_color;
                else ctrl.BackColor = in_between;
                ctrl.ForeColor = fore_color;
                if (ctrl is LinkLabel llbl)
                {
                    llbl.LinkColor = fore_color;
                    llbl.ActiveLinkColor = fore_color;
                    llbl.VisitedLinkColor = fore_color;
                }
            }
        }
    }
}
