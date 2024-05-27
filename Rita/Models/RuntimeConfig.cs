using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Nuke.Common.IO;
using Nuke.Common;
using System.IO;
using NuGet.Versioning;
using System.CodeDom;
using System.Security.Cryptography;
using JetBrains.Annotations;

namespace Cloud.Models
{

    public class RuntimeConfig
    {

        public Runtime Runtime { get; set; }
                     
            public RuntimeConfig()
            {
               Runtime = new Runtime("linux-x64");
            }

    }
}
