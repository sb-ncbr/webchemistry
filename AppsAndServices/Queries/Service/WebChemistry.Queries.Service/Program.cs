namespace WebChemistry.Queries.Service
{
    using System;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            // Handle --wiki-ref command
            if (args.Length >= 1 && args[0].Equals("--wiki-ref", StringComparison.Ordinal))
            {
                if (args.Length != 3)
                {
                    Console.WriteLine("Error: --wiki-ref requires exactly 2 arguments: <outputFolder> <formatVersion>");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  {0} --wiki-ref <outputFolder> <formatVersion>", AppDomain.CurrentDomain.FriendlyName);
                    Console.WriteLine();
                    Console.WriteLine("Arguments:");
                    Console.WriteLine("  outputFolder   - Directory where wiki reference will be created");
                    Console.WriteLine("  formatVersion  - Version string for the format (e.g., '1.0')");
                    Environment.Exit(1);
                    return;
                }

                var outputFolder = args[1];
                var formatVersion = args[2];

                try
                {
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }
                    QueriesWikiReference.Create(outputFolder, formatVersion);
                    Console.WriteLine("Wiki reference created successfully at: {0}", Path.GetFullPath(outputFolder));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error creating wiki reference: {0}", ex.Message);
                    Environment.Exit(1);
                }
                return;
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
