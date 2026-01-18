namespace LiquidDocsData.Enums;

// Example usage:
//string zip = "94105";
//var location = ZipCodeDirectory.Lookup(zip);

//if (location is not null)
//{
//    Console.WriteLine($"ZIP {zip} corresponds to {location.Value.City}, {location.Value.State}");
//}
//else
//{
//    Console.WriteLine("ZIP code not found.");
//}

public static class ZipCodeDirectory
{
    //private static readonly Dictionary<string, (string City, USStates State)> ZipCodeMap = new()
    //{
    //    // Add as many as needed, here are sample entries
    //    ["10001"] = ("New York", USStates.NY),
    //    ["90001"] = ("Los Angeles", USStates.CA),
    //    ["60601"] = ("Chicago", USStates.IL),
    //    ["73301"] = ("Austin", USStates.TX),
    //    ["94105"] = ("San Francisco", USStates.CA),
    //    ["33101"] = ("Miami", USStates.FL),
    //    ["30301"] = ("Atlanta", USStates.GA),
    //    ["85001"] = ("Phoenix", USStates.AZ),
    //    ["98101"] = ("Seattle", USStates.WA),
    //    ["20001"] = ("Washington", USStates.DC)
    //};

    //public static (string City, USStates State)? Lookup(string zip)
    //{
    //    return ZipCodeMap.TryGetValue(zip, out var result) ? result : null;
    //}
}