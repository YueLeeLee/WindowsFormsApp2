using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    [Serializable]
    class UnityMessage
    {
        public string type { get; set; }
        public object data { get; set; }
        public long handle { get; set; }
    }

    [Serializable]
    class LoginInfo
    {
        public string userName { get; set; }
        public string passWord { get; set; }
    }
}
