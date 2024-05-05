using Newtonsoft.Json;

public class sendRequest
{
    [JsonProperty(PropertyName = "objects")]
    public string Objects { get; set; }

    [JsonProperty(PropertyName = "saving_path")]
    public string Saving_path { get; set; }

    [JsonProperty(PropertyName = "is_override")]
    public bool Is_override { get; set; }
    [JsonProperty(PropertyName = "is_new")]
    public bool Is_new { get; set; }
    [JsonProperty(PropertyName = "num_gen")]
    public int Num_gen { get; set; }
}