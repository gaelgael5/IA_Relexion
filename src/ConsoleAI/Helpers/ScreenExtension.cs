using System.CommandLine;
using System.CommandLine.IO;

namespace ConsoleAI.Helpers
{
    internal static class ScreenExtension
    {

        static ScreenExtension()
        {
            _console = new SystemConsole();
        }

        public static void WriteWhite(this string text)
        {
            _console.WriteLine($"\x1b[37m{text}\x1b[0m");
        }

        public static void WriteRed(this string text)
        {
            _console.WriteLine(@"\x1b[31m{text}\x1b[0m");
        }

        public static void WriteGreen(this string text)
        {
            _console.WriteLine($"\x1b[32m{text}\x1b[0m");
        }

        public static void WriteYellow(this string text)
        {
            _console.WriteLine($"\x1b[33m{text}\x1b[0m");
        }

        //public static void Test()
        //{
        //    // Affichage d'un tableau
        //    var table = new TableView<(string Nom, int Age)>();
        //    table.Items = new[] { ("Alice", 30), ("Bob", 25) };
        //    table.AddColumn(x => x.Nom, "Nom");
        //    table.AddColumn(x => x.Age, "Âge");
        //    var renderer = new ConsoleRenderer(_console, OutputMode.Ansi, resetAfterRender: true);
        //    table.Render(renderer, new Region(0, 0, 80, 10));
        //}

        private static readonly SystemConsole _console;

    }
}
