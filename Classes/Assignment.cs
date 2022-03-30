using Newtonsoft.Json;

namespace CanvasBot.Classes
{
    public class CanvasAssignment
    {
        public string Name { get; set; }
        private string Title { get; set; }
        public string Id { get; set; }
        [JsonProperty("course_id")]
        public string CourseId { get; set; }
        [JsonProperty("due_at")]
        public string Due_At { get; set; }
        public string Lock_At { get; set; }

        public TimeSpan DueIn { get; set; }


    }
}
