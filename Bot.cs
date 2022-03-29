using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CanvasBot.Classes;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CanvasBot
{
    class Bot
    {
        static string canvasKey;
        static string discordKey;
        static void Main(string[] args)
        {
            if (!File.Exists("config.json"))
            {

                var file = File.Create("config.json");
                file.Close();
                initConfig();
            }
            string json = File.ReadAllText("config.json");
            JObject jsonData;
            try
            {
                jsonData = JObject.Parse(json);
            }
            catch (JsonReaderException)
            {
                initConfig();
            }
            jsonData = JObject.Parse(json);
            canvasKey = jsonData["canvasKey"].ToString();
            discordKey = jsonData["discordKey"].ToString();

            Console.WriteLine("Starting CanvasBot...");
            //Debug.WriteLine(Assignments(48));
            MainAsync().GetAwaiter().GetResult();
        }

        static void initConfig()
        {
            Console.WriteLine("Looks like it's your first time running CanvasBot.\nLet's get you set up.");
            Console.WriteLine("First, we'll need your Canvas API key. Please enter it now:");
            canvasKey = Console.ReadLine();
            Console.WriteLine("Verifying...");
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", canvasKey);
            Task<HttpResponseMessage> response;
            do
            {
                response = client.GetAsync("https://canvas.instructure.com/api/v1/users/self/profile");
                if (response.Result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Invalid token. Please try again:");
                    canvasKey = Console.ReadLine();
                    client.DefaultRequestHeaders.Authorization
                        = new AuthenticationHeaderValue("Bearer", canvasKey);
                    Console.WriteLine("Verifying...");
                }
            }
            while (response.Result.StatusCode == HttpStatusCode.Unauthorized);
            Console.WriteLine("Canvas key accepted.\nNow, we'll need your Discord API key.\nPlease enter it now:");
            string discord = Console.ReadLine();
            var config = new Dictionary<string, string> {
                    {"discordKey", discord},
                    {"canvasKey", canvasKey}
                };
            var output = JsonConvert.SerializeObject(config);
            File.WriteAllText("config.json", output);
        }

        static async Task MainAsync()
        {
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = discordKey,
                TokenType = TokenType.Bot
            });
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "c!" }
            });

            commands.RegisterCommands<CmdModule>();


            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        public static string Assignments(int hours)
        {
            var canvasHandler = new CanvasHandler(canvasKey);
            User user = canvasHandler.GetUserInfo(1).Result;
            List<CanvasAssignment> assignsDue = new List<CanvasAssignment>();
            foreach (Course c in user.Courses)
            {
                // Get Assignments for course.
                var ass = canvasHandler.GetAssignments(c.Id).Result;

                foreach (CanvasAssignment a in ass)
                {
                    DateTime dueDate;
                    if (a.Lock_At is null && a.Due_At is null)
                    {
                        continue;
                    }
                    else if (a.Lock_At is null && a.Due_At is not null)
                    {

                        dueDate = DateTime.Parse(a.Due_At); 
                    }
                    else {
                        dueDate = DateTime.Parse(a.Lock_At);
                    }
                    a.DueIn = dueDate.Subtract(DateTime.Now);
                    //Debug.WriteLine(timeDiff);
                    if (a.DueIn.TotalMilliseconds > 0 && a.DueIn.TotalHours < hours)
                    {
                        assignsDue.Add(a);
                    }
                }
            }
            //var result = canvasGetter.GetAssignments(1).Result;
            
            
            StringBuilder asses = new StringBuilder();
            List<CanvasAssignment> sortedAssignments = assignsDue.OrderBy(o => o.DueIn.TotalMilliseconds).ToList();
            List<string> dedupe = new List<string>();
            foreach (CanvasAssignment a in sortedAssignments)
            {
                if (dedupe.Contains(a.Name))
                    continue;
                asses.Append($"{a.Name} - due in ");
                // time formatting sucks
                if (a.DueIn.Days > 0)
                {
                    if (a.DueIn.Days > 1)
                        asses.Append($"{a.DueIn.Days} days, ");
                    else
                        asses.Append($"{a.DueIn.Days} day, ");
                }
                if (a.DueIn.Hours > 0)
                {
                    if (a.DueIn.Hours > 1)
                        asses.Append($"{a.DueIn.Hours} Hours, ");
                    else
                        asses.Append($"{a.DueIn.Hours} Hour, ");
                }
                if (a.DueIn.Minutes > 0)
                {
                    if (a.DueIn.Minutes > 1)
                        asses.Append($"{a.DueIn.Minutes} Minutes, ");
                    else
                        asses.Append($"{a.DueIn.Minutes} Minute, ");
                }
                if (a.DueIn.TotalSeconds < 60)
                    asses.Append(" less than a minute!");
                asses.Append("\n");
                dedupe.Add(a.Name);
            }

            return asses.ToString();
        }

        public static string Courses()
        {
            var canvasGetter = new CanvasHandler(canvasKey);
            var result = canvasGetter.GetUserInfo(1).Result;

            StringBuilder list = new StringBuilder();
            foreach (Course c in result.Courses)
            {
                list.Append($"{c.Name}\n");
                
            }
            return list.ToString();
        }

    }
}