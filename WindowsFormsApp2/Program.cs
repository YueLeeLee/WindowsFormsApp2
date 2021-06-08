using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Text.Json;

namespace WindowsFormsApp2
{
    static class Program
    {
        //static Form m_MainFrom;
        //static Queue<string> m_messageQueue;
        //static NamedPipeServerStream m_pipeServer;

        //static object _msgQLock = new object();
        //static object _fileLock = new object();

        //static int PipeInOutBufferSize = 2048;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            UnityService.Instance.Awake();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //WriteLog("Application started");
            //SetUpTimer();
            //m_MainFrom = new Form1();
            //m_MainFrom.CreateControl();
            //OnStartUp();
            //Application.Run(m_MainFrom);
            //Form1 f = new Form1();
            //f.SendToBack();
            Application.Run(new Form1());
        }

        
    }
}
