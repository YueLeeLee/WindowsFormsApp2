using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace WindowsFormsApp2
{
    class NetWorkManager
    {
        string m_serverURL = "https://sso.zhiyinlou.com/portal/login/777?";
        public void SendMessageLogin(string id, string password)
        {
            string url = string.Format("{0}param1={1}&param2={2}", m_serverURL, id, password);
            HttpWebRequest wr = WebRequest.CreateHttp(url);
            wr.Method = WebRequestMethods.Http.Post;
            
        }
    }
}
