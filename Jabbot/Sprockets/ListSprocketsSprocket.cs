using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jabbot.Sprockets.Core;
using Jabbot.Core;

namespace Jabbot.Sprockets
{
    public class ListSprocketsSprocket : ISprocket
    {
        public bool Handle(Models.ChatMessage message, IBot bot)
        {
            if (message.Content.Equals("listsprockets", StringComparison.OrdinalIgnoreCase))
            {
                IList<ISprocket> sprockets = (bot as Bot).Sprockets;
                StringBuilder ret = new StringBuilder();
                ret.AppendFormat("Loaded Sprockets ({0}){1}",sprockets.Count,Environment.NewLine);
                foreach (var s in sprockets)
                {
                    ret.AppendFormat("{0}{1}", s.GetType().Name, Environment.NewLine);
                }
                bot.Send(ret.ToString(), message.Room);
                //TODO: Do something
                return true;
            }
            return false;
        }
    }
}
