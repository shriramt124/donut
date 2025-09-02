using System;

namespace ArrayLearn
{
    class Arrays
    {
        public void Arrbasics()
        {
            int[] numbers = new int[5];
            int[] scores = new int[6];
            int[] marks = { 52, 14, 88, 76 };
            int firstElement = scores[0];
            int secondElement = scores[1];
            scores[2] = 80;
            Console.WriteLine("Length of the array is : " + numbers.Length);
            //we can modfiy the elements of the array
            scores[2] = 80;
            foreach (int ele in marks)
            {
                Console.WriteLine(ele);
            }

            Console.WriteLine("Enter the numbers of the array ");

            for (int i = 0; i < numbers.Length; i++)
            {
                int n = int.Parse(Console.ReadLine());
                numbers[i] = n;
            }

            for (int i = 0; i < numbers.Length; i++)
            {
                Console.WriteLine($"number[{i}] is: {numbers[i]}");
            }

            //cmmon array methods
            Array.Sort(numbers);
            foreach (int ele in numbers)
            {
                Console.Write(ele + " ");
            }
            Console.WriteLine("\n");

            //reversing the array
            Array.Reverse(numbers);
            foreach (int ele in numbers)
            {
                Console.Write(ele + " ");
            }
            Console.WriteLine("\n");

            //finding hte index
            int index = Array.IndexOf(numbers, 5);
            //it will return -1 if the index will not be present
            Console.Write("index of the numbers is :" + index);

            Array.Resize(ref numbres, 7); //it will resize to 7 and the new columns that are added will assigned to 0
        }
    }
}
