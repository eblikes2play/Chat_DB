using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySQLManager;
using RedisManager;
using System.Net;
using System.Runtime.InteropServices;
using LunkerLibrary;
using System.Net.Sockets;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using FEConnectionManager;



namespace MessageManager
{

    /// <summary>
    /// Get Packet from FE connection, translate to MySQLManager vice versa.
    class MessageController
    {
        public FEConnectionController fe;
        MySQLController sqlCon;
        RedisController redCon;
        //Socket s;
        public bool connect = false;

        public MessageController(){
         
        }


        public async Task FirstSetting()
        {
            await Task.WhenAll(FEListen(), CreateMySqlConnection(), CreateRedisConnection());
            connect = true;
        }

        public void Start()
        {
            //서버마다 하나씩...!
            fe.AcceptConnectionAsync();

            int numOfServer = fe.feList.Count;

                if (numOfServer>0)
                {
                    for (int i =0; i<numOfServer; i++)
                    {
                        DataProcessAsync(fe.feList.ElementAt(i));
                    }
                }                                 
        }


        public Task CreateMySqlConnection()
        {
            return Task.Run(() =>
            {
                sqlCon = new MySQLController();
            });
        }

        public Task CreateRedisConnection()
        {
            return Task.Run(() =>
            {
                redCon = new RedisController();
            });
        }

        public Task FEListen()
        {
            return Task.Run(() =>
            {
                fe = new FEConnectionController();
            });
        }
       
        //RECEIVE DATA FROM SERVER
        public async Task RcvHeaderProcess(Socket sock)
        {     
                Console.WriteLine("[MM][ReceiveWait][Header]");
                object p = NetworkManager.Read(sock, Constants.HeaderSize, typeof(CommonHeader));
                CommonHeader newHeader = (CommonHeader)p;
                Console.WriteLine("[MM][Receive][Header]");

                MessageTypeHandler(sock, newHeader);
                Console.WriteLine("[MM][Send]");  
        }

        public LBModifyRequestBody RcvBodyProcess(Socket sock, int bodySize)
        {
                Console.WriteLine("[MM][ReceiveWait][Body][Modify]");
                object p = NetworkManager.Read(sock, bodySize, typeof(LBModifyRequestBody));
                Console.WriteLine("[MM][Receive][Modify]");

                return (LBModifyRequestBody) p;
           
        }

        public CBJoinRoomRequestBody RcvJoinBodyProcess(Socket sock, int bodySize)
        {
            Console.WriteLine("[MM][ReceiveWait][Body][Join]");
            object p = NetworkManager.Read(sock, bodySize, typeof(CBJoinRoomRequestBody));

            Console.WriteLine("[MM][Receive][Join]");
            return (CBJoinRoomRequestBody)p;
        }

        public CBLeaveRequestBody RcvLeaveBodyProcess(Socket sock, int bodySize)
        {
            Console.WriteLine("[MM][ReceiveWait][Body][Leave]");
            object p = NetworkManager.Read(sock, bodySize, typeof(CBJoinRoomRequestBody));

            Console.WriteLine("[MM][Receive][Request]");
            return (CBLeaveRequestBody)p;
        }





        public void MessageTypeHandler(Socket sock, CommonHeader newHeader)
        {            
            string id;
            string pw;
            string newPw;
            bool dummyFlag;
            int bodySize;
            int roomNumber;
            Cookie cookie;
            QueryResult result = new QueryResult();

            CommonHeader resultHeader = new CommonHeader();
            LBModifyRequestBody modUserInfo;
            CBCreateRoomResponseBody newRoomInfo;
            CBListRoomResponseBody roomListInfo;
            CBJoinRoomRequestBody joinRoomInfo;
            CBLeaveRequestBody leaveRoomInfo;

            switch ((int)newHeader.Type/100)
            {
                case 3:
                  
                    switch (newHeader.Type)
                    {
                       
                        case MessageType.Signup:

                            id = newHeader.UserInfo.GetPureId();
                            pw = newHeader.UserInfo.GetPurePwd();
                            dummyFlag = newHeader.UserInfo.IsDummy;
                            result =  sqlCon.CreateUser(id, pw, dummyFlag);
                           
                            resultHeader = new CommonHeader(result.type, result.state, 0, new Cookie(-1), new UserInfo(result.userId, String.Empty, false));
                            break;

                        case MessageType.Signin:

                            id = newHeader.UserInfo.GetPureId();
                            pw = newHeader.UserInfo.GetPurePwd();
                            dummyFlag = newHeader.UserInfo.IsDummy;

                            result =  sqlCon.ConfirmSignIn(id, pw);
                            cookie = redCon.CreateUserCookie(id);
                           
                            resultHeader = new CommonHeader(result.type, result.state, 0, cookie, new UserInfo(result.userId, String.Empty, false));
                            break;

                        case MessageType.Logout:

                            id = newHeader.UserInfo.GetPureId();
                            pw = newHeader.UserInfo.GetPurePwd();
                            dummyFlag = newHeader.UserInfo.IsDummy;

                            result = sqlCon.LogOut(id, pw);
                            redCon.DeleteUserCookie(id);
                           
                            resultHeader = new CommonHeader(result.type, result.state, 0, new Cookie(-1), new UserInfo(result.userId, String.Empty, false));
                            break;

                        case MessageType.Delete:

                            id = newHeader.UserInfo.GetPureId();
                            pw = newHeader.UserInfo.GetPurePwd();
                            dummyFlag = newHeader.UserInfo.IsDummy;

                            result =  sqlCon.DeleteUser(id, pw, dummyFlag);
                            redCon.DeleteUserCookie(id);
                          
                            resultHeader =  new CommonHeader(result.type, result.state, 0, new Cookie(-1), new UserInfo(result.userId, String.Empty, false));
                            break;

                        case MessageType.Modify:

                            bodySize = newHeader.BodyLength;
                            modUserInfo = RcvBodyProcess(sock, bodySize);

                            id = newHeader.UserInfo.GetPureId();
                            pw = newHeader.UserInfo.GetPurePwd();
                            newPw = new String(modUserInfo.Npwd);

                            result =  sqlCon.ModifyUser(id, pw, newPw);
                            resultHeader = new CommonHeader(result.type, result.state, 0, new Cookie(-1), new UserInfo(result.userId, String.Empty, false));
                            break;       
                    }

                    //header send
                    NetworkManager.Send(sock, resultHeader);
                    Console.WriteLine("[MM][Send][3xx]");

                    break;

                case 4:     //SendAsync connection 수정해야됨
                    id = newHeader.UserInfo.GetPureId();
                    redCon.UpdateUserCookie(id);

                    switch (newHeader.Type)
                    {
                        case MessageType.CreateRoom :
                            id = newHeader.UserInfo.GetPureId();
                            result =  redCon.CreateRoom(id);
                            resultHeader = new CommonHeader(result.type, result.state, 0, new Cookie(-1), new UserInfo(result.userId, String.Empty, false));
                            newRoomInfo = new CBCreateRoomResponseBody(result.room);
                            NetworkManager.Send(sock, resultHeader, newRoomInfo);

                            break;

                        case MessageType.ListRoom:
                            id = newHeader.UserInfo.GetPureId();
                            result = redCon.GetAllRoomInfo(id);
                            resultHeader = new CommonHeader(result.type, result.state, 0, new Cookie(-1), new UserInfo(result.userId, String.Empty, false));
                            roomListInfo = new CBListRoomResponseBody(result.roomList);
                            NetworkManager.Send(sock, resultHeader, roomListInfo);

                            break;

                        case MessageType.JoinRoom:

                            id = newHeader.UserInfo.GetPureId();
                            bodySize = newHeader.BodyLength;
                            joinRoomInfo =  RcvJoinBodyProcess(sock, bodySize);
                            roomNumber = joinRoomInfo.RoomInfo.RoomNo;
                            result =  redCon.JoinRoom(id, roomNumber);
                            resultHeader = new CommonHeader(result.type, result.state, 0, new Cookie(-1), new UserInfo(result.userId, String.Empty, false));
                           NetworkManager.Send(sock, resultHeader);

                            break;

                        case MessageType.LeaveRoom:

                            id = newHeader.UserInfo.GetPureId();
                            bodySize = newHeader.BodyLength;
                            leaveRoomInfo = RcvLeaveBodyProcess(sock, bodySize);
                            roomNumber = leaveRoomInfo.RoomInfo.RoomNo;
                            result = redCon.LeaveRoom(id, roomNumber);
                            resultHeader = new CommonHeader(result.type, result.state, 0, new Cookie(-1), new UserInfo(result.userId, String.Empty, false));
                            NetworkManager.Send(sock, resultHeader);

                            break;
                    }
                    Console.WriteLine("[MM][Send][4xx]");

                    break;
                    

                default : 

                    switch (newHeader.Type)
                    {
                        case MessageType.BENotice:

                            switch (newHeader.State)
                            {
                                case MessageState.Fail:

                                    Console.WriteLine("[MM][Recieved][Login][Setup]");
                                    break;

                                case MessageState.Success:

                                    Console.WriteLine("[MM][Recieved][Chat][Setup]");
                                    break;
                            }
                            break;
                        
                        default :
                            Console.WriteLine("[MM][Recieved][Nothing]");
                            break;
                    }

                break;
               
            }
        }

        //SEND DATA TO MYSQL
        public async void DataProcessAsync(FEServer fe)
        {
            Socket feSock;

            feSock = fe.socket;
           //create task

            if (feSock!= null&&fe.wait){
                fe.wait = false;
                await RcvHeaderProcess(feSock);
            }

            fe.wait = true;   
        }
    }
}
