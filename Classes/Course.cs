using Newtonsoft.Json;

namespace CanvasBot.Classes
{
    public class Course
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "start_at")]
        public string Start_At { get; set; }
    }
}
