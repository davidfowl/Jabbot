using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
﻿using System.Linq;
using System.Text;

namespace Jabbot.Sprockets
{
    [InheritedExport]
    public interface ISprocketInitializer
    {
        void Initialize(Bot bot);
    }
}
