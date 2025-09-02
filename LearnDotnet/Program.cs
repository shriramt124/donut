using System;
using ArrayLearn;
using LoopsLearn;

namespace HelloWorld
{
    class Program
    {
        public void Loops()
        {
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine("ram");
            }

            //for each loop also
            string[] fruits = { "apple", "banana", "cherry", "date" };
            foreach (string fruit in fruits)
            {
                Console.WriteLine(fruit);
            }
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            // p.Loops();
            Loops lp = new Loops();
            lp.printEvenOodd();
            lp.add(2, 4);
            lp.SayHello("shriram");
            Console.WriteLine("Nth fibonacci is : " + lp.fib(4));

            Console.WriteLine("Enter the value of the m and n to demonstrate the pass bt refrence");
            int m = int.Parse(Console.ReadLine());
            int n = int.Parse(Console.ReadLine());
            //the m and n value that you are passing must be initialized just before passing as it to the ref

            lp.changeValue(ref m, ref n);
            Console.WriteLine("After swapping values : ");
            Console.WriteLine("a is : " + m + " b is : " + n);

            int x,
                y;
            lp.exampleofOut(out x, out y); //promise that the value will be assigned in this method in the out
            Console.WriteLine("x is : " + x + " y is : " + y);

            Arrays ar = new Arrays();
            ar.Arrbasics();
        }
    }
}
