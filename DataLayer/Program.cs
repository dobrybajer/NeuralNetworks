namespace DataLayer
{
    class Program
    {
        private const string Path1 = "..\\..\\..\\Data\\b20130402.l";
        private const string Path2 = "..\\..\\..\\Data\\d20130403.l";

        static void Main(string[] args)
        {
            var parser = new Parser(Path1);
            var model = parser.ReadFile();

        }
    }
}
