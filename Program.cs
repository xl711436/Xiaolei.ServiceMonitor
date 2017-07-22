using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Xiaolei.TraceLib;

namespace Xiaolei.ServiceMonitor
{
    static class Program
    {

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(int threadId, uint msg, IntPtr wParam, IntPtr lParam);

        public static int MessageCode_Stop = 0x80F0;

        public static string CurrentProcessPath = string.Empty;

        public static string MutexName = ConfigurationManager.AppSettings["MutexName"];

        static void Main(string[] args)
        {

            CurrentProcessPath = Process.GetCurrentProcess().MainModule.FileName;
            System.Diagnostics.Trace.WriteLine(CurrentProcessPath);
            if (args.Length == 1)
            {
                if (args[0].ToLower() == "start")
                {
                    bool createdNew;
                    System.Threading.Mutex instance = new System.Threading.Mutex(true, MutexName, out createdNew);
                    if (createdNew)
                    {
                        TraceHelper.TraceInfo("start mutex in");
                        ServiceMonitor curInstance = new ServiceMonitor();
                        curInstance.ExitFlag = false;
                        Application.AddMessageFilter(new MsgFilter(curInstance));
                        curInstance.Start();

                        //维持循环，接收推出消息
                        while (!curInstance.ExitFlag)
                        {
                            Application.DoEvents();
                            System.Threading.Thread.Sleep(1000);
                        }

                        TraceHelper.TraceInfo("ReleaseMutex");
                        instance.ReleaseMutex();
                    }
                    else
                    {
                        TraceHelper.TraceInfo("start mutex not  in");
                    }
                }

                if (args[0].ToLower() == "stop")
                {

                    bool createdNew;
                    System.Threading.Mutex instance = new System.Threading.Mutex(true, MutexName, out createdNew);
                    if (createdNew)
                    {
                        TraceHelper.TraceInfo("stop mutex   in");
                        instance.ReleaseMutex();
                    }
                    else
                    {
                        TraceHelper.TraceInfo("stop mutex  not in");
                        Process[] curProcessArray = Process.GetProcesses();

                        foreach (Process curProcess in curProcessArray)
                        {
                            try
                            {
                                if (curProcess.MainModule.FileName == CurrentProcessPath)
                                {
                                    System.Diagnostics.Trace.WriteLine("post stop message " + curProcess.Id);
                                    PostThreadMessage(curProcess.Threads[0].Id, (uint)MessageCode_Stop, IntPtr.Zero, IntPtr.Zero);
                                }
                            }
                            catch
                            {

                            }
                        }
                        TraceHelper.TraceInfo("exit");
                        Application.Exit();
                    }
                }
            }
        }
    }

    /// <summary>监听线程消息
    /// </summary>
    public class MsgFilter : IMessageFilter
    {
        private ServiceMonitor curInstance;

        public MsgFilter(ServiceMonitor I_Instance)
        {
            curInstance = I_Instance;
        }

        public bool PreFilterMessage(ref Message m)
        {
            //如果收到退出消息，退出程序
            if (m.Msg == Program.MessageCode_Stop)
            {
                System.Diagnostics.Trace.WriteLine("receive stop message");
                curInstance.Stop();
                curInstance.ExitFlag = true;

                return true;
            }
            return false;
        }
    }


}
