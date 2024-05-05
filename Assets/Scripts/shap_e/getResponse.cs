using Newtonsoft.Json;

public class getResponse
{
    [JsonProperty(PropertyName = "data")]
    public string Data { get; set; }
}