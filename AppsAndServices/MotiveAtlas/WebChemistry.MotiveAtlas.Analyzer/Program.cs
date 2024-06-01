using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebChemistry.MotiveAtlas.Analyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            ////args = new string[]
            ////{
            ////    @"I:\test\MotiveAtlas\Test",
            ////    @"I:\test\MotiveAtlas\test_config.json"
            ////};

            AtlasAnalyzer.Run(args);
        }
    }
}
