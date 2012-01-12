using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using System.Configuration;
using System.Threading.Tasks;
using MomentApp;

namespace Jabbot.AspNetBotHost
{
    public class KeepAliveModule : NancyModule
    {
        private static readonly string _hostBaseUrl = ConfigurationManager.AppSettings["Application.HostBaseUrl"];
        private static Job _momentJob = null;
        private static object _momentLock = new object();
        private static readonly string _momentApiKey = ConfigurationManager.AppSettings["Moment.ApiKey"];
        const int MINUTES_BETWEEN_KEEPALIVES = 2;


        public KeepAliveModule()
            : base("/keepalive")
        {
            Get["/"] = _ =>
            {
                if (_momentJob != null)
                {
                    if ((DateTime.Now - _momentJob.at).TotalMilliseconds < 0)
                    {
                        throw new InvalidOperationException(String.Format("A keepalive is already scheduled for {0}", _momentJob.at));
                    }
                }
                _momentJob = null;
                ScheduleKeepAlive(_hostBaseUrl + "/keepalive");
                return "OK";
            };
        }

        public static void ScheduleKeepAlive()
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
        }


        private static void ScheduleKeepAlive(string Url)
        {
            lock (_momentLock)
            {
                if (_momentJob == null)
                {
                    _momentJob = new Moment(_momentApiKey).ScheduleJob(new Job()
                    {
                        at = DateTime.Now.AddMinutes(MINUTES_BETWEEN_KEEPALIVES),
                        method = "GET",
                        uri = new Uri(Url)
                    });
                }
            }
        }
    }
}