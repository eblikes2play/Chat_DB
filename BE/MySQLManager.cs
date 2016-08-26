using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;



namespace MySQLManager
{


    /// <summary>
    /// When got result of query.
    /// response to MessageManager, return QueryResult struct
    /// </summary>
    public struct QueryResult{
        public MessageType type;
        public MessageState state;
        public string userId;
        public ChattingRoom room;
        public ChattingRoom[] roomList;

        public QueryResult(MessageType type, MessageState state, string id, ChattingRoom room, ChattingRoom[] list)
        {
            this.type = type;
            this.state = state;
            userId = id;
            this.room = room;
            roomList = list;
        }
    }
 
    public class MySQLController
    {
        MySQLConnectionHandler connHandler;
        MySqlConnection connection;
        string beServerIP;
        string userID;
        string userPW;
        string db;

        public MySQLController()
        {
            beServerIP = "192.168.56.102";
            userID = "admin";
            userPW = "";
            db = "UserDB";
            connHandler = new MySQLConnectionHandler(beServerIP, userID, userPW, db);
            connection = connHandler.Connect();
        }
        /// <summary>
        /// create connection with MySQL
        /// </summary>
       


        /// <summary>
        /// SEND QUERY TO MySQL database
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pw"></param>
        /// <param name="dummyflag"></param>
        /// 
        //if username is unique, create userinfo
        //if not, return false 
        public QueryResult CreateUser(string id, string pw, bool dummyflag)
        {
            #region SignUp

            MySqlCommand cmd;
            MySqlDataReader rdr;
            StringBuilder commandBuilder;
            string findUserQuery;
            string addQuery;

            commandBuilder = new StringBuilder();
            commandBuilder.AppendFormat("SELECT * FROM UserInfo WHERE id = '{0}'", id);
            findUserQuery = commandBuilder.ToString();

            cmd = new MySqlCommand(findUserQuery, connection);

            rdr = cmd.ExecuteReader();

            if (!rdr.HasRows)
            {
                rdr.Close();
                commandBuilder.Clear();

                commandBuilder.AppendFormat("INSERT INTO UserInfo VALUES('{0}', '{1}', {2})", id, pw, dummyflag);
                addQuery = commandBuilder.ToString();
                cmd = new MySqlCommand(addQuery, connection);
            
                cmd.ExecuteNonQuery();
                Console.WriteLine("[MySQL][Create][Success]");
                return new QueryResult(MessageType.Signup, MessageState.Success, id, new ChattingRoom(0), new ChattingRoom[0]);
            }
            else
            {
                Console.WriteLine("Same user name is already in database");
                rdr.Close();
                Console.WriteLine("[MySQL][Create][Fail]");
                return new QueryResult(MessageType.Signup, MessageState.Fail, id, new ChattingRoom(0), new ChattingRoom[0]);
            }

            #endregion
        }

        //if userinfo is in database, delete userinfo
        //if not, return false
        public QueryResult DeleteUser(string id, string pw, bool dummyflag)
        {
            #region Delete

            MySqlCommand cmd;
            StringBuilder commandBuilder;
            string deleteQuery;
            int result;

            commandBuilder = new StringBuilder();
            commandBuilder.AppendFormat("DELETE FROM UserInfo WHERE id = '{0}' AND pw ='{1}'", id, pw);
            deleteQuery = commandBuilder.ToString();          
            cmd = new MySqlCommand(deleteQuery, connection);
          
            result = cmd.ExecuteNonQuery();

            if (result == 0) {
                Console.WriteLine("[MySQL][Delete][Fail] Cannot find vaild user info in database");
                return new QueryResult(MessageType.Delete, MessageState.Fail, id, new ChattingRoom(0), new ChattingRoom[0]);            //fail
            }
            else if(result == 1)
            {
                Console.WriteLine("[MySQL][Delete][Success]");
                return new QueryResult(MessageType.Delete, MessageState.Success, id, new ChattingRoom(0), new ChattingRoom[0]);           //success
            }
            else
            {
                Console.WriteLine("[MySQL][Delete][Error] Matched Several User Info in database");
                return new QueryResult(MessageType.Delete, MessageState.Error, id, new ChattingRoom(0), new ChattingRoom[0]);          //error
            }
           
            #endregion
        }

        //if specific user name and password match up with in database, modify user password
        //else return false
        public QueryResult ModifyUser(string id, string pw, string newPw)
        {
            #region Modify

            MySqlDataReader rdr;
            MySqlCommand cmd;
            StringBuilder commandBuilder;
            string findUserQuery;
            string modifyQuery;

            commandBuilder = new StringBuilder();
            commandBuilder.AppendFormat("SELECT id FROM UserInfo WHERE id = '{0}' AND pw = '{1}'", id, pw);
            findUserQuery = commandBuilder.ToString();

            cmd = new MySqlCommand(findUserQuery, connection);

            rdr = cmd.ExecuteReader();

            if (rdr.HasRows)
            {
                rdr.Close();
                commandBuilder.Clear();

                commandBuilder.AppendFormat("UPDATE UserInfo SET pw = '{0}' WHERE id = '{1}'", newPw, id);
                modifyQuery = commandBuilder.ToString();

                cmd = new MySqlCommand(modifyQuery, connection);

                cmd.ExecuteNonQuery();

                Console.WriteLine("[MySQL][Modify][Success]");
                return new QueryResult(MessageType.Modify, MessageState.Success, id, new ChattingRoom(0), new ChattingRoom[0]);
            }
            else
            {
                rdr.Close();
                Console.WriteLine("[MySQL][Modify][Fail]");
                return new QueryResult(MessageType.Modify, MessageState.Fail, id, new ChattingRoom(0), new ChattingRoom[0]);
            }

            #endregion
        }

        public QueryResult LogOut(string id, string pw)
        {
            Console.WriteLine("[MySQL][LogOut][Success]");
            return new QueryResult(MessageType.Logout, MessageState.Success, id, new ChattingRoom(0), new ChattingRoom[0]);
        }


        /// <summary>
        /// check id first,
        /// if id confirmed, check pw
        /// pw is right, signin OK
        /// else Fail code : -1
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pw"></param>
        /// <returns></returns>
        public QueryResult ConfirmSignIn(string id, string pw)
        {
            #region SignIn

            MySqlDataReader rdr;
           
            MySqlCommand cmd;
            StringBuilder commandBuilder;
            string findUserQuery;
            string confirmQuery;

            commandBuilder = new StringBuilder();
            commandBuilder.AppendFormat("SELECT id FROM UserInfo WHERE id = '{0}'", id);
            findUserQuery = commandBuilder.ToString();

            cmd = new MySqlCommand(findUserQuery, connection);

            rdr = cmd.ExecuteReader();

            if (rdr.HasRows)
            {
                rdr.Close();
                commandBuilder.Clear();
                commandBuilder.AppendFormat("SELECT id FROM UserInfo WHERE pw = '{0}'", pw);
                confirmQuery = commandBuilder.ToString();

                cmd = new MySqlCommand(confirmQuery, connection);

                string result = cmd.ExecuteScalar().ToString();  //...not sure

                if (id == result)
                {
                   
                    Console.WriteLine("[MySQL][SignIn][Sucess]");
                    
                    return new QueryResult(MessageType.Signin, MessageState.Success, id, new ChattingRoom(0), new ChattingRoom[0]);                           //Success
                }
                else
                {
                    Console.WriteLine("[MySQL][SignIn][Fail]");
                    
                    return new QueryResult(MessageType.Signin, MessageState.Fail, id, new ChattingRoom(0), new ChattingRoom[0]);                          //Fail
                }
            }
            else
            {
                rdr.Close();
                Console.WriteLine("[MySQL][SignIn][Error]"); 
                return new QueryResult(MessageType.Signin, MessageState.Error, id, new ChattingRoom(0), new ChattingRoom[0]);                              //Error : no user info
            }

            
            #endregion
        }


        public void DeleteAllDummies()
        {
            MySqlCommand cmd;
            string deleteDummyQuery;
            deleteDummyQuery = "DELETE FROM UserInfo WHERE dummyflag = 1";
            cmd = new MySqlCommand(deleteDummyQuery, connection);

            cmd.ExecuteNonQuery();
            Console.WriteLine("[MySQL][DeleteDummy][Success]");
        }

        /// <summary>
        /// MySQLConnectionHandler established MySQL connection
        /// </summary>

        private class MySQLConnectionHandler
        {
            #region MySQLConnection
            string strConn;
            string serverIP;
            string userID;
            string userPW;
            string db;
            StringBuilder connInfo;

            MySqlConnection conn = null;

            public MySQLConnectionHandler(string serverIP, string userID, string userPW, string db)
            {
                this.serverIP = serverIP;
                this.userID = userID;
                this.userPW = userPW;
                this.db = db;
                connInfo = new StringBuilder();

                string[] DBInfo = { this.serverIP, this.userID, this.userPW, this.db };
                connInfo.AppendFormat("Server={0};Uid={1};Pwd={2};Database={3};", DBInfo);
                
                strConn = connInfo.ToString();
                Console.WriteLine(strConn);
            }


            public MySqlConnection Connect()
            {
                while (true)
                {
                    try
                    {
                        conn = new MySqlConnection(strConn);
                        conn.Open();

                        Console.WriteLine("[MySQL][Connect][Success]");
                        return conn;
                    }
                    catch (MySqlException ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("[MySQL][Connect][Error]");
                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        conn.Close();
                        Console.WriteLine("[MySQL][Connect][Error]:"+e.Message);
                    }
                }
            }

            public void Disconnect()
            {
                conn.Close();
                Console.WriteLine("[MySQL][Disconnect][Success]");
            }
            #endregion 
        }

    }

}
