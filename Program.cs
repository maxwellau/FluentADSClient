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
            // create client and check connection
            SimplifiedAdsClient plc = new SimplifiedAdsClient(amsNetID : "192.168.1.12.1.1", port: 851);
            Console.WriteLine(plc.CheckClientConnection());

            // get all symbols into {variable name : datatype} dictionary, adn then convert it into a class list
            Dictionary<string, dynamic> SymbolsDictionary = plc.GetAllSymbols();
            var AllVariables = plc.ConvertPlcDictionaryToClassList(SymbolsDictionary);

            // read all states ONCE
            AllVariables = await plc.ReadAll(AllVariables);

            // write to variable (built in conversion is included)
            await plc.WriteToVariable("IO.nThing2", 588);

            // for constant polling with client that is declared in line 15 (plc)
            // async Task pollInfinite()
            // {
            //     while(true)
            //     {
            //         AllVariables = await plc.ReadAll(AllVariables);
            //         await Task.Delay(20);
            //     }
            // }
            // await pollInfinite();
            var nigga = await plc.ReadSingleVariable("IO.nThing2");
            System.Console.WriteLine(nigga);
        }
    }
}
