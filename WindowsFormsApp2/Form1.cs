using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Pipes;
using System.Threading;


namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private bool m_toHide = false;
        private string m_licensePath;

        //System.Timers.Timer m_timer = new System.Timers.Timer();

        public Form1()
        {
            InitializeComponent();

            UnityService.WinHandle = this.Handle;
            this.CenterToScreen();
            //UnityService.Instance.RigisterHideEvent(this.HideWindow);

            //System.Timers.Timer t = new System.Timers.Timer(100);
            //t.Elapsed += new System.Timers.ElapsedEventHandler(this.TickToHide);//到达时间的时候执行事件；
            //t.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            //t.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
        }

        //void HideWindow()
        //{
        //    m_toHide = true;
        //    Console.WriteLine("how many times does this called...");
        //}


        //protected override void OnActivated(EventArgs e)
        //{
        //    this.Hide();
        //}

        //void TickToHide(object source, System.Timers.ElapsedEventArgs e)
        //{
        //    if(m_toHide)
        //    {
        //        Console.WriteLine(Thread.CurrentThread.Name);
        //        this.Hide();
        //        m_toHide = false;
        //    }
        //}

        //protected override void OnClosed(EventArgs e)
        //{
        //    Console.WriteLine("onclosed ....");
        //}

        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    Console.WriteLine("onclosing ....");
        //    UnityService.Instance.OnClientClose();

        //    while(!UnityService.Instance.CloseMsgSended)
        //    {
        //        Thread.Sleep(50);
        //    }
        //}

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            m_licensePath = this.openFileDialog1.FileName;
            this.textBox3.Text = m_licensePath;
            Console.WriteLine(m_licensePath);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(m_licensePath))
                return;

            if (!File.Exists(m_licensePath))
                return;

            if (!m_licensePath.EndsWith(".ulf"))
                return;

            UnityService.Instance.CheckLicense(m_licensePath);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //this.Hide();
            UnityService.Instance.m_hideWindow = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Console.WriteLine("clicked to return license ....");
            Console.WriteLine(UnityService.Instance.Connected);

            UnityService.Instance.ReturnLicense();
        }

    }
}
