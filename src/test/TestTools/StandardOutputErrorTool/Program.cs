using System;
using System.Threading.Tasks;

namespace StandardOutputErrorTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var runs = args.Length <= 0 ? 100000 : int.Parse(args[0]);
            var text = "0123456789abcdefghijklmnopqrstuvwxyz";
            var output = "OUT: " + text;
            var err = "ERR: " + text;
            var t1 = Task.Run(() => {
                for(int i = 0; i < runs; i++){
                    Console.Error.Write(err + ", " + i);
                }
            });

            Task.WaitAll(t1);
            
            var t2 = Task.Run(() => {
                for(int i = 0; i < runs; i++){
                    Console.Write(output + ", " + i);
                }
            });

            Task.WaitAll(t1, t2);
        }
    }
}
