using System;
using System.IO;
using System.Reflection;

namespace T3D
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var programName = Assembly.GetEntryAssembly()?.GetName().Name;

            Console.WriteLine("=================");
            Console.WriteLine($"{programName} {Assembly.GetEntryAssembly()?.GetName().Version}");
            Console.WriteLine("=================");

            if (args.Length < 2)
            {
                Console.WriteLine($"Usage: {programName} <archive.t3d> <output folder>");

                return -1;
            }

            Directory.CreateDirectory(args[1]);

            try
            {
                using (var t3d = T3DArchive.OpenRead(args[0]))
                {
                    foreach (var entry in t3d.Entries)
                    {
                        Console.Write($"Extracting {Path.Combine(args[1], entry.FileName)} ... ");
                        entry.ExtractToDirectory(args[1]);
                        Console.WriteLine("ok!");
                    }
                }

                return 0;
            }

            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: {ex.Message}");
            }

            return -1;
        }
    }
}
