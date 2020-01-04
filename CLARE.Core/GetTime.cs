using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLARE.Core
{
    namespace EX_SCALE
    {
        public enum TIME_SCALE
        {
            TIME_1MS = 1000,
        }
    }

    public class GetTime
    {
        #region private variable
        private bool m_bIsTimeUp = false;
        private int m_nTimeScale = 0;
        private System.DateTime dtTimerStart = System.DateTime.Now;
        private System.DateTime dtTimerEnd = System.DateTime.Now;
        #endregion

        #region private function
        #endregion


        #region public variable
        #endregion

        #region public function
        public void TimeStart(int msStart)
        {
            m_bIsTimeUp = false;
            dtTimerStart = System.DateTime.Now;
            System.TimeSpan tsTimeSpan = new TimeSpan(0, 0, 0, 0, msStart);
            dtTimerEnd = System.DateTime.Now + tsTimeSpan;
        }
        public bool TimeUp()
        {
            if (System.DateTime.Now < dtTimerEnd) m_bIsTimeUp = false;
            else m_bIsTimeUp = true;
            return m_bIsTimeUp;
        }
        public void TimeGo()
        {
            m_bIsTimeUp = false;
            dtTimerStart = System.DateTime.Now;
            dtTimerEnd = System.DateTime.Now;
        }
        public System.TimeSpan TimePass()
        {
            System.TimeSpan tsPass = System.DateTime.Now - dtTimerStart;
            return tsPass;
        }
        #endregion


        public GetTime(EX_SCALE.TIME_SCALE unit)
        {
            m_nTimeScale = (int)unit;
            dtTimerStart = System.DateTime.Now;
            dtTimerEnd = System.DateTime.Now;
        }

        public GetTime(GetTime getTime)
        {
            m_nTimeScale = getTime.m_nTimeScale;
            dtTimerStart = getTime.dtTimerStart;
            dtTimerEnd = getTime.dtTimerEnd;
        }
    }
}
