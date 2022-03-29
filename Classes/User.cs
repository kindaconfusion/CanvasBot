namespace CanvasBot.Classes
{
    public class User
    {
        public long DiscordId { get; set; }

        public long CanvasId { get; set; }
        public List<Course> Courses { get; set; }
    }
}
