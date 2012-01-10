using System.ComponentModel.Composition;
﻿
namespace Jabbot.Sprockets
{
    [InheritedExport]
    public interface ISprocketInitializer
    {
        void Initialize(Bot bot);
    }
}
