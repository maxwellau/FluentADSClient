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
            public int[] Marshall { get; set; }
        }

        #endregion

        #region general helper functions

        // debugging helper function, meant for error handling
        public bool CheckClientConnection()
        {
            return this.Client.IsConnected;
        }

        private static int GetIndexFromString(string Name, List<PlcVariableClass> ClassList)
        {
            for(int i = 0; i < ClassList.Count; i++)
            {
                if(Name == ClassList[i].Name)
                {
                    return i;
                }
            }
            return -1;
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
        private static int[] AutoMarshaller(PlcVariableClass plcVariable)
        {
            if(plcVariable.NativeType == typeof(string)) return new int[] {80};
            return null;
        }

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
            foreach(var v in PlcVariableToType)
            {
                System.Console.WriteLine(v.Key + "\t" + v.Value);
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
                //  i would use case but the case syntax for different c# versions are different, so i aint risking it lol
                if (n.Value == "BOOL") PlcVariablesToNativeType.Add(n.Key, typeof(bool)); // map BOOL to native boolean
                if (n.Value == "BYTE") PlcVariablesToNativeType.Add(n.Key, typeof(byte)); // map BYTE to native byte
                if (n.Value == "UINT") PlcVariablesToNativeType.Add(n.Key, typeof(ushort)); // map UINT to native ushort
                if (n.Value == "INT") PlcVariablesToNativeType.Add(n.Key, typeof(short)); // map INT to native short
                if (n.Value == "DINT") PlcVariablesToNativeType.Add(n.Key, typeof(int)); // map DINT to native int
                if (n.Value == "UDINT") PlcVariablesToNativeType.Add(n.Key, typeof(uint)); // map DINT to native uint
                if (n.Value == "REAL") PlcVariablesToNativeType.Add(n.Key, typeof(float)); // map REAL to native float
                if (n.Value == "LREAL") PlcVariablesToNativeType.Add(n.Key, typeof(double)); // map LREAL to native double
                if (n.Value == "STRING(80)") PlcVariablesToNativeType.Add(n.Key, typeof(string)); // map STRING(80) to native string
            }
            return PlcVariablesToNativeType;
        }

        public List<PlcVariableClass> ConvertPlcDictionaryToClassList(Dictionary<string, dynamic> PlcToNativeVariables)
        {
            AllVariables = new List<PlcVariableClass>(); // create list of PlcVariableClass
            // Iteratively append each element of hashtable into AllVariables list
            foreach (var p in PlcToNativeVariables) { AllVariables.Add(new PlcVariableClass(p.Key, p.Value)); }
            // define variable handler iteratively
            foreach (var l in AllVariables) 
            { 
                l.Handler = Client.CreateVariableHandle(l.Name); 
                l.Marshall = AutoMarshaller(l);
            }
            return AllVariables;
        }
        public async Task<List<PlcVariableClass>> ReadAll(List<PlcVariableClass> AllVariables, int sleepTime = 50)
        {
            if(!Client.IsConnected) // check if connection is alive
            {
                System.Console.WriteLine("Client Not Connected");
                return new List<PlcVariableClass>{new PlcVariableClass("PLC NOT CONNECTED", typeof(string))};
            }
            //start reading async
            foreach(var l in AllVariables)
                {
                    //note the getter and setter in the plc variable class
                    l.Value = await Client.ReadAnyAsync(variableHandle : l.Handler,
                                                        type : l.NativeType,
                                                        args : l.Marshall,
                                                        cancel: new CancellationToken());
                    Console.WriteLine($"{l.Name}\t{l.State}");
                }
            return AllVariables;
        }
        public async Task<dynamic> ReadSingleVariable(string VariableName)
        {
            if(!Client.IsConnected) // check if connection is alive
            {
                System.Console.WriteLine("Client Not Connected"); 
                return null;
            }
            int i = GetIndexFromString(VariableName, AllVariables); // get index from list of plc class, returns -1 if not found
            if(i != -1)
            {
                var plcVariable = AllVariables[i];
                dynamic dataType = plcVariable.NativeType;
                try
                {
                    plcVariable.Value = await Client.ReadAnyAsync(variableHandle : plcVariable.Handler,
                                                        type : plcVariable.NativeType,
                                                        args : plcVariable.Marshall,
                                                        cancel: new CancellationToken());
                    if(plcVariable.Value.Failed)
                    {
                        System.Console.WriteLine("Failed to read value");
                        return null;
                    }
                    System.Console.WriteLine($"{plcVariable.Name} value is {plcVariable.State}"); // Note the getter and setter in PlcVariableClass
                    return plcVariable.State;
                }
                catch
                {
                    System.Console.WriteLine("Cannot convert target datatype to C# datatype");
                    return null;
                }
            }
            System.Console.WriteLine("Variable not found");
            return null;
        }
        public async Task<bool> WriteToVariable(string VariableName, dynamic WriteValue)
        {
            if(!Client.IsConnected) // check if connection is alive
            {
                System.Console.WriteLine("Client Not Connected");
                return false;
            }
            int i = GetIndexFromString(VariableName, AllVariables); // get index from list of plc class, returns -1 if not found
            if(i != -1)
            {
                var plcVariable = AllVariables[i];
                dynamic dataType = plcVariable.NativeType;
                try
                {
                    ResultWrite n = await Client.WriteAnyAsync(variableHandle : plcVariable.Handler,
                                                            value : Convert.ChangeType(WriteValue, dataType),
                                                            args : plcVariable.Marshall, 
                                                            cancel : new CancellationToken()); //writes to plc
                    if(n.Failed)
                    {
                        System.Console.WriteLine("Failed to write value");
                        return false;
                    }
                    System.Console.WriteLine($"Successfully wrote {WriteValue} to {plcVariable.Name}");
                    return true;
                }
                catch
                {
                    System.Console.WriteLine("Cannot convert convert given value to target datatype");
                    return false;
                }
                
            }
            System.Console.WriteLine("Variable not found");
            return false;
        }
        #endregion
    }   
}