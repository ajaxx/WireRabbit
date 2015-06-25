using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Nito.KitchenSink.OptionParsing;

namespace Revionics.WireRabbit
{
    public sealed class AppOptions : IOptionArguments
    {
        public AppOptions()
        {
            Patterns = new List<string>();
        }

        [Option("broker", 'B', OptionArgument.Required)]
        public string BrokerUri { get; set; }

        [Option("exchange", 'e', OptionArgument.Required)]
        public string Exchange { get; set; }

        [PositionalArguments]
        public List<string> Patterns { get; private set; }

        public void Validate()
        {
        }

        public static int Usage()
        {
            Console.Error.WriteLine("Usage: app [OPTIONS] ...");
            return -1;
        }
    }
}
