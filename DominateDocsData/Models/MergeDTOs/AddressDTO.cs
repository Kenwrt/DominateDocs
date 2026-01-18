namespace DominateDocsData.Models;

public class AddressDTO
{
    public int PlaceID { get; set; }
    public string FullAddress { get; set; }

    public string StreetAddress { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string ZipCode { get; set; }

    public string County { get; set; }

    public string Country { get; set; }

    public double? Lat { get; set; }

    public double? Lng { get; set; }
}