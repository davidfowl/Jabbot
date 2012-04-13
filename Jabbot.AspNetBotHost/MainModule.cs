using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;

namespace Jabbot.AspNetBotHost
{
    public class MainModule : NancyModule
    {
        public MainModule()
        {
            Get["/"] = _ =>
            {
                return View["index.html"];
            };
        }
    }
}