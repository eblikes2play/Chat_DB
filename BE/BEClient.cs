using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using LunkerLibrary;
using MySQLManager;
using FEConnectionManager;
using MessageManager;
using StackExchange.Redis;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using RedisManager;



/// <summary>
/// Main
/// </summary>

namespace BEClient
{
    class Program
    {
        static void Main(string[] args)
        {
            MessageController mm = new MessageController();
            Task task = mm.FirstSetting();

                try
                {
                    while (true)
                    {
                        if (task.IsCompleted && mm.connect)
                        {
                            mm.Start();
                        }
                    }
                }               
                catch(OutOfMemoryException m)
                {
                    Console.WriteLine("[BE][Main][Error]:" + m.Message);
                }     
        }
    }
}
