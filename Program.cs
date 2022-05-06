using System;

namespace _2048
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new _2048())
                game.Run();
        }
    }
}
