
namespace CardsAndSymbols.ConsoleTest
{
    using ProjectivePlane;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Program
    {
        private const int NumCards = 55;
        private const int NumSymbols = 57;

        public static void Main(string[] args)
        {
            // Set up modern logging using the logging builder
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Warning); // Only show warnings and errors
            });
            var logger = loggerFactory.CreateLogger<Program>();
            
            try
            {

            var symbols = Symbol.CreateDefaultSymbolList(NumSymbols);
            var constructor = new ProjectivePlaneConstructor<Symbol>(symbols, NumCards, logger);
            var points = constructor.PlanePoints;

            List<Card<Symbol>>? cards = null;

            if (ProjectivePlaneConstructor<Symbol>.VerifyPointLines(points))
            {
                // Map points to cards and lines to symbols
                cards = points.Select(point => new Card<Symbol>(point.Lines)).ToList();
                Console.WriteLine("Cards ({0}):\n", cards.Count);

                foreach (var card in cards)
                {
                    Console.WriteLine("{0}", card);
                }
            }
            else
            {
                Console.WriteLine("Unable to distribute symbols in cards");
            }

            Console.WriteLine("\nPress any key to validate each card pair has exactly one symbol in common");
            Console.ReadKey();

            if (cards == null)
            {
                Console.WriteLine("No cards to validate");
                Environment.Exit(1);
            }

            foreach (var card in cards)
            {
                foreach (var otherCard in cards)
                {
                    if (card.Equals(otherCard))
                    {
                        continue;
                    }
                    var intersectionCount = 0;
                    foreach (var symbol in card.Symbols)
                    {
                        if (otherCard.Symbols.Contains(symbol))
                        {
                            ++intersectionCount;
                        }
                    }
                    if (intersectionCount != 1)
                    {
                        Console.Error.WriteLine("Card {0} and card {1} have {2} symbols in common", card, otherCard, intersectionCount);
                        Environment.Exit(1);
                    }
                }

                Console.WriteLine("Card {0} validated with no errors", card);
            }
            Console.WriteLine();
            Console.WriteLine("All cards validated with no errors!!\n");
            Environment.Exit(0);
            }
            finally
            {
                loggerFactory?.Dispose();
            }
        }
    }
}
