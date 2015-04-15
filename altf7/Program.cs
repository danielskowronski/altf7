using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Windows.Forms;//for Keys enum
using System.Runtime.InteropServices;

namespace altf7
{
    class Program
    {
        #region dllimport

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("kernel32.dll")]
        static extern uint GetLastError();
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out point pt);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(UInt16 virtualKeyCode);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion dllimport

        #region operating functions
        static void setForegroundWindowPos(int x, int y)
        {
            IntPtr handle = GetForegroundWindow();
            SetWindowPos(handle, -2, x, y, 0, 0, 1);
        }
        static string getForegroundWindowTitle()
        {
            int chars = 256;
            StringBuilder buff = new StringBuilder(chars);
            IntPtr handle = GetForegroundWindow();
            if (GetWindowText(handle, buff, chars) > 0)
            {
                return buff.ToString();
            }
            else
            {
                return "NULL";
            }
        }
        public struct point { public int x; public int y; }
        static point getMousePos()
        {
            point p;
            bool b = GetCursorPos(out p);
            return p;
        }
        static bool mouseClicked()
        {
            return GetAsyncKeyState(1) != 0 || GetAsyncKeyState(2) != 0;
        }
        #endregion operating functions

        #region manual testing functions
        static void setPosByKeyboard()
        {
            while (true)
            {
                string title = getForegroundWindowTitle(); int x, y;
                Console.WriteLine(title);
                Console.Write("X? "); x = int.Parse(Console.ReadLine());
                Console.Write("Y? "); y = int.Parse(Console.ReadLine());
                setForegroundWindowPos(x, y);
            }
        }
        static void getMousePosLoop()
        {
            while (true)
            {
                point p = getMousePos();

                Console.WriteLine(p.x + "\t" + p.y + "\t"+(mouseClicked() ? "clicked" : "not"));

                System.Threading.Thread.Sleep(100);
            }
        }
        #endregion manual testing functions


        #region kbd
        // http://blogs.msdn.com/b/toub/archive/2006/05/03/589423.aspx
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Console.WriteLine((Keys)vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        #endregion kbd
        static void doMagic()
        {
            while (true)
            {
                string title = getForegroundWindowTitle();
                point p = getMousePos();
                setForegroundWindowPos(p.x, p.y);
                Console.WriteLine(p.x + "\t" + p.y + "\t" + title);
            }
        }

        static void Main(string[] args)
        {
            //doMagic();
            //getMousePosLoop();
            
            _hookID = SetHook(_proc);
            getMousePosLoop();
            UnhookWindowsHookEx(_hookID);
        }
    }
}
