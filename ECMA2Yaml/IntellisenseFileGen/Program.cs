using ECMA2Yaml;
using IntellisenseFileGen.Models;
using Microsoft.OpenPublishing.FileAbstractLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntellisenseFileGen
{
    class Program
    {
        static void Main(string[] args)
        {
            IntellisenseFileGenHelper.Start(args);
        }
    }
}
