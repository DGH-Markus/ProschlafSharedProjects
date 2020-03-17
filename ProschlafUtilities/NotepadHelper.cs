using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ProschlafUtils
{
    public static class NotepadHelper
    {
        #region Imports
        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        private static extern int SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);
        #endregion

        /// <summary>
        /// Displays the specified text in a new notepad process window (without saving the text to the disk first).
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        public static Exception ShowMessage(string text, string title = null)
        {
            try
            {
                Process notepad = Process.Start(new ProcessStartInfo("notepad.exe"));
                if (notepad != null)
                {
                    notepad.WaitForInputIdle();

                    if (!string.IsNullOrEmpty(title))
                        SetWindowText(notepad.MainWindowHandle, title);

                    if (!string.IsNullOrEmpty(text))
                    {
                        IntPtr child = FindWindowEx(notepad.MainWindowHandle, new IntPtr(0), "Edit", null);
                        SendMessage(child, 0x000C, 0, text);
                    }
                }

                return null;
            }
            catch(Exception ex)
            {
                return ex;
            }
        }
    }
}
