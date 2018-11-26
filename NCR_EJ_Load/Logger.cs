using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NCR_EJ_Load
{
    class Logger
    {
        public void LogMsg(string _debugMsg)
        {
            string file_date;
            file_date = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2,'0') + DateTime.Now.Day.ToString().PadLeft(2,'0');
            File.AppendAllText("NCR_EJ_Load_" + file_date + ".log", DateTime.Now.Date.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " " + ":::: " + _debugMsg + Environment.NewLine);
        }
       
    }
}
