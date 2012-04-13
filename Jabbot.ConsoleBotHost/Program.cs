using System;
using System.Configuration;
using System.Linq;

namespace Jabbot.ConsoleBotHost
{
    class Program
    {
        private static readonly string _serverUrl = ConfigurationManager.AppSettings["Bot.Server"];
        private static readonly string _botName = ConfigurationManager.AppSettings["Bot.Name"];
        private static readonly string _botPassword = ConfigurationManager.AppSettings["Bot.Password"];
        private static readonly string _botRooms = ConfigurationManager.AppSettings["Bot.RoomList"];
        private static bool _appShouldExit = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Jabbot Bot Runner Starting...");

            Bot bot = new Bot("frankenbot", "123456", "http://jabbr-bots.apphb.com");
            bot.StartUp();
            Console.ReadLine();
            return;
            while (!_appShouldExit)
            {
                RunBot();
            }

        }

        private static void RunBot()
        {
            return;
            try
            {
                Console.WriteLine(String.Format("Connecting to {0}...", _serverUrl));
                Bot bot = new Bot(_serverUrl, _botName, _botPassword);
                bot.StartUp();
                JoinRooms(bot);
                Console.Write("Press enter to quit...");
                Console.ReadLine();
                bot.ShutDown();
                _appShouldExit = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.GetBaseException().Message);
            }

        }
        private static void JoinRooms(Bot bot)
        {
            foreach (var room in _botRooms.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()))
            {
                Console.Write("Joining {0}...", room);
                if (TryCreateRoomIfNotExists(room, bot))
                {
                    bot.JoinRoom(room);
                    Console.WriteLine("OK");
                }
                else
                {
                    Console.WriteLine("Failed");
                }
            }
        }
        private static bool TryCreateRoomIfNotExists(string roomName, Bot bot)
        {
            try
            {
                bot.CreateRoom(roomName);
            }
            catch (AggregateException e)
            {
                if (!e.GetBaseException().Message.Equals(string.Format("The room '{0}' already exists", roomName),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
