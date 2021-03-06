﻿using Mubox.Model.Input;
using System;
using System.Runtime.InteropServices;

namespace Mubox.Control.Input.Hooks
{
    public static class MouseInputHook
    {
        static MouseInputHook()
        {
            hookProc = new WinAPI.WindowHook.HookProc(MouseHook);
            hookProcPtr = Marshal.GetFunctionPointerForDelegate(hookProc);
            MouseInputPerformance = Performance.CreatePerformance("_MouseInput");
        }

        private static Performance MouseInputPerformance = null;
        private static WinAPI.WindowHook.HookProc hookProc = null;
        private static IntPtr hookProcPtr = IntPtr.Zero;
        private static long _nextMouseMoveAccept = 0L;

        public static UIntPtr MouseHook(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode == 0)
                {
                    var wm = (WinAPI.WM)wParam.ToUInt32();
                    if (wm == WinAPI.WM.MOUSEMOVE)
                    {
                        if (DateTime.Now.Ticks <= _nextMouseMoveAccept)
                        {
                            // a.k.a not handled
                            return Mubox.WinAPI.WindowHook.CallNextHookEx(hHook, nCode, wParam, lParam);
                        }
                        _nextMouseMoveAccept = DateTime.Now.AddMilliseconds(50).Ticks; // 20fps - we limit this to ease off of network and cpu utilization for mousemove, the framerate choice here is arbitrary
                    }
                    Mubox.WinAPI.WindowHook.MSLLHOOKSTRUCT mouseHookStruct = (Mubox.WinAPI.WindowHook.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Mubox.WinAPI.WindowHook.MSLLHOOKSTRUCT));
                    if (OnMouseInputReceived(wm, mouseHookStruct))
                    {
                        return new UIntPtr(1); // handled
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            try
            {
                return Mubox.WinAPI.WindowHook.CallNextHookEx(hHook, nCode, wParam, lParam);
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            return UIntPtr.Zero;
        }

        private static bool isStarted;
        private static IntPtr hHook = IntPtr.Zero;
        private static System.Windows.Threading.Dispatcher dispatcher;

        public static void Start()
        {
            if (isStarted)
                return;
            isStarted = true;

            if (dispatcher == null)
            {
                dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            }

            //            IntPtr nextHook = IntPtr.Zero // COMMENTED BY CODEIT.RIGHT;
            //IntPtr dwThreadId = Win32.Threads.GetCurrentThreadId();
            IntPtr hModule = Marshal.GetHINSTANCE(System.Reflection.Assembly.GetEntryAssembly().GetModules()[0]);
            hHook = WinAPI.WindowHook.SetWindowsHookEx(WinAPI.WindowHook.HookType.WH_MOUSE_LL, hookProcPtr, hModule, 0);
            if (hHook == IntPtr.Zero)
            {
                // failed
                isStarted = false;
                ("MSHOOK: Hook Failed winerr=0x" + Marshal.GetLastWin32Error().ToString("X")).Log();
            }
        }

        public static void Stop()
        {
            if (!isStarted)
                return;
            isStarted = false;

            if (hHook != IntPtr.Zero)
            {
                Mubox.WinAPI.WindowHook.UnhookWindowsHookEx(hHook);
                //("MSHOOK: Unhook Success.").Log();
                hHook = IntPtr.Zero;
            }
        }

        public static event EventHandler<MouseInput> MouseInputReceived;

        private static bool OnMouseInputReceived(WinAPI.WM wm, WinAPI.WindowHook.MSLLHOOKSTRUCT hookStruct)
        {
            // ignore injected input
            if (hookStruct.flags.HasFlag(WinAPI.WindowHook.LLMHF.INJECTED))
            {
                return false;
            }

            MouseInput mouseInputEventArgs = MouseInput.CreateFrom(wm, hookStruct);

            if (Performance.IsPerformanceEnabled)
            {
                MouseInputPerformance.Count(Convert.ToInt64(mouseInputEventArgs.Time));
            }
            try
            {
                if (MouseInputReceived != null)
                {
                    MouseInputReceived(null, mouseInputEventArgs);
                    return mouseInputEventArgs.Handled;
                }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            return false;
        }
    }
}