using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using LunkerLibrary;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;

/// <summary>
/// Set up Connection w/FE
/// </summary>

namespace FEConnectionManager
{

    struct FEServer{
       
        public Socket socket;
        public bool wait;
      
        public FEServer(Socket s)
        {
            socket = s;
            wait = true;        
        }
    }

    class FEConnectionController
    {
        public List<FEServer> feList;
        public Task<Socket> acceptTask;
        Socket listenSocket;
        IPEndPoint ep;
        int port;
        int backlog = 10;
        CommonHeader requestHeader = new CommonHeader(MessageType.BENotice, MessageState.Request, 0, new Cookie(), new UserInfo());


        /// </summary>
        public FEConnectionController()
        {         
            feList = new List<FEServer>();
            acceptTask = null;
            port = 50010;

            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ep = new IPEndPoint(IPAddress.Any, port);
            listenSocket.Bind(ep);
            listenSocket.Listen(backlog);
            Console.WriteLine("[FE][Listen]"+port);
        }
       
        public async void AcceptConnectionAsync()
        {
            if(acceptTask != null)
            {
                if (acceptTask.IsCompleted)
                {
                    Console.WriteLine("[FE][Accept][Await]");
                    Socket serverSock = await AcceptAsync();
                    Console.WriteLine("[FE][AcceptConn][Success]");
                    feList.Add(new FEServer(serverSock));

                    // send info request
                    Console.WriteLine("[FE][Accept][Await2]");
                    await NetworkManager.SendAsync(serverSock, requestHeader);
                    Console.WriteLine("[FE][Accept][Response]"+ IPAddress.Parse(((IPEndPoint)serverSock.RemoteEndPoint).Address.ToString()));
                    Console.WriteLine("Current FE Number : " + feList.Count);
                }
            }
            else
            {
                Console.WriteLine("[FE][FirstAccept]");
                Console.WriteLine("[FE][Accept][Await]");
                Socket serverSock = await AcceptAsync();
                Console.WriteLine("[FE][Accept][Success]");
                feList.Add(new FEServer(serverSock));

                // send info request
                Console.WriteLine("[FE][Accept][Await2]");
                await NetworkManager.SendAsync(serverSock, requestHeader);
                Console.WriteLine("[FE][Accept][Response]" + IPAddress.Parse(((IPEndPoint)serverSock.RemoteEndPoint).Address.ToString()));
                Console.WriteLine("Current FE Number : " + feList.Count);
            }          
        }  

        private Task<Socket> AcceptAsync()
        {
            acceptTask = Task.Run<Socket>(() =>
            {
                Socket socket;

                try
                {
                    socket = listenSocket.Accept();
                }
                catch (Exception e)
                {
                    socket = null;
                    Console.WriteLine(e.Message);
                    Console.WriteLine("[FE][Accept][Error]");
                }

                Console.WriteLine("[FE][Accept][Success]");
                return socket;
            });
            
            return acceptTask;
        }
        
    }


}
