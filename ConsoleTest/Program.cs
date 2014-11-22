
namespace CardsAndSymbols.ConsoleTest
{
    using ProjectivePlane;
    using System;
    using System.Linq;

    public class Program
    {
        private const int NumCards = 55;
        private const int NumSymbols = 57;

        public static void Main(string[] args)
        {
            var symbols = Symbol.CreateDefaultSymbolList(NumSymbols);
            var constructor = new ProjectivePlaneConstructor<Symbol>(symbols, NumCards);
            var points = constructor.PlanePoints;

            if (ProjectivePlaneConstructor<Symbol>.VerifyPointLines(points))
            {
                // Map points to cards and lines to symbols
                var cards = points.Select(point => new Card<Symbol>(point.Lines)).ToList();
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

            Console.WriteLine("\nPress any key to continue");
            Console.ReadKey();
        }
    }
}
