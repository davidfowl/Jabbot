using System.ComponentModel.Composition;
using Jabbot.Core;
﻿
namespace Jabbot.Sprockets.Core
{
    [InheritedExport]
    public interface ISprocketInitializer
    {
        void Initialize(IBot bot);
    }
}
