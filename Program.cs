using HULK;

namespace WatchRun
{
    class Program
    {
        static void Main()
        {
        Load Run = new(Console.WriteLine);
            while(true)
            {
            Console.Write(">");
            Run.RunProgram(Console.ReadLine());
            }
        }
    }

}

