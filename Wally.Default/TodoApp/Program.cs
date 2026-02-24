using System;
using System.Collections.Generic;

namespace TodoApp
{
    class Program
    {
        static List<string> todos = new List<string>();

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Todo App!");
            while (true)
            {
                Console.WriteLine("\nCommands: add <task>, list, remove <index>, exit");
                Console.Write("Enter command: ");
                string input = Console.ReadLine();
                if (input.StartsWith("add "))
                {
                    string task = input.Substring(4);
                    todos.Add(task);
                    Console.WriteLine("Task added.");
                }
                else if (input == "list")
                {
                    for (int i = 0; i < todos.Count; i++)
                    {
                        Console.WriteLine($"{i}: {todos[i]}");
                    }
                }
                else if (input.StartsWith("remove "))
                {
                    if (int.TryParse(input.Substring(7), out int index) && index >= 0 && index < todos.Count)
                    {
                        todos.RemoveAt(index);
                        Console.WriteLine("Task removed.");
                    }
                    else
                    {
                        Console.WriteLine("Invalid index.");
                    }
                }
                else if (input == "exit")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Unknown command.");
                }
            }
        }
    }
}