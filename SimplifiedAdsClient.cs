namespace FluentAdsClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TwinCAT;
    using TwinCAT.Ads;
    using TwinCAT.Ads.TypeSystem;
    using TwinCAT.Ads.ValueAccess;
    using TwinCAT.TypeSystem;
    using TwinCAT.TypeSystem.Generic;
    public class SimplifiedAdsClient
    {
        public string AmsNetID;
        public int Port;
        public AdsClient Client;
        public List<PlcVariableClass> AllVariables;

        #region classes and constructors
        public SimplifiedAdsClient(string amsNetID, int port)
        {
            // some constructor stuff
            this.AmsNetID = amsNetID;
            this.Port = port;
            this.Client = new AdsClient(); // create twincat ads client
            AmsAddress address = new AmsAddress(this.AmsNetID, this.Port); // create ams address object
            this.Client.Connect(address); // connect to twincat server
        }

        // class instance of PLC variable
        public class PlcVariableClass
        {
            public PlcVariableClass(string name, Type nativeType) // constructor
            {
                Name = name;
                NativeType = nativeType;
            }
            public string Name { get; set; }
            public Type NativeType { get; set; }
            public ResultAnyValue Value { get; set; }
            public dynamic State { get { return Value.Value; } }
            public uint Handler { get; set; }
        }

        #endregion

        #region general helper functions

        // debugging helper function, meant for error handling
        public bool CheckClientConnection()
        {
            return this.Client.IsConnected;
        }


        // internal helper function to filter unneeded namespaces
        private static bool IncludeVariable(string child, string[] parent)
        {
            foreach(var p in parent)
            {
                if(child.Contains(p))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region main logic methods within this class
        public Dictionary<string, dynamic> GetAllSymbols()
        {
            // create hashtable
            Dictionary<string, string> PlcVariableToType = new Dictionary<string, string>(); // create hashtable/dictionary of varaiblename to datatype

            // create symbol loaders
            SymbolLoaderSettings parentSettings = new SymbolLoaderSettings(SymbolsLoadMode.DynamicTree); // bro tbh, idek what this does i just trial and error and it worked lmao
            IAdsSymbolLoader parentLoader = (IAdsSymbolLoader)SymbolLoaderFactory.Create(Client, parentSettings);

            SymbolLoaderSettings childSettings = new SymbolLoaderSettings(SymbolsLoadMode.Flat); // same for this lol
            IAdsSymbolLoader childLoader = (IAdsSymbolLoader)SymbolLoaderFactory.Create(Client, childSettings);

            // load symbol info (assuming non-cancellable call in this case)
            var parentSymbol = parentLoader.GetSymbolsAsync(new CancellationToken());
            var childSymbol = childLoader.GetSymbolsAsync(new CancellationToken());

            // note the namespaces to exclude
            string[] exclude = {"Constants", "Global_Version", "TwinCAT_SystemInfoVarList"};

            // check for null
            if(parentSymbol.Result.Symbols != null && childSymbol.Result.Symbols != null)
            {
                foreach(var n in childSymbol.Result.Symbols)
                {
                    if(IncludeVariable(n.ToString(),exclude)) // filter non-needed namespaces
                    {
                        PlcVariableToType.Add(n.InstancePath.ToString(),n.DataType.ToString()); // append to hashtable
                    }
                } 
            }
            // convert hashtable of {string:string} to native c# {string:datatype} and return
            return MapTwincatVariablesToCSharpVariables(PlcVariableToType); 
        }

        // convert {string:string} hashtable to {string:native c# datatype}
        private static Dictionary<string, dynamic> MapTwincatVariablesToCSharpVariables(Dictionary<string, string> DecodedTCVariables)
        {
            Dictionary<string, dynamic> PlcVariablesToNativeType = new Dictionary<string, dynamic>(); // create new hashtable of tcvariable to native c# datatype
            foreach(var n in DecodedTCVariables)
            {
                if (n.Value == "BOOL") PlcVariablesToNativeType.Add(n.Key, typeof(bool)); // map BOOL to native boolean
                if (n.Value == "INT") PlcVariablesToNativeType.Add(n.Key, typeof(Int16)); // map INT to native int16
                if (n.Value == "REAL") PlcVariablesToNativeType.Add(n.Key, typeof(float)); // map REAL to native float
            }
            return PlcVariablesToNativeType;
        }

        public List<PlcVariableClass> ConvertPlcDictionaryToClassList(Dictionary<string, dynamic> PlcToNativeVariables)
        {
            AllVariables = new List<PlcVariableClass>(); // create list of PlcVariableClass
            // Iteratively append each element of hashtable into AllVariables list
            foreach (var p in PlcToNativeVariables) { AllVariables.Add(new PlcVariableClass(p.Key, p.Value)); }
            // define variable handler iteratively
            foreach (var l in AllVariables) { l.Handler = Client.CreateVariableHandle(l.Name); }
            return AllVariables;
        }
        public async Task<List<PlcVariableClass>> ReadAll(List<PlcVariableClass> AllVariables, int sleepTime = 50)
        {
            //start reading async
            foreach(var l in AllVariables)
                {
                    //note the getter and setter in the plc variable class
                    l.Value = await Client.ReadAnyAsync(l.Handler, l.NativeType, new CancellationToken());
                    Console.WriteLine($"{l.Name}\t{l.State}");
                }
            return AllVariables;
        }
        #endregion
    }   
}