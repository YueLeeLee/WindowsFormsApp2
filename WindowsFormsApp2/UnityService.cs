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
    

    class UnityService
    {
        static UnityService s_instance;

        static IntPtr s_handle;

        public delegate void OnHideWindowRequest();

        OnHideWindowRequest m_onHideWindow = null;

        Queue<string> m_messageQueue;
        NamedPipeServerStream m_pipeServer;

        // TEST public
        public bool m_hideWindow = false;

        object _msgQLock = new object();
        object _fileLock = new object();

        int PipeInOutBufferSize = 2048;

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

        public void RigisterHideEvent(OnHideWindowRequest cb)
        {
            m_onHideWindow = cb;
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
            System.Timers.Timer timer = new System.Timers.Timer(100);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTick);//到达时间的时候执行事件；
            timer.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            timer.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；

            m_messageQueue = new Queue<string>();
        }

        void OnTick(object source, System.Timers.ElapsedEventArgs e)
        {
            //if (m_hideWindow && m_onHideWindow != null) 
            //{
            //    m_onHideWindow.Invoke();
            //    m_hideWindow = false;
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


        public void CheckLicense(string filepath)
        {
            //license:check
            UnityMessage um = new UnityMessage();
            um.type = "license:check";
            um.data = filepath;

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

            //m_pipeServer.BeginWaitForConnection(WaitForConnectionCallback, m_pipeServer);


            Console.WriteLine("----------------- UnityService OnStartUp");
            ThreadPool.QueueUserWorkItem(delegate
            {
                Console.WriteLine("-----------------Form1_Load 2 ");
                m_pipeServer.BeginWaitForConnection((o) =>
                {
                    WriteLog("Connect came ");

                    NamedPipeServerStream pServer = (NamedPipeServerStream)o.AsyncState;
                    pServer.EndWaitForConnection(o);

                    var data = new byte[PipeInOutBufferSize];

                    var count = pServer.Read(data, 0, PipeInOutBufferSize);

                    //StreamReader sr = new StreamReader(pServer);
                    //string buffer = sr.ReadToEnd();

                    if (count > 0)
                    {
                        string buffer = Encoding.Default.GetString(data, 0, count-2);
                        //buffer = "{\"type\":\"window:show\"}"; //"{\"type\":\"window:show\",\"data\":{},\"handle\":0}\n";

                        JObject jo = (JObject)JsonConvert.DeserializeObject(buffer);
                        string msgtype = jo["type"].ToString();
                        string mdata = jo["data"].ToString();
                        //UnityMessage msg = JsonSerializer.Deserialize<UnityMessage>(buffer);
                        if(!string.IsNullOrEmpty(msgtype))
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
                            else if(msgtype == "window:hide")
                            {
                                m_hideWindow = true;
                                needsendmsg = false;
                            }
                            else if (msgtype == "license:return" && mdata == "success")
                            {
                                needsendmsg = false;
                            }

                            if(needsendmsg)
                            {
                                string message = GetFormatedMsgFromJson(um);

                                lock (_msgQLock)
                                {
                                    m_messageQueue.Enqueue(message);
                                }
                                WriteLog("sended message");
                            }
                        }


                        //HandleMessage(buffer);
                    }
                    /*
                    //lock (_fileLock)
                    {
                        string testFile = string.Format("C:\\Users\\user\\Desktop\\hello{0}.txt", buffer.Length);//  + buffer.Length() + ".txt";
                        FileStream fs = null;
                        if (!File.Exists(testFile))
                        {
                            fs = File.Create(testFile);
                        }

                        if (fs != null)
                        {
                            if (buffer.Length > 0)
                            {
                                StreamWriter sw = new StreamWriter(fs);
                                sw.Write(buffer);
                                sw.Close();
                            }
                            fs.Close();
                        }
                    }
                    */

                }, m_pipeServer);
            });


        }
    }
}
