using System;
using System.Net;

namespace Assets.Scripts.RuntimeUnitTestToolkit
{
    public class User
    {
        public IPEndPoint clientIPEndPoint;
        public string v;

        public User(string v, IPEndPoint clientIPEndPoint)
        {
            this.v = v;
            this.clientIPEndPoint = clientIPEndPoint;
        }

        public IPEndPoint GetIPEndPoint()
        {
            IPEndPoint ipEndPoint = new IPEndPoint(new IPAddress(1), 1);
            return ipEndPoint;
        }

        public string GetName()
        {
            string str = "lalala";
            return str;
        }
    }
}