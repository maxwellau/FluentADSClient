# FluentAds Client
## Just a library that uses Beckhoff.TwinCAT.Ads --version 6.0.74 that can save some time
----
# How to use
## 1. Include the FluentAdsClient namepsace in your program
### using FlientAdsClient; (line 6 in sample program)
---
## 2. Create a SimplifiedAdsClient by calling the constructor and check connection
### SimplifiedAdsClient plc = new SimplifiedAdsClient(amsNetID : "192.168.1.12.1.1", port: 851);
### Console.WriteLine(plc.CheckClientConnection());
---
## 3. If you successfully created the client, you can retrieve all the symbols with these methods
### Dictionary<string, dynamic> SymbolsDictionary = plc.GetAllSymbols();
### var AllVariables = plc.ConvertPlcDictionaryToClassList(SymbolsDictionary);
---
## 4. You can read all the states with this method
### AllVariables = await plc.ReadAll(AllVariables);
---
## Note: If you want to constantly poll the states in a Real-Time application, you may create an async Task to constantly read the variable states
# Enquiries
## Email me at maxweliau12345@gmail.com if you have any questions