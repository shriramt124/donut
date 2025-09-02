using System;

namespace LoopsLearn
{
    class Loops
    {
        public void printEvenOodd()
        {
            Console.Write("Enter the number");
            int num = int.Parse(Console.ReadLine());
            if (num % 2 == 0)
            {
                Console.WriteLine("It is even number");
            }
            else
            {
                Console.WriteLine("It is odd number");
            }
        }

        public void add(int a, int b)
        {
            Console.WriteLine("sum of two numbers is : " + (a + b));
        }

        public void SayHello(string name)
        {
            Console.WriteLine($"hello {name} how are you");
        }

        public int fib(int n)
        {
            //pass by value
            //it returns the nth fibonnacci
            if (n == 0 || n == 1)
            {
                return n;
            }
            return fib(n - 1) + fib(n - 2);
        }

        //pass bt refrence parameter
        public void changeValue(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        public void exampleofOut(out int a, out int b)
        {
            //here out is just a promise that it will return some value and intitalize it in a and b
            a = 10;
            b = 23;
            /*
            out lets a method set values in caller variables.

Method must assign out variables before returning.

Caller must include out when calling.

Use tuples for clearer multiple returns in many cases, but out is common and perfectly valid.
            */
        }
    }
}
