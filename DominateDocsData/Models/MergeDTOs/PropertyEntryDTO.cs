using Microsoft.AspNetCore.Components;

namespace DominateDocsData.Models;

public class PropertyEntryDTO
{
    public AddressDTO Address { get; set; } = new();
    public ElementReference ContainerRef { get; set; }
    public string MarkerId { get; set; } = Guid.NewGuid().ToString();
}