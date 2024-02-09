using System.Windows.Forms;
using JSON_Tools.Utils;

namespace JSON_Tools.Forms
{
    /// <summary>
    /// various methods that every new form in this app should call.<br></br>
    /// You may ask, "Wait, why do we have a static class with a bunch of methods everyone needs to call?"<br></br>
    ///     "Shouldn't this be a subclass of Form, and have all the forms just subclass this?"<br></br>
    /// To which I answer, NO!<br></br>
    /// *I TRIED* having a superclass that all my forms inherited from, and the result was a total mess.
    /// </summary>
    public static class NppFormHelper
    {
        /// <summary>
        /// CALL THIS IN YOUR Dispose(bool disposing) METHOD, INSIDE OF THE ".Designer.cs" FILE<br></br>
        /// When this form is initialized, *if it is a modeless dialog* (i.e., !isModal; the form does not block the parent application until closed)<br></br>
        /// this will call Notepad++ with the NPPM_MODELESSDIALOG message to register the form.
        /// <strong>VERY IMPORTANT: in your Designer.cs files, in the part where it says this.Controls.Add(nameOfControl),
        /// you need to make sure the controls are added in tabstop order.</strong><br></br>
        /// This is because the order in which the controls are added controls tab order.<br></br>
        /// For example, if you want to go through your controls in the order<br></br>
        /// 1. FooButton<br></br>
        /// 2. BarTextBox<br></br>
        /// 3. BazCheckBox<br></br>
        /// You must go to your Designer.cs file and make sure that the Form adds the controls in this order:<br></br>
        /// <code>
        /// this.Controls.Add(this.FooButton);
        /// this.Controls.Add(this.BarTextBox);
        /// this.Controls.Add(this.BazCheckBox);
        /// </code>
        /// </summary>
        /// <param name="isModal">if true, this blocks the parent application until closed. THIS IS ONLY TRUE OF POP-UP DIALOGS</param>
        public static void RegisterFormIfModeless(Form form, bool isModal)
        {
            if (!isModal)
                Npp.notepad.AddModelessDialog(form.Handle);
        }

        /// <summary>
        /// CALL THIS METHOD IN A KeyUp HANDLER, *UNLESS USING GenericKeyUpHandler BELOW*<br></br>
        /// Tab -> go through controls, Shift+Tab -> go through controls backward.<br></br>
        /// Ignores invisible or disabled controls.
        /// </summary>
        /// <param name="form">the parent form</param>
        /// <param name="sender">probably a control with a tabstop</param>
        /// <param name="e">the key event that triggered this</param>
        public static void GenericTabNavigationHandler(Form form, object sender, KeyEventArgs e)
        {
            if (sender is TextBox tb && tb.Parent is ListBox)
                return; // ComboBoxes are secretly two controls in one (see https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox?view=windowsdesktop-8.0)
                        // this event fires twice for a CombobBox because of this, so we need to suppress the extra one this way
            Control next = form.GetNextControl((Control)sender, !e.Shift);
            while (next == null || !next.TabStop || !next.Visible || !next.Enabled)
                next = form.GetNextControl(next, !e.Shift);
            next.Focus();
            e.Handled = true;
        }

        /// <summary>
        /// CALL THIS IN YOUR KeyUp HANDLER<br></br>
        /// Enter presses button,<br></br>
        /// escape focuses editor (or closes if isModal),<br></br>
        /// Ctrl+V pastes text into text boxes and combo boxes<br></br>
        /// if isModal:<br></br>
        /// - tab goes through controls,<br></br>
        /// - shift-tab -> go through controls backward<br></br>
        /// </summary>
        /// <param name="form"></param>
        /// <param name="isModal">if true, this blocks the parent application until closed. THIS IS ONLY TRUE OF POP-UP DIALOGS</param>
        public static void GenericKeyUpHandler(Form form, object sender, KeyEventArgs e, bool isModal)
        {
            // enter presses button
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                if (sender is Button btn)
                {
                    // Enter has the same effect as clicking a selected button
                    btn.PerformClick();
                }
                else
                    PressEnterInTextBoxHandler(sender, isModal);
            }
            // Escape ->
            //     * if this.IsModal (meaning this is a pop-up dialog), close this.
            //     * otherwise, focus the editor component.
            else if (e.KeyData == Keys.Escape)
            {
                if (isModal)
                    form.Close();
                else
                    Npp.editor.GrabFocus();
            }
            // Tab -> go through controls, Shift+Tab -> go through controls backward
            else if (e.KeyCode == Keys.Tab && isModal)
            {
                GenericTabNavigationHandler(form, sender, e);
            }
        }

        /// <summary>
        /// NPPM_MODELESSDIALOG consumes the KeyDown and KeyPress events for the Enter key,<br></br>
        /// so our KeyUp handler needs to simulate pressing enter to add a new line in a multiline text box.<br></br>
        /// Note that this does not fully repair the functionality of the Enter key in a multiline text box,
        /// because only one newline can be created for a single keypress of Enter, no matter how long the key is held down.
        /// </summary>
        /// <param name="sender">the text box that sent the message</param>
        /// <param name="isModal">if true, this blocks the parent application until closed. THIS IS ONLY TRUE OF POP-UP DIALOGS</param>
        public static void PressEnterInTextBoxHandler(object sender, bool isModal)
        {

            if (!isModal && sender is TextBox tb && tb.Multiline)
            {
                int selstart = tb.SelectionStart;
                tb.SelectedText = "";
                string text = tb.Text;
                tb.Text = text.Substring(0, selstart) + "\r\n" + text.Substring(selstart);
                tb.SelectionStart = selstart + 2; // after the inserted newline
                tb.SelectionLength = 0;
                tb.ScrollToCaret();
            }
        }

        /// <summary>
        /// CALL THIS IN YOUR Dispose(bool disposing) METHOD, INSIDE OF THE ".Designer.cs" FILE<br></br>
        /// If this was a modeless dialog (i.e., !isModal; a dialog that does not block Notepad++ while open),<br></br>
        /// call Notepad++ with the NPPM_MODELESSDIALOG message to unregister the form.
        /// </summary>
        /// <param name="isModal">if true, this blocks the parent application until closed. THIS IS ONLY TRUE OF POP-UP DIALOGS</param>
        public static void UnregisterFormIfModeless(Form form, bool isModal)
        {
            if (!form.IsDisposed && !isModal)
                Npp.notepad.RemoveModelessDialog(form.Handle);
        }
    }
}
