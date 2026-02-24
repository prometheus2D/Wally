using System;

namespace WeatherApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Weather App!");
            Console.Write("Enter city: ");
            string city = Console.ReadLine();
            // Mock weather data
            string weather = "Sunny, 25°C";
            Console.WriteLine($"Weather in {city}: {weather}");
        }
    }
}