/*
    by korenkonder
    GitHub/GitLab: korenkonder
*/

using System;

namespace ARBConverter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Version ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                Console.WriteLine($"ARBConverter v{ver}");
                Console.WriteLine($"Usage: ARBConverter <file> [-i]");
                Console.WriteLine($"    -i          Include data from \"shared.glsl\" and \"sharedXXXX.glsl\"");
                return;
            }

            bool include = args.Length > 1;
            if (include) include = args[1] != "-i";
            else include = true;

            ARBConverter arb = new ARBConverter();
            arb = new ARBConverter();
            arb.Convert(args[0], include);
        }
    }
}
