using Jabbot.Models;
using Jabbot.Core;

namespace Jabbot.Sprockets.Core
{
    public interface IMessageHandler
    {
        /// <summary>
        /// Allows the user to handle a message incoming from the bot's room
        /// </summary>
        /// <param name="message">Incoming message</param>
        /// <param name="bot">bot instance</param>
        /// <returns>true if handled, false if not</returns>
        bool Handle(ChatMessage message, IBot bot);
    }
}
