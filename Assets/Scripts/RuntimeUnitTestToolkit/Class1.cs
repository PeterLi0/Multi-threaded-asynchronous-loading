using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;

namespace Assets.Scripts.RuntimeUnitTestToolkit
{
    class Server : MonoBehaviour
    {
        IPAddress serverIp;
        IPEndPoint serverIPEndPoint;
        private string txbServerport = "";
        private string txbServerIP = "";
        UdpClient receiveUdpClient;
        Button btnStart = GameObject.Find("btn").GetComponent<Button>();
        Button btnStop = GameObject.Find("btn").GetComponent<Button>();
        private int tcpPort;
        private UdpClient sendUdpClient;
        private TcpListener tcpListener;
        private List<User> userList;
        private string userListstring;
        private NetworkStream networkStream;
        private BinaryWriter binaryWriter;
        private int port = 0;

        // 启动服务器
        // 根据博客中协议的设计部分
        // 客户端先向服务器发送登录请求，然后通过服务器返回的端口号
        // 再与服务器建立连接
        // 所以启动服务按钮事件中有两个套接字：一个是接收客户端信息套接字和
        // 监听客户端连接套接字
        private void btnStart_Click(object sender, EventArgs e)
        {
            serverIp = IPAddress.Parse(txbServerIP);
            serverIPEndPoint = new IPEndPoint(serverIp, int.Parse(txbServerport));
            receiveUdpClient = new UdpClient(serverIPEndPoint);
            // 启动接收线程
            Thread receiveThread = new Thread(ReceiveMessage);
            receiveThread.Start();
            btnStart.enabled = false;
            btnStop.enabled = true;

            // 随机指定监听端口
            System.Random random = new System.Random();
            tcpPort = random.Next(port + 1, 65536);

            // 创建监听套接字
            tcpListener = new TcpListener(serverIp, tcpPort);
            tcpListener.Start();

            // 启动监听线程
            Thread listenThread = new Thread(ListenClientConnect);
            listenThread.Start();
            AddItemToListBox(string.Format("服务器线程{0}启动，监听端口{1}", serverIPEndPoint, tcpPort));
        }

        private void AddItemToListBox(string v)
        {
            throw new NotImplementedException();
        }

        // 接收客户端发来的信息
        private void ReceiveMessage()
        {
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    // 关闭receiveUdpClient时下面一行代码会产生异常
                    byte[] receiveBytes = receiveUdpClient.Receive(ref remoteIPEndPoint);
                    string message = Encoding.Unicode.GetString(receiveBytes, 0, receiveBytes.Length);

                    // 显示消息内容
                    AddItemToListBox(string.Format("{0}:{1}", remoteIPEndPoint, message));

                    // 处理消息数据
                    // 根据协议的设计部分，从客户端发送来的消息是具有一定格式的
                    // 服务器接收消息后要对消息做处理
                    string[] splitstring = message.Split(',');
                    // 解析用户端地址
                    string[] splitsubstring = splitstring[2].Split(':');
                    IPEndPoint clientIPEndPoint = new IPEndPoint(IPAddress.Parse(splitsubstring[0]), int.Parse(splitsubstring[1]));
                    switch (splitstring[0])
                    {
                        // 如果是登录信息，向客户端发送应答消息和广播有新用户登录消息
                        case "login":
                            User user = new User(splitstring[1], clientIPEndPoint);
                            // 往在线的用户列表添加新成员
                            userList.Add(user);
                            AddItemToListBox(string.Format("用户{0}({1})加入", user.GetName(), user.GetIPEndPoint()));
                            string sendString = "Accept," + tcpPort.ToString();
                            // 向客户端发送应答消息
                            SendtoClient(user, sendString);
                            AddItemToListBox(string.Format("向{0}({1})发出：[{2}]", user.GetName(), user.GetIPEndPoint(), sendString));
                            for (int i = 0; i < userList.Count; i++)
                            {
                                if (userList[i].GetName() != user.GetName())
                                {
                                    // 给在线的其他用户发送广播消息
                                    // 通知有新用户加入
                                    SendtoClient(userList[i], message);
                                }
                            }

                            AddItemToListBox(string.Format("广播：[{0}]", message));
                            break;
                        case "logout":
                            for (int i = 0; i < userList.Count; i++)
                            {
                                if (userList[i].GetName() == splitstring[1])
                                {
                                    AddItemToListBox(string.Format("用户{0}({1})退出", userList[i].GetName(), userList[i].GetIPEndPoint()));
                                    userList.RemoveAt(i); // 移除用户
                                }
                            }
                            for (int i = 0; i < userList.Count; i++)
                            {
                                // 广播注销消息
                                SendtoClient(userList[i], message);
                            }
                            AddItemToListBox(string.Format("广播:[{0}]", message));
                            break;
                    }
                }
                catch
                {
                    // 发送异常退出循环
                    break;
                }
            }
            AddItemToListBox(string.Format("服务线程{0}终止", serverIPEndPoint));
        }

        private void SendtoClient(object p, string message)
        {
            throw new NotImplementedException();
        }

        // 向客户端发送消息
        private void SendtoClient(User user, string message)
        {
            // 匿名方式发送
            sendUdpClient = new UdpClient(0);
            byte[] sendBytes = Encoding.Unicode.GetBytes(message);
            IPEndPoint remoteIPEndPoint = user.GetIPEndPoint();
            sendUdpClient.Send(sendBytes, sendBytes.Length, remoteIPEndPoint);
            sendUdpClient.Close();
        }

        // 接受客户端的连接
        private void ListenClientConnect()
        {
            TcpClient newClient = null;
            while (true)
            {
                try
                {
                    newClient = tcpListener.AcceptTcpClient();
                    AddItemToListBox(string.Format("接受客户端{0}的TCP请求", newClient.Client.RemoteEndPoint));
                }
                catch
                {
                    AddItemToListBox(string.Format("监听线程({0}:{1})", serverIp, tcpPort));
                    break;
                }

                Thread sendThread = new Thread(SendData);
                sendThread.Start(newClient);
            }
        }

        // 向客户端发送在线用户列表信息
        // 服务器通过TCP连接把在线用户列表信息发送给客户端
        private void SendData(object userClient)
        {
            TcpClient newUserClient = (TcpClient)userClient;
            userListstring = null;
            for (int i = 0; i < userList.Count; i++)
            {
                userListstring += userList[i].GetName() + "," + userList[i].GetIPEndPoint().ToString() + ";";
            }

            userListstring += "end";
            networkStream = newUserClient.GetStream();
            binaryWriter = new BinaryWriter(networkStream);
            binaryWriter.Write(userListstring);
            binaryWriter.Flush();
            AddItemToListBox(string.Format("向{0}发送[{1}]", newUserClient.Client.RemoteEndPoint, userListstring));
            binaryWriter.Close();
            newUserClient.Close();
        }

    }
}

