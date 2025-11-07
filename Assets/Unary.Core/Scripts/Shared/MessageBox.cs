using System;
using System.Runtime.InteropServices;

namespace Unary.Core
{
    public class MessageBox
    {
        [DllImport("User32.dll", EntryPoint = "MessageBox", CharSet = CharSet.Auto)]
        private static extern int MsgBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        private static readonly IntPtr NullPtr = new(0);

        public static void Show(string title, string text)
        {
            MsgBox(NullPtr, text, title, 0);
        }
    }
}
