﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Windows.Forms;//for Keys enum
using System.Runtime.InteropServices;

using System.Reflection;
using System.IO;
using System.Drawing;

namespace altf7
{
    class Program
    {
        #region dllimport + const + hook

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
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
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        private static Int32 showWindow = 0; //0 - SW_HIDE - Hides the window and activates another window.

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        private static IntPtr ThisConsole = GetConsoleWindow();

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

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        #endregion dllimport + const + hook

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

        static bool lastWasAlt = false;
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN) || (wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                //Console.WriteLine((Keys)vkCode + "\t" + vkCode.ToString());
                if (vkCode == 164) { lastWasAlt = true; Console.WriteLine((Keys)vkCode + "\t" + vkCode.ToString()); }
                else if (vkCode == 118 && lastWasAlt) { moveForegroundWindowUntilClick(); Console.WriteLine((Keys)vkCode + "\t" + vkCode.ToString()); }
                else lastWasAlt = false;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
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

        #region tray
        private static NotifyIcon TrayIcon;
        private static void TrayIcon_Click(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {//reserve right click for context menu
                showWindow = ++showWindow % 2;
                ShowWindow(ThisConsole, showWindow);
            }
        }
        private static void smoothExit(object sender, EventArgs e)
        {
            TrayIcon.Visible = false;
            Application.Exit();
            Environment.Exit(1);
        }
        private static void initTray()
        {
            Console.Title = "Alt+F7 daemon - to hide/restore click icon on tray. To close - [X] or right click in tray.";

            TrayIcon = new NotifyIcon();
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Stream IconResourceStream = currentAssembly.GetManifestResourceStream("altf7.icon.ico");
            TrayIcon.Icon = new Icon(IconResourceStream);
            TrayIcon.Visible = true;

            ShowWindow(ThisConsole, showWindow);
            TrayIcon.MouseClick += new MouseEventHandler(TrayIcon_Click);

            TrayIcon.ContextMenuStrip = new ContextMenuStrip();
            TrayIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[] { new ToolStripMenuItem() });
            TrayIcon.ContextMenuStrip.Items[0].Text = "Exit";
            TrayIcon.ContextMenuStrip.Items[0].Click += new EventHandler(smoothExit);
        }
        

        #endregion tray

        static void moveForegroundWindowUntilClick()
        {
            while (true)
            {
                string title = getForegroundWindowTitle();
                point p = getMousePos();
                setForegroundWindowPos(p.x, p.y);
                Console.WriteLine(p.x + "\t" + p.y + "\t" + title);
                if (mouseClicked()) break;
            }
        }

        static void Main(string[] args)
        {
            initTray();
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

    }
}
