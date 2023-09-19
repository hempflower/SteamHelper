using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.Win32;

namespace SteamHelper
{
    class Utils
    {

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetFocus(IntPtr hWnd);
        public delegate bool EnumDelegate(IntPtr hWnd, int lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "GetWindowText", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);
        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        public static string FindSteamExePath()
        {
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Valve\\Steam");

            var installPathValue = registryKey.GetValue("InstallPath");
            if(installPathValue == null)
            {
                return "";
            }

            var installPath = installPathValue.ToString();
            var executablePath = Path.Combine(installPath, "Steam.exe");

            // Check file exist

            if(!File.Exists(executablePath))
            {
                return "";
            }

            return executablePath;
        }

        public static void KillSteamClient()
        {
            RunCommand("taskkill", "/F /IM Steam.exe");
            RunCommand("taskkill", "/F /IM steamwebhelper.exe");
            RunCommand("taskkill", "/F /IM steamerrorreporter.exe");
            RunCommand("taskkill", "/F /IM steamservice.exe");
            RunCommand("taskkill", "/F /IM GameOverlayUI.exe");

            Thread.Sleep(1000);
        }

        public static void RunCommand(string fileName,string args)
        {
            Process cmdProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            cmdProcess.Start();
        }

        public static void LaunchSteam(string args)
        {
            var steamPath = FindSteamExePath();
            Process steamProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = steamPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };

            steamProcess.Start();
        }


        private static void AttachedThreadInputAction(Action action)
        {
            var foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            var appThread = GetCurrentThreadId();
            bool threadsAttached = false;
            try
            {
                threadsAttached =
                    foreThread == appThread ||
                    AttachThreadInput(foreThread, appThread, true);
                if (threadsAttached) action();
                else throw new ThreadStateException("AttachThreadInput failed.");
            }
            finally
            {
                if (threadsAttached)
                    AttachThreadInput(foreThread, appThread, false);
            }
        }

        public static void ForceWindowToForeground(IntPtr hwnd)
        {
            const int SW_SHOW = 5;
            AttachedThreadInputAction(
                () =>
                {
                    BringWindowToTop(hwnd);
                    ShowWindow(hwnd, SW_SHOW);
                });
        }

        public static IntPtr TryGetSteamLoginWindow()
        {
            IntPtr steamLoginWindow = IntPtr.Zero;
            EnumDelegate filter = delegate (IntPtr hWnd, int lParam)
            {
                StringBuilder strbTitle = new StringBuilder(255);
                int nLength = GetWindowText(hWnd, strbTitle, strbTitle.Capacity + 1);
                string title = strbTitle.ToString();

                StringBuilder strbClassname = new StringBuilder(256);
                int nRet = GetClassName(hWnd, strbClassname, strbClassname.Capacity);
                string classname = strbClassname.ToString();

                if (IsWindowVisible(hWnd) &&
                    !string.IsNullOrEmpty(title) &&
                    !string.IsNullOrEmpty(classname) &&
                    classname.Equals("SDL_app") &&
                    title.Contains("Steam") &&
                    title.Length > 5)
                {
                    steamLoginWindow = hWnd;
                }
                return true;
            };
            EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero);
            return steamLoginWindow;
        }

        public static IntPtr TryGetSteamMainWindow()
        {
            IntPtr steamMainWindow = IntPtr.Zero;
            EnumDelegate filter = delegate (IntPtr hWnd, int lParam)
            {
                StringBuilder strbTitle = new StringBuilder(255);
                int nLength = GetWindowText(hWnd, strbTitle, strbTitle.Capacity + 1);
                string title = strbTitle.ToString();

                StringBuilder strbClassname = new StringBuilder(256);
                int nRet = GetClassName(hWnd, strbClassname, strbClassname.Capacity);
                string classname = strbClassname.ToString();

                if (!string.IsNullOrEmpty(title) &&
                    !string.IsNullOrEmpty(classname) &&
                    classname.Equals("SDL_app") &&
                    title.Equals("Steam"))
                {
                    steamMainWindow = hWnd;
                }
                return true;
            };
            EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero);
            return steamMainWindow;
        }
        public static IntPtr WaitForWindow(int timeout, Func<IntPtr> TryGetWindow, CancellationToken token)
        {
            IntPtr window = TryGetWindow();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (window == IntPtr.Zero && sw.Elapsed < TimeSpan.FromSeconds(timeout) && !token.IsCancellationRequested)
            {
                window = TryGetWindow();
                Thread.Sleep(200);
            }
            return window;
        }

        public static bool SteamAutoLogin(string username,string password,bool remember,int timeout, CancellationToken token)
        {
            try
            {
                // Waiting for steam login window
                IntPtr steamLoginWindowHandle = Utils.WaitForWindow(timeout, Utils.TryGetSteamLoginWindow, token);
                Console.WriteLine("Input Account");
                if (steamLoginWindowHandle == IntPtr.Zero)
                {
                    return false;
                }
                using (var automation = new UIA3Automation())
                {
                    AutomationElement window = automation.FromHandle(steamLoginWindowHandle);
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    while (!window.IsAvailable && !window.IsOffscreen)
                    {
                        Thread.Sleep(200);
                    }
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    // Getting window document
                    AutomationElement document = window.FindFirstDescendant(e => e.ByControlType(ControlType.Document));
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    // Getting textboxes from document
                    List<TextBox> textBoxes = document.FindAllChildren(e => e.ByControlType(ControlType.Edit)).Select(edit => edit.AsTextBox()).ToList();
                    // If steam window is still not login window
                    while (textBoxes.Count != 2)
                    {
                        Utils.ForceWindowToForeground(steamLoginWindowHandle);
                        var imageControlTypeCount = document.FindAllChildren(e => e.ByControlType(ControlType.Image)).Count();
                        var textControlTypeCount = document.FindAllChildren(e => e.ByControlType(ControlType.Text)).Count();
                        var groupContolTypeCount = document.FindAllChildren(e => e.ByControlType(ControlType.Group)).Count();
                        // If new shitty update
                        if (imageControlTypeCount == 2 && textControlTypeCount == 1 && groupContolTypeCount > 1)
                        {
                            Button addAccountButton = document.FindAllChildren().Last().AsButton();
                            Utils.ForceWindowToForeground(steamLoginWindowHandle);
                            addAccountButton.Focus();
                            addAccountButton.WaitUntilEnabled();
                            addAccountButton.Invoke();
                            Thread.Sleep(30);
                        }
                        // Refresh textboxes
                        textBoxes = document.FindAllChildren(e => e.ByControlType(ControlType.Edit)).Select(edit => edit.AsTextBox()).ToList();
                        Thread.Sleep(50);
                    }
                    // Getting remember password checkbox and its state
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    Button checkBox = document.FindFirstChild(e => e.ByControlType(ControlType.Group)).AsButton();
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    bool rememberPasswordState = checkBox.FindFirstChild(e => e.ByControlType(ControlType.Image)) != null;

                    // Getting login in button
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    Button loginButton = document.FindFirstChild(e => e.ByControlType(ControlType.Button)).AsButton();

                    // Writing login
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    textBoxes[0].Focus();
                    textBoxes[0].WaitUntilEnabled();
                    textBoxes[0].Text = username;

                    // Writing password
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    textBoxes[1].Focus();
                    textBoxes[1].WaitUntilEnabled();
                    textBoxes[1].Text = password;

                    // Clicking on remember password checkbox
                    if (remember)
                    {
                        Utils.ForceWindowToForeground(steamLoginWindowHandle);
                        checkBox.Focus();
                        checkBox.WaitUntilEnabled();
                        checkBox.Invoke();
                    }

                    // Clicking on login in button
                    Utils.ForceWindowToForeground(steamLoginWindowHandle);
                    loginButton.Focus();
                    loginButton.WaitUntilEnabled();
                    loginButton.Invoke();
                }
                return true;
            }
            catch (Exception ex)
            {
                return SteamAutoLogin(username, password, remember, timeout, token);
            }
        }
    }
}
