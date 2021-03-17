namespace MSDNUrlPatch
{
    class Program
    {
        static void Main(string[] args)
        {
            var opt = new CommandLineOptions();
            if (opt.Parse(args))
            {
                new UrlRepairHelper(opt).Start();
            }
        }
    }
}
