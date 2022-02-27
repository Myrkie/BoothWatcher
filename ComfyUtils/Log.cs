using System;

#pragma warning disable CS8603
#pragma warning disable CS8625
#pragma warning disable IDE0036
#pragma warning disable IDE0071

namespace ComfyUtils
{
    public class Log
    {
        public static void Msg(object txt = null, bool newline = true)
        {
            Console.ResetColor();
            if (txt != null)
            {
                Console.Write($"{txt.ToString()}{(newline ? "\n" : "")}");
            }
            else
            {
                Console.WriteLine();
            }

        }
        public static string Input(object txt = null, char suffix = ':')
        {
            Console.ResetColor();
            Console.Write($"{txt}{suffix} ");
            return Console.ReadLine();
        }
        public static ConsoleKeyInfo KeyInput(object txt = null, bool silent = true)
        {
            Console.ResetColor();
            Console.Write($"{txt}");
            ConsoleKeyInfo Response = Console.ReadKey(silent);
            Console.WriteLine();
            return Response;
        }
        public static void Pause() => Console.ReadKey(true);
        public static void Clear()
        {
            Console.Clear();
            Console.ResetColor();
        }
    }
}
