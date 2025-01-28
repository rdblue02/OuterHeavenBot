using System.Text.Json.Serialization;

namespace OuterHeavenLight.Entities.Request
{
    public class Userdata
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    } 
}
