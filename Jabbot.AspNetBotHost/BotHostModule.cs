using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using Nancy;
using System.Diagnostics;
using MomentApp;
using System.Threading.Tasks;

namespace Jabbot.AspNetBotHost
{
    public class BotHostModule : NancyModule
    {
        private static readonly string _hostBaseUrl = ConfigurationManager.AppSettings["Application.HostBaseUrl"];
        private static readonly string _serverUrl = ConfigurationManager.AppSettings["Bot.Server"];
        private static readonly string _botName = ConfigurationManager.AppSettings["Bot.Name"];
        private static readonly string _botPassword = ConfigurationManager.AppSettings["Bot.Password"];
        private static readonly string _botRooms = ConfigurationManager.AppSettings["Bot.RoomList"];
        private static readonly string _momentApiKey = ConfigurationManager.AppSettings["Moment.ApiKey"];
        private static Bot _bot;

        public BotHostModule()
        {
            StartBot();
            Get["/"] = _ =>
            {
                return "Jabbot AspNet Nancy Bot Runner";
            };
            Get["/bot/start"] = _ =>
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

            Get["/bot/stop"] = _ =>
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

            // This is for ensuring that the process doesn't die permanently -- 
            // We create a task with MomentApp (TBD whether we will use this permanently
            Get["/keepalive"] = _ =>
            {
                ScheduleKeepAlive(_hostBaseUrl + "/keepalive");
                return "OK";
            };
        }



        private static void ScheduleKeepAlive(string Url)
        {
            return;
            new Moment(_momentApiKey).ScheduleJob(new Job()
            {   
                at = DateTime.Now.AddMinutes(5),
                method = "GET",
                uri = new Uri(Url)
            });
        }

        private static void StartBot()
        {
            if (!_hostBaseUrl.Contains("localhost"))
            {
                if (!Uri.IsWellFormedUriString(_hostBaseUrl, UriKind.Absolute))
                    throw new InvalidOperationException("The Application.HostBaseUrl is not well formed.  Check the configuration settings.");
                Task.Factory.StartNew(() =>
                {
                    ScheduleKeepAlive(_hostBaseUrl + "/keepalive");
                });

            }
            if (_bot != null)
            {
                _bot.ShutDown();
            }
            _bot = new Bot(_serverUrl, _botName, _botPassword);
            _bot.PowerUp();
            
            JoinRooms(_bot);

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
                    bot.Join(room);
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