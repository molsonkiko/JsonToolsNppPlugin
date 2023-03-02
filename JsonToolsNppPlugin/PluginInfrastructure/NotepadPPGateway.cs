// NPP plugin platform for .Net v0.94.00 by Kasper B. Graversen etc.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using JSON_Tools.PluginInfrastructure;
using JSON_Tools.Utils;

namespace Kbg.NppPluginNET.PluginInfrastructure
{
	public interface INotepadPPGateway
	{
		void FileNew();

		void AddToolbarIcon(int funcItemsIndex, toolbarIcons icon);
		void AddToolbarIcon(int funcItemsIndex, Bitmap icon);
		string GetNppPath();
		string GetPluginConfigPath();
		string GetCurrentFilePath();
		unsafe string GetFilePath(IntPtr bufferId);
		void SetCurrentLanguage(LangType language);
		bool OpenFile(string path);
		bool SaveCurrentFile();
		void ShowDockingForm(System.Windows.Forms.Form form);
		void HideDockingForm(System.Windows.Forms.Form form);
		Color GetDefaultForegroundColor();
		Color GetDefaultBackgroundColor();
		string GetConfigDirectory();
		int[] GetNppVersion();
		void SetCurrentBufferInternalName(string newName);
	}

	/// <summary>
	/// This class holds helpers for sending messages defined in the Msgs_h.cs file. It is at the moment
	/// incomplete. Please help fill in the blanks.
	/// </summary>
	public class NotepadPPGateway : INotepadPPGateway
	{
		private const int Unused = 0;

		public void FileNew()
		{
			Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_MENUCOMMAND, Unused, NppMenuCmd.IDM_FILE_NEW);
		}

		public void AddToolbarIcon(int funcItemsIndex, toolbarIcons icon)
		{
			IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(icon));
			try {
				Marshal.StructureToPtr(icon, pTbIcons, false);
				_ = Win32.SendMessage(
					PluginBase.nppData._nppHandle,
					(uint) NppMsg.NPPM_ADDTOOLBARICON,
					PluginBase._funcItems.Items[funcItemsIndex]._cmdID,
					pTbIcons);
			} finally {
				Marshal.FreeHGlobal(pTbIcons);
			}
		}

		public void AddToolbarIcon(int funcItemsIndex, Bitmap icon)
		{
			var tbi = new toolbarIcons();
			tbi.hToolbarBmp = icon.GetHbitmap();
			AddToolbarIcon(funcItemsIndex, tbi);
		}

		/// <summary>
		/// Gets the path of the current document.
		/// </summary>
		public string GetCurrentFilePath()
		{
			var path = new StringBuilder(2000);
			Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);
			return path.ToString();
		}

		/// <summary>
		/// This method incapsulates a common pattern in the Notepad++ API: when
		/// you need to retrieve a string, you can first query the buffer size.
		/// This method queries the necessary buffer size, allocates the temporary
		/// memory, then returns the string retrieved through that buffer.
		/// </summary>
		/// <param name="message">Message ID of the data string to query.</param>
		/// <returns>String returned by Notepad++.</returns>
		public string GetString(NppMsg message)
		{
			int len = Win32.SendMessage(
					PluginBase.nppData._nppHandle,
					(uint) message, Unused, Unused).ToInt32()
				+ 1;
			var res = new StringBuilder(len);
			_ = Win32.SendMessage(
				PluginBase.nppData._nppHandle, (uint) message, len, res);
			return res.ToString();
		}

		/// <returns>The path to the Notepad++ executable.</returns>
		public string GetNppPath()
			=> GetString(NppMsg.NPPM_GETNPPDIRECTORY);

		/// <returns>The path to the Config folder for plugins.</returns>
		public string GetPluginConfigPath()
			=> GetString(NppMsg.NPPM_GETPLUGINSCONFIGDIR);

		/// <summary>
		/// Open a file for editing in Notepad++, pretty much like using the app's
		/// File - Open menu.
		/// </summary>
		/// <param name="path">The path to the file to open.</param>
		/// <returns>True on success.</returns>
		public bool OpenFile(string path)
			=> Win32.SendMessage(
				PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_DOOPEN, Unused, path).ToInt32()
			!= 0;

		/// <summary>
		/// Gets the path of the current document.
		/// </summary>
		public unsafe string GetFilePath(IntPtr bufferId)
		{
			var path = new StringBuilder(2000);
			Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_GETFULLPATHFROMBUFFERID, bufferId, path);
			return path.ToString();
		}

		public void SetCurrentLanguage(LangType language)
		{
			Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_SETCURRENTLANGTYPE, Unused, (int) language);
		}

		/// <summary>
		/// open a standard save file dialog to save the current file<br></br>
		/// Returns true if the file was saved
		/// </summary>
		public bool SaveCurrentFile()
		{
			IntPtr result = Win32.SendMessage(PluginBase.nppData._nppHandle,
					(uint)(NppMsg.NPPM_SAVECURRENTFILEAS),
					0, 0);
			return result.ToInt32() == 1;
		}

		public void HideDockingForm(System.Windows.Forms.Form form)
		{
			Win32.SendMessage(PluginBase.nppData._nppHandle,
					(uint)(NppMsg.NPPM_DMMHIDE),
					0, form.Handle);
		}

		public void ShowDockingForm(System.Windows.Forms.Form form)
		{
			Win32.SendMessage(PluginBase.nppData._nppHandle,
					(uint)(NppMsg.NPPM_DMMSHOW),
					0, form.Handle);
		}

		public Color GetDefaultForegroundColor()
		{
			var rawColor = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETEDITORDEFAULTFOREGROUNDCOLOR, 0, 0);
			return Color.FromArgb(rawColor & 0xff, (rawColor >> 8) & 0xff, (rawColor >> 16) & 0xff);
		}

		public Color GetDefaultBackgroundColor()
		{
			var rawColor = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETEDITORDEFAULTBACKGROUNDCOLOR, 0, 0);
			return Color.FromArgb(rawColor & 0xff, (rawColor >> 8) & 0xff, (rawColor >> 16) & 0xff);
		}

		/// <summary>
		/// Figure out default N++ config file path<br></br>
		/// Path is usually -> .\Users\<username>\AppData\Roaming\Notepad++\plugins\config\
		/// </summary>
		public string GetConfigDirectory()
        {
			var sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
			Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
			return sbIniFilePath.ToString();
		}

		/// <summary>
		/// 2-int array. First entry: major version. Second entry: minor version
		/// </summary>
		/// <returns></returns>
		public int[] GetNppVersion()
		{
			// the low word (i.e., version & 0xffff
			int version = Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETNPPVERSION, 0, 0).ToInt32();
			int minor = version & 0xffff;
			int major = version >> 16;
			return new int[] { major, minor };
        }

		/// <summary>
		/// Changes the apparent name of the current buffer. Does not work on files that have already been saved to disk.<br></br>
		/// </summary>
		/// <param name="newName"></param>
		public void SetCurrentBufferInternalName(string newName)
		{
            long bufferId = Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0).ToInt64();
            // change the current file extension to ".dson" (this command only works for unsaved files, but of course we just made an unsaved file)
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_INTERNAL_SETFILENAME, (IntPtr)bufferId, newName);
        }
	}

	/// <summary>
	/// This class holds helpers for sending messages defined in the Resource_h.cs file. It is at the moment
	/// incomplete. Please help fill in the blanks.
	/// </summary>
	class NppResource
	{
		private const int Unused = 0;

		public void ClearIndicator()
		{
			Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) Resource.NPPM_INTERNAL_CLEARINDICATOR, Unused, Unused);
		}
	}
}
