# FluentAds Client
## Just a library that uses Beckhoff.TwinCAT.Ads --version 6.0.74 that can save some time
----
# How to use
## 1. Include the FluentAdsClient namepsace in your program
    using FlientAdsClient; (line 6 in sample program)
---
## 2. Create a SimplifiedAdsClient by calling the constructor and check connection
    SimplifiedAdsClient plc = new SimplifiedAdsClient(amsNetID : "192.168.1.12.1.1", port: 851);
    Console.WriteLine(plc.CheckClientConnection());
---
## 3. If you successfully created the client, you can retrieve all the symbols with these methods
    Dictionary<string, dynamic> SymbolsDictionary = plc.GetAllSymbols();
    var AllVariables = plc.ConvertPlcDictionaryToClassList(SymbolsDictionary);
---
## 4. You can read all the states with this method
    AllVariables = await plc.ReadAll(AllVariables);
---
## 5. You can write to a specific variable via a string and value. It has a built in conversion and returns FALSE if it failed to write
    await plc.WriteToVariable("IO.nThing2", 588);
## 6. If you want to constantly poll the states in a Real-Time application, you may create an async Task to constantly read the variable states. 
---
## You can declare this method within your main method after creating the client (For instance, on the initialisation of a WPF page) Full example in Program.cs.
#### Note. I did not use the "await" keyword when calling the pollInfinite() method in this case as i wish for this method to run async. However, if you want to do synchronous polling, feel free to use "await pollInifinite() instead." 
#### However, I chose to await Task.Delay(20) as I wanted to delay 20ms before re-reading the variables on the PLC
    async Task pollInfinite()
    {
        while(true)
        {
            AllVariables = await plc.ReadAll(AllVariables);
            await Task.Delay(20);
        }
    }
    pollInfinite();
# Enquiries
## Email me at maxweliau12345@gmail.com if you have any questions