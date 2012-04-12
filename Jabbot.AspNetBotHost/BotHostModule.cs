using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using Nancy;
using System.Diagnostics;
using MomentApp;
using System.Threading.Tasks;
using JabbR.Client.Models;

namespace Jabbot.AspNetBotHost
{
    public class BotHostModule : NancyModule
    {
        private static readonly string _serverUrl = ConfigurationManager.AppSettings["Bot.Server"];
        private static readonly string _botName = ConfigurationManager.AppSettings["Bot.Name"];
        private static readonly string _botPassword = ConfigurationManager.AppSettings["Bot.Password"];
        private static readonly string _botRooms = ConfigurationManager.AppSettings["Bot.RoomList"];
        private static readonly string _startMode = ConfigurationManager.AppSettings["Bot.StartMode"];
        private static Bot _bot;

        public BotHostModule()
            : base("bot")
        {
            SetupRoutes();
            AutoStartBotIfRequired();
        }

        private static void AutoStartBotIfRequired()
        {
            if (_startMode.Equals("auto", StringComparison.OrdinalIgnoreCase) && _bot == null)
            {
                StartBot();
            }
        }

        private void SetupRoutes()
        {
            Get["/start"] = _ =>
            {
                try
                {
                    StartBot();
                    return "Bot Started";
                }
                catch (Exception e)
                {
                    return e.GetBaseException().ToString();
                }
            };

            Get["/stop"] = _ =>
            {
                try
                {
                    ShutDownBot();
                    return "Bot Shut Down";
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            };
        }

        private static void StartBot()
        {

            KeepAliveModule.ScheduleKeepAlive();
            if (_bot != null)
            {
                TryShutBotDown();
            }
            _bot = new Bot(_serverUrl, _botName, _botPassword);
            _bot.StartUp();
            JoinRooms(_bot);

        }

        private static void TryShutBotDown()
        {
            try
            {
                _bot.ShutDown();
            }
            catch
            { }
        }


        private static void ShutDownBot()
        {
            _bot.ShutDown();
            _bot = null;
        }

        private static void JoinRooms(Bot bot)
        {
            foreach (var room in _botRooms.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()))
            {
                Trace.Write("Joining {0}...", room);
                if (TryCreateRoomIfNotExists(room, bot))
                {
                    bot.JoinRoom(room);
                    Trace.WriteLine("OK");
                }
                else
                {
                    Trace.WriteLine("Failed");
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