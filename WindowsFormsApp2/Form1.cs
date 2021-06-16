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
        private string m_licenseKey;
        private int m_tipTickCount = 0;

        private List<Control> m_loginControls = new List<Control>();
        private List<Control> m_licenseControls = new List<Control>();

        public Form1()
        {
            InitializeComponent();
            InitControlLists();
            RefreshWindow();

            this.textBox1.Text = "pengyue.li@unity3d.com"; //"lpy12270@163.com";
            this.textBox2.Text = "Lpyunity123"; //"Lpy1314159";
            this.textBox3.Text = "SC-MADP-CFPC-JXD5-TMDR-EEJR";

            Console.WriteLine("Form1 show ?  ....");

        }

        void InitControlLists()
        {
            if(m_loginControls.Count <= 0)
            {
                m_loginControls.Add(this.label1);
                m_loginControls.Add(this.label2);
                m_loginControls.Add(this.label5);
                m_loginControls.Add(this.textBox1);
                m_loginControls.Add(this.textBox2);
                m_loginControls.Add(this.button1);
            }

            if(m_licenseControls.Count <= 0)
            {
                m_licenseControls.Add(this.label3);
                m_licenseControls.Add(this.textBox3);
                m_licenseControls.Add(this.button3);
                m_licenseControls.Add(this.label6);
            }
        }

        void DoHideWindow()
        {
            if (button1.InvokeRequired)
            {
                button1.Invoke(new MethodInvoker(delegate
                {
                    this.Hide();
                }));
            }
        }

        void DoCloseWindow()
        {
            Application.ExitThread();
            //if (button1.InvokeRequired)
            //{
            //    button1.Invoke(new MethodInvoker(delegate
            //    {
            //        this.Close();
            //    }));
            //}
        }


        protected override void OnActivated(EventArgs e)
        {
            this.CenterToScreen();
        }

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

        protected override void OnClosing(CancelEventArgs e)
        {
            Console.WriteLine("onclosing ....");
            //UnityService.Instance.OnClientClose();
            UnityService.Instance.DisconnectPipe();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            m_licenseKey = this.textBox3.Text;

            if (string.IsNullOrEmpty(m_licenseKey))
            {
                ShowTip("请输入激活码");
                return;
            }

            UnityService.Instance.CheckLicense(m_licenseKey);
        }

        void ShowTip(string tip)
        {
            this.label4.Text = tip;
            m_tipTickCount = 3;
        }

        void OnTimerTick(object o, EventArgs e)
        {
            if(m_tipTickCount > 0)
            {
                m_tipTickCount--;
            }
            else if(this.label4.Text.Length > 0)
            {
                this.label4.Text = "";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.timer1.Tick += this.OnTimerTick;
            this.timer1.Interval = 1000;
            this.timer1.Start();

            UnityService.WinHandle = this.Handle;
            UnityService.Instance.Awake();
            UnityService.Instance.RigisterHideEvent(this.DoHideWindow);
            UnityService.Instance.RigisterCloseEvent(this.DoCloseWindow);
            UnityService.Instance.RigisterLoginEvent(this.OnGotLoginResult);
        }

        void OnGotLoginResult()
        {
            if (button1.InvokeRequired)
            {
                button1.Invoke(new MethodInvoker(delegate
                {
                    RefreshWindow();
                }));
            }
        }

        void RefreshWindow()
        {
            bool loginVisible = true;
            bool licenseVisible = false;
            switch (UnityService.Instance.CurrentState)
            {
                case UnityState.kLoggedOut:
                    {
                        loginVisible = true;
                        licenseVisible = false;
                    }
                    break;
                case UnityState.kLoggedInUnActivated:
                    {
                        loginVisible = false;
                        licenseVisible = true;
                    }
                    break;
                case UnityState.kLoggedInActivated:
                    {
                        Console.WriteLine("Going to hide window ");
                        this.DoCloseWindow();
                        return;
                    }
            }

            foreach(var c in m_loginControls)
            {
                c.Visible = loginVisible;
            }

            foreach(var c in m_licenseControls)
            {
                c.Visible = licenseVisible;
            }

            if(UnityService.Instance.LastErrorMessage.Length > 0)
            {
                string tip = UnityService.Instance.LastErrorMessage;
                ShowTip(tip);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            string username = this.textBox1.Text;
            string password = this.textBox2.Text;

            UnityService.Instance.DoUnityLogin(username, password);
        }

    }
}
