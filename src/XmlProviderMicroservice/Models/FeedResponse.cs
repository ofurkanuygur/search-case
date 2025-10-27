using System.Xml.Serialization;

namespace XmlProviderMicroservice.Models;

[XmlRoot("feed")]
public sealed class FeedResponse
{
    [XmlArray("items")]
    [XmlArrayItem("item")]
    public List<Item> Items { get; set; } = new();
    
    [XmlElement("meta")]
    public Meta Meta { get; set; } = new();
}
