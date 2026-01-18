namespace LiquidDocsData.Enums;

//usage:
//var county = CountyLookup.GetCounty(USStates.CA, "Los Angeles");

//if (county != null)
//    Console.WriteLine($"County: {county}");
//else
//    Console.WriteLine("County not found.");

public static class CountyLookup
{
    // Format: State → City → County
    //private static readonly Dictionary<USStates, Dictionary<string, string>> Data = new()
    //{
    //    [USStates.CA] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    //    {
    //        ["Los Angeles"] = "Los Angeles County",
    //        ["San Francisco"] = "San Francisco County",
    //        ["San Diego"] = "San Diego County",
    //        ["Fresno"] = "Fresno County"
    //    },
    //    [USStates.TX] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    //    {
    //        ["Houston"] = "Harris County",
    //        ["Austin"] = "Travis County",
    //        ["Dallas"] = "Dallas County",
    //        ["San Antonio"] = "Bexar County"
    //    },
    //    [USStates.FL] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    //    {
    //        ["Miami"] = "Miami-Dade County",
    //        ["Orlando"] = "Orange County",
    //        ["Tampa"] = "Hillsborough County"
    //    },
    //    [USStates.NY] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    //    {
    //        ["New York"] = "New York County",
    //        ["Buffalo"] = "Erie County",
    //        ["Rochester"] = "Monroe County"
    //    }
    //    // Add more as needed
    //};

    //public static string? GetCounty(USStates state, string city)
    //{
    //    if (Data.TryGetValue(state, out var cities) && cities.TryGetValue(city, out var county))
    //        return county;

    //    return null;
    //}
}