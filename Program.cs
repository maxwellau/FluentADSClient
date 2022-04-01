using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAdsClient;

namespace Program
{
    class Program
    {
        public static async Task Main()
        {
            SimplifiedAdsClient plc = new SimplifiedAdsClient(amsNetID : "192.168.1.12.1.1", port: 851);
            Console.WriteLine(plc.CheckClientConnection());
            Dictionary<string, dynamic> SymbolsDictionary = plc.GetAllSymbols();
            var AllVariables = plc.ConvertPlcDictionaryToClassList(SymbolsDictionary);
            AllVariables = await plc.ReadAll(AllVariables);
            Console.ReadKey();
        }
    }
}
