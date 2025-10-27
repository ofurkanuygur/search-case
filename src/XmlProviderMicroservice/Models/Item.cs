using System.Xml.Serialization;

namespace XmlProviderMicroservice.Models;

public sealed class Item
{
    [XmlElement("id")]
    public string Id { get; set; } = string.Empty;
    
    [XmlElement("headline")]
    public string Headline { get; set; } = string.Empty;
    
    [XmlElement("type")]
    public string Type { get; set; } = string.Empty;
    
    [XmlElement("publication_date")]
    public string PublicationDate { get; set; } = string.Empty;
    
    [XmlElement("stats")]
    public ItemStats Stats { get; set; } = new();
    
    [XmlArray("categories")]
    [XmlArrayItem("category")]
    public List<string> Categories { get; set; } = new();
}
