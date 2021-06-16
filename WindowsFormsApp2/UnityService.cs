using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Threading;
using System.Text.Json;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace WindowsFormsApp2
{
    public enum UnityState
    {
        kLoggedOut = 0,
        kLoggedInUnActivated,
        kLoggedInActivated,
        Count,
    }

    class UnityService
    {
        static UnityService s_instance;

        static IntPtr s_handle;

        public delegate void OnWindowOptionRequest();

        OnWindowOptionRequest m_onHideWindow = null;
        OnWindowOptionRequest m_onCloseWindow = null;
        OnWindowOptionRequest m_onGotLoginResult = null;

        Queue<string> m_messageQueue;
        NamedPipeServerStream m_pipeServer;
        System.Timers.Timer m_timer;

        bool m_waitingMessage = false;

        bool m_hideWindow = false;
        bool m_closeWindow = false;

        object _msgQLock = new object();
        object _fileLock = new object();
        ReaderWriterLock _rwlock = new ReaderWriterLock();

        int PipeInOutBufferSize = 2048;

        UnityState m_currentState = UnityState.kLoggedOut;

        string m_lastErrorMessage = "";

        public static UnityService Instance { 
            get
            {
                if (s_instance == null)
                    s_instance = new UnityService();

                return s_instance;
            }
        }

        public static IntPtr WinHandle
        {
            set { s_handle = value; }
            get { return s_handle; }
        }

        public bool CloseMsgSended
        {
            get; set;
        }

        public bool Connected
        {
            get 
            { 
                if (m_pipeServer != null)
                    return m_pipeServer.IsConnected;
                return false;
            }
        }

        public UnityState CurrentState
        {
            get { return m_currentState; }
        }

        public string LastErrorMessage
        {
            get { return m_lastErrorMessage; }
        }

        public void RigisterHideEvent(OnWindowOptionRequest cb)
        {
            m_onHideWindow = cb;
        }

        public void RigisterCloseEvent(OnWindowOptionRequest cb)
        {
            m_onCloseWindow = cb;
        }

        public void RigisterLoginEvent(OnWindowOptionRequest cb)
        {
            m_onGotLoginResult = cb;
        }

        public void Awake()
        {
            string testFile = string.Format("C:\\Users\\user\\Desktop\\log.txt");
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }

            CloseMsgSended = false;

            SetUpTimer();
            OnStartUp();
        }


        void SetUpTimer()
        {
            System.Timers.Timer m_timer = new System.Timers.Timer(100);
            m_timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTick);//到达时间的时候执行事件；
            m_timer.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            m_timer.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；


            m_messageQueue = new Queue<string>();
        }

        void OnTick(object source, System.Timers.ElapsedEventArgs e)
        {
            if (m_hideWindow && m_onHideWindow != null)
            {
                m_onHideWindow.Invoke();
                m_hideWindow = false;
            }

            //if (m_closeWindow && m_onCloseWindow != null)
            //{
            //    Console.WriteLine("Going to close window ......");
            //    m_onCloseWindow.Invoke();
            //    m_closeWindow = false;
            //}

            if (m_messageQueue.Count > 0)
            {
                WriteLog("OnTick : m_messageQueue.Count > 0");
                if (m_pipeServer.IsConnected)
                {
                    try
                    {

                        long handle = (long)s_handle;
                        if (handle <= 0)
                            return;

                        string message = "";
                        lock (_msgQLock)
                        {
                            message = m_messageQueue.Dequeue();
                        }

                        if (message != "")
                        {
                            //WriteLog(message);

                            Console.WriteLine("sending message : " + message);

                            byte[] data = Encoding.Default.GetBytes(message);

                            m_pipeServer.Write(data, 0, data.Length);
                            m_pipeServer.Flush();
                            m_pipeServer.WaitForPipeDrain();
                        }
                    }
                    catch { }
                }
                else
                {
                    Console.WriteLine("pipe server connection lost ....");
                }
            }
        }

        void WaitMsg()
        {

            Console.WriteLine("-----------------before  WaitForConnection");
            m_pipeServer.WaitForConnection();
            Console.WriteLine("-----------------after  WaitForConnection");

            while(m_pipeServer.IsConnected)
            {
                if(!m_pipeServer.CanRead)
                {
                    Thread.Sleep(100);
                    continue;
                }
                var data = new byte[PipeInOutBufferSize];
                var count = m_pipeServer.Read(data, 0, PipeInOutBufferSize);
                bool toDisconnect = false;

                if (count > 0)
                {
                    string buffer = Encoding.Default.GetString(data, 0, count - 2);

                    JObject jo = (JObject)JsonConvert.DeserializeObject(buffer);
                    string msgtype = jo["type"].ToString();
                    string mdata = jo["data"].ToString();
                    m_lastErrorMessage = "";
                    if (!string.IsNullOrEmpty(msgtype))
                    {
                        bool needsendmsg = true;
                        UnityMessage um = new UnityMessage();
                        if (msgtype == "window:show")
                        {
                            long handle = (long)s_handle;
                            um.type = "window:show";
                            um.data = "";
                            um.handle = handle;
                        }
                        else if (msgtype == "window:hide")
                        {
                            m_hideWindow = true;
                            needsendmsg = false;
                        }
                        else if(msgtype == "window:close")
                        {
                            toDisconnect = true;
                            m_closeWindow = true;
                            needsendmsg = false;
                        }
                        else if(msgtype == "user:login")
                        {
                            needsendmsg = false;
                            JObject jdata = (JObject)JsonConvert.DeserializeObject(mdata);
                            int result = (int)jdata["result"];
                            m_lastErrorMessage = jdata["error"].ToString();
                            if (result >= 0 && result < (int)UnityState.Count)
                            {
                                m_currentState = (UnityState)result;
                                LoginResult();
                            }
                        }
                        else if (msgtype == "license:check")
                        {
                            needsendmsg = false;
                            JObject jdata = (JObject)JsonConvert.DeserializeObject(mdata);
                            int result = (int)jdata["result"];
                            m_lastErrorMessage = jdata["error"].ToString();
                            if (result >= 0 && result < (int)UnityState.Count)
                            {
                                m_currentState = (UnityState)result;
                                LoginResult();
                            }
                        }
                        else if (msgtype == "license:return" && mdata == "success")
                        {
                            needsendmsg = false;
                        }

                        if (needsendmsg)
                        {
                            string message = GetFormatedMsgFromJson(um);

                            byte[] wdata = Encoding.Default.GetBytes(message);

                            m_pipeServer.Write(wdata, 0, wdata.Length);
                        }
                    }
                }

                try
                {
                    m_pipeServer.WaitForPipeDrain();
                    m_pipeServer.Flush();
                    if (toDisconnect)
                        m_pipeServer.Disconnect();
                }
                catch
                {

                }
            }

            //if(!m_pipeServer.IsConnected)
            //{
            //    WriteLog("In my exiting...");

            //   // m_pipeServer.Disconnect();
            //    m_pipeServer.Dispose();

            //    if (m_timer != null)
            //        m_timer.Stop();

            //    if (m_onCloseWindow != null)
            //    {
            //        Console.WriteLine("Going to close window ......");
            //        m_onCloseWindow.Invoke();
            //    }
            //}
        }

        void SendMessage(string msg)
        {
            Console.WriteLine("will send msg : " + msg);
            if (m_pipeServer.IsConnected && msg != "")
            {
                Console.WriteLine("sended msg : " + msg);
                byte[] data = Encoding.Default.GetBytes(msg);

                m_pipeServer.Write(data, 0, data.Length);
                m_pipeServer.Flush();
                m_pipeServer.WaitForPipeDrain();
            }
        }

        void WriteLog(string log)
        {
            lock (_fileLock)
            {
                string testFile = string.Format("C:\\Users\\user\\Desktop\\log.txt");
                if (!File.Exists(testFile))
                {
                    FileStream fs = File.Create(testFile);
                    fs.Close();
                }

                StreamWriter logWriter = new StreamWriter(testFile, true);
                logWriter.WriteLine(log);
                logWriter.Close();
            }
        }

        string GetFormatedMsgFromJson(object obj)
        {
            return JsonConvert.SerializeObject(obj) + "\f";
        }

        public void DoUnityLogin(string username, string password)
        {
            LoginInfo login = new LoginInfo();
            login.userName = username;
            login.passWord = password;

            UnityMessage um = new UnityMessage();
            um.type = "user:login";
            um.data = login;

            string msg = GetFormatedMsgFromJson(um);
            lock (_msgQLock)
                m_messageQueue.Enqueue(msg);
        }

        void LoginResult()
        {
            if (m_onGotLoginResult != null)
                m_onGotLoginResult.Invoke();
        }

        public void CheckLicense(string key)
        {
            //license:check
            UnityMessage um = new UnityMessage();
            um.type = "license:check";
            um.data = key;

            string msg = GetFormatedMsgFromJson(um);
            lock (_msgQLock)
                m_messageQueue.Enqueue(msg);
        }

        public void ReturnLicense()
        {
            UnityMessage um = new UnityMessage();
            um.type = "license:return";
            um.data = "";

            string msg = GetFormatedMsgFromJson(um);
            lock (_msgQLock)
                m_messageQueue.Enqueue(msg);
        }

        public void DisconnectPipe()
        {
            if(m_pipeServer!= null)
            {
                m_pipeServer.Disconnect();
                Console.WriteLine("pipe Disconnected .....");
            }
        }

        public void OnClientClose()
        {
            CloseMsgSended = false;
            UnityMessage um = new UnityMessage();
            um.type = "window:close";
            um.data = "";

            string msg = GetFormatedMsgFromJson(um);
            SendMessage(msg);

            CloseMsgSended = true;
            //lock (_msgQLock)
            //    m_messageQueue.Enqueue(msg);
        }

        void OnStartUp()
        {
            m_pipeServer = new NamedPipeServerStream("Unity-loginIPCService", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous | PipeOptions.WriteThrough, PipeInOutBufferSize, PipeInOutBufferSize);
            //m_pipeServer.WaitForConnection();
            //WaitMessage();
            Thread t = new Thread(this.WaitMsg);
            t.Start();

            
            Console.WriteLine("----------------- UnityService OnStartUp");
            

            Console.WriteLine("fk");
        }
    }
}
