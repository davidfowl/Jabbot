using System;
using System.Collections.Generic;
using System.Linq;

namespace Jabbot.CommandSprockets
{
    public abstract class RestrictedCommandSprocket : CommandSprocket
    {
        public virtual bool IsRestricted { get { return false; } }

        public abstract IEnumerable<string> AllowedUserList { get; }
        public abstract IEnumerable<string> BannedUserList { get; }

        protected virtual bool RequestorIsAllowed
        {
            get
            {
                return AllowedUserList.Any(u => u.Equals(Message.FromUser, StringComparison.OrdinalIgnoreCase));
            }
        }

        protected virtual bool RequestorIsBanned
        {
            get
            {
                return BannedUserList.Any(u => u.Equals(Message.FromUser, StringComparison.OrdinalIgnoreCase));
            }
        }
        public override bool MayHandle(string initiator, string command)
        {
            if (base.MayHandle(initiator, command))
            {
                EnsureRequestorIsAllowed();
                EnsureRequestorIsNotBanned();
                return true;
            }
            return false;
        }
        protected void EnsureRequestorIsNotBanned()
        {
            if (RequestorIsBanned)
            {
                throw new InvalidOperationException(String.Format(
                   "You are not allowed to execute the {0} command", Command));
            }

        }

        protected void EnsureRequestorIsAllowed()
        {
            if (!RequestorIsAllowed)
            {
                throw new InvalidOperationException(String.Format(
                    "You must be an administrator to execute the {0} command", Command));
            }
        }
    }
}
