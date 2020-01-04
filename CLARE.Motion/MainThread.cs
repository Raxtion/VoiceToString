using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Xaml.Controls;

using CLARE.Core;
using CLARE.Data;
using VoiceToString_CSharp;

namespace CLARE.Motion
{
    public class MainThread
    {
        private IniFile g_IniFile = null;
        private RefCore g_RefCore = null;
        private MainPage g_MainPage = null;
        private bool m_bStopThread = false;

        public MainThread(ref RefCore refcore, ref MainPage page, ref IniFile iniFile)
        {
            g_IniFile = iniFile;
            g_RefCore = refcore;
            g_MainPage = page;

        }
        public bool IsThreadAlive() { return !m_bStopThread; }
        public void ThreadTerminate() { m_bStopThread = true; }
        public void MainThreadExecute(object obj)
        {
            GetTime tmMS = new GetTime(Core.EX_SCALE.TIME_SCALE.TIME_1MS);
            GetTime tmMainCycle = new GetTime(Core.EX_SCALE.TIME_SCALE.TIME_1MS);
            
            m_bStopThread = false;

            while (true)
            {
                TimeSpan ts = tmMainCycle.TimePass();
                //g_RefCore.m_listLog.Add(ts.ToString());
                tmMainCycle.TimeGo();

                if (m_bStopThread == true) break;

                Thread.Sleep(500);
            }

            g_RefCore.m_listLog.Add("Main Thread End");

        }



    }
}
