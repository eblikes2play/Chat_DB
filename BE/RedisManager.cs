using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using StackExchange.Redis;
using MySQLManager;


namespace RedisManager
{

class RedisController
    {
        private static readonly string redisServerEndPoint = "192.168.56.107:6379";
        private static readonly string KeyForRoomInfo = "Room:";
        private static readonly string KeyForCookie = ":Cookie:";
        private static readonly string KeyForRank = "Rank:";
        public Queue<int> unoccupiedRoomNum;
        private Random rand = new Random();
        private TimeSpan cookieExpiry;
        ConnectionMultiplexer redis;
        private static int maxRoomNumber;

        IDatabase db;

        public RedisController()
        {
            redis = ConnectionMultiplexer.Connect(redisServerEndPoint);
            Console.WriteLine("[Redis][Connect][Success]");

            db = redis.GetDatabase();

            maxRoomNumber = 50;
            //Cookie expires after 5min.
            cookieExpiry = new TimeSpan(0, 0, 300);
            unoccupiedRoomNum = new Queue<int>();


            //Total number of rooms : 50
            for(int i =1; i<=maxRoomNumber; i++)
            {
                unoccupiedRoomNum.Enqueue(i);
            }
        }

        private int UseRoomNumber()
        {
            try
            {
                int vaildRoomNumber = unoccupiedRoomNum.Peek();
                unoccupiedRoomNum.Dequeue();
                return vaildRoomNumber;
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }        
        }

        private void ReturnRoomNumber(int number)
        {
            unoccupiedRoomNum.Enqueue(number);
        }

        public QueryResult CreateRoom(string userId)
        {
            int number = UseRoomNumber();

            if(number != -1)
            {
                string roomNum = UseRoomNumber().ToString();
                bool result =  db.HashSet(KeyForRoomInfo, roomNum, 0);
                Console.WriteLine("[Redis][CreateRoom][Success]");
                return new QueryResult(MessageType.CreateRoom, MessageState.Success, userId, new ChattingRoom(number), new ChattingRoom[0]);
            }
            else
            {
                Console.WriteLine("[Redis][CreateRoom][Fail] : NO available room Number");
                return new QueryResult(MessageType.CreateRoom, MessageState.Fail, userId, new ChattingRoom(0), new ChattingRoom[0]);
            }
        }

        public QueryResult JoinRoom(string userId,int roomNumber)
        {
            string roomNum = roomNumber.ToString();
            bool searchResult = db.HashExists(KeyForRoomInfo, roomNumber.ToString());
            
            if (searchResult)
            {
                string preUserNum = db.HashGet(KeyForRoomInfo, roomNum);
                preUserNum = (Int32.Parse(preUserNum) + 1).ToString();
                bool result =db.HashSet(KeyForRoomInfo, roomNum, preUserNum);

                if (result)
                {
                    Console.WriteLine("[Redis][JoinRoom][Sucess]");
                    return new QueryResult(MessageType.JoinRoom, MessageState.Success, userId, new ChattingRoom(roomNumber), new ChattingRoom[0]);
                }
                else
                {
                    Console.WriteLine("[Redis][JoinRoom][Fail]");
                    return new QueryResult(MessageType.JoinRoom, MessageState.Fail, userId, new ChattingRoom(roomNumber), new ChattingRoom[0]);
                }
            }
            else
            {
                Console.WriteLine("[Redis][JoinRoom][Fail] : Wrong room number");
                return new QueryResult(MessageType.JoinRoom, MessageState.Fail, userId, new ChattingRoom(roomNumber), new ChattingRoom[0]);
            }
        }


        public QueryResult LeaveRoom(string userId, int roomNumber)
        {
            string roomNum = roomNumber.ToString();
            bool searchResult = db.HashExists(KeyForRoomInfo, roomNumber.ToString());

            if (searchResult)
            {
                string preUserNum = db.HashGet(KeyForRoomInfo, roomNum);
                preUserNum = (Int32.Parse(preUserNum) - 1).ToString();
                if (preUserNum.Equals("0"))
                {
                    DeleteRoom(roomNumber);
                    Console.WriteLine("[Redis][LeaveRoom][Sucess] : Destroy Room");
                    return  new QueryResult(MessageType.LeaveRoom, MessageState.Success, userId, new ChattingRoom(roomNumber), new ChattingRoom[0]);
                }

                bool result = db.HashSet(KeyForRoomInfo, roomNum, preUserNum);

                if (result)
                {
                    Console.WriteLine("[Redis][LeaveRoom][Sucess]");
                    return new QueryResult(MessageType.LeaveRoom, MessageState.Success, userId, new ChattingRoom(roomNumber), new ChattingRoom[0]);
                }
                else
                {
                    Console.WriteLine("[Redis][LeaveRoom][Fail]");
                    return new QueryResult(MessageType.LeaveRoom, MessageState.Fail, userId, new ChattingRoom(roomNumber), new ChattingRoom[0]);
                }
            }
            else
            {
                Console.WriteLine("[Redis][LeaveRoom][Fail]");
                return new QueryResult(MessageType.LeaveRoom, MessageState.Fail, userId, new ChattingRoom(roomNumber), new ChattingRoom[0]);
            }
        }

         
        //안쓸듯
        public bool DeleteRoom(int roomNumber)
        {
            string roomNum = roomNumber.ToString(); 
            bool result =db.HashDelete(KeyForRoomInfo, roomNum);
            ReturnRoomNumber(roomNumber);
            Console.WriteLine("[Redis][DeleteRoom][Sucess]");
            return result;
        }

        public QueryResult GetAllRoomInfo(string userId)
        {
            HashEntry[] list = db.HashGetAll(KeyForRoomInfo);
            List<ChattingRoom> roomList = new List<ChattingRoom>();
            ChattingRoom[] roomArray;
           
            foreach(HashEntry value in list)
            {
                ChattingRoom room = new ChattingRoom((int)value.Name);
                roomList.Add(room);
            }

            if (roomList.Count != 0)
            {
                roomArray = roomList.ToArray();
                Console.WriteLine("[Redis][ListRoom][Sucess]");
                return new QueryResult(MessageType.ListRoom, MessageState.Success, userId, new ChattingRoom(0), roomArray);
            }
            else
            {
                Console.WriteLine("[Redis][ListRoom][Fail]");
                return new QueryResult(MessageType.ListRoom, MessageState.Fail, userId, new ChattingRoom(0), new ChattingRoom[0]);
            }        
        }


        public Cookie CreateUserCookie(string userName)
        {
            StringBuilder commandBuilder;
            commandBuilder = new StringBuilder();
            commandBuilder.Append(userName);
            commandBuilder.Append(KeyForCookie);


            string userKey = commandBuilder.ToString();
            string userCookie = rand.Next(1, 10000).ToString();
            
            bool result = db.StringSet(userKey, userCookie, cookieExpiry);

            Console.WriteLine("[Redis][Cookie][Create][Sucess]");
            return new Cookie(Convert.ToInt32(userCookie));  
        }

        //blind to user
        public void UpdateUserCookie(string userName)
        {
            StringBuilder commandBuilder;
            commandBuilder = new StringBuilder();
            commandBuilder.Append(userName);
            commandBuilder.Append(KeyForCookie);

            string userKey = commandBuilder.ToString();
            bool result = db.KeyExpire(userKey, cookieExpiry);
            Console.WriteLine("[Redis][Cookie][Update][Sucess]");
        }

        //blind to user
        public void DeleteUserCookie(string userName)
        {
            StringBuilder commandBuilder;
            commandBuilder = new StringBuilder();
            commandBuilder.Append(userName);
            commandBuilder.Append(KeyForCookie);

            string userKey = commandBuilder.ToString();
            bool result = db.KeyDelete(userKey);
            Console.WriteLine("[Redis][Cookie][Delete][Sucess]");
        }

        //update score when user send message
        public void UpdateRank(string userName)
        {
            db.SortedSetIncrement(KeyForRank, userName, 1.0);
            Console.WriteLine("[Redis][Rank][Update][Sucess]");
        }


        //get rank
        public void GetRankAndScore()
        {

            SortedSetEntry[] rank = db.SortedSetRangeByRankWithScores(KeyForRank, 0, 9, Order.Descending);
            //rank struct
        }      
    }
}
