namespace WebChemistry.Queries.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 3)
            {
                if (args[0].Equals("--wiki-ref", StringComparison.Ordinal))
                {
                    QueriesWikiReference.Create(args[1], args[2]);
                    Console.WriteLine("Wiki ref created.");
                    return;
                }
            }

            //args = new string[]
            //{
            //    @"I:\test\Queries\StandaloneTest\result",
            //    @"I:\test\Queries\StandaloneTest\settings.json"
            //};

            QueriesService.Run(args);
        }
    }
}
