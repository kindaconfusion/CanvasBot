using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using DSharpPlus;
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
            string json = System.IO.File.ReadAllText("config.json");
            JObject jsonData;
            try
            {
                jsonData = JObject.Parse(json);
            }
            catch (JsonReaderException e)
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

            discord.MessageCreated += async (s, e) =>
            {
                if (e.Message.Content.ToLower().StartsWith("ping"))
                    await e.Message.RespondAsync("pong!");
                if (e.Message.Content.ToLower().StartsWith("!assignments"))
                {
                    int hours;
                    if (e.Message.Content.Length > 12)
                    {
                        string[] args = e.Message.Content.ToLower().Substring(13).Split(" ");
                        hours = Int32.Parse(args[0]);
                    }
                    else
                    {
                        hours = 48;
                    }
                   
                    
                    //Debug.WriteLine(args[0]);
                    //Debug.WriteLine(hours);
                    Assignments(hours);
                    await e.Message.RespondAsync("Assignments due within " + hours + " hours: \n" + Assignments(hours));
                }
                if (e.Message.Content.ToLower().StartsWith("!courses"))
                    await e.Message.RespondAsync("Courses currently enrolled: \n" + Courses());
            };

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        static string Assignments(int hours)
        {
            string json = System.IO.File.ReadAllText("courses.json");
            User user = JsonConvert.DeserializeObject<User>(json);
            var canvasGetter = new CanvasGetter(canvasKey);
            List<Assignment> assignsDue = new List<Assignment>();
            foreach (Course c in user.Courses)
            {
                var ass = canvasGetter.GetAssignments(c.Id).Result;

                foreach (Assignment a in ass)
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
            
            
            string asses = "";
            List<Assignment> sortedAssignments = assignsDue.OrderBy(o => o.DueIn.TotalMilliseconds).ToList();
            List<string> dedupe = new List<string>();
            foreach (Assignment a in sortedAssignments)
            {
                if (dedupe.Contains(a.Name))
                    continue;
                string time = "";
                // time formatting sucks
                if (a.DueIn.Days > 0)
                {
                    if (a.DueIn.Days > 1)
                        time += (int)a.DueIn.Days + " days, ";
                    else
                        time += (int)a.DueIn.Days + " day, ";
                }
                if (a.DueIn.Hours > 0)
                {
                    if (a.DueIn.Hours > 1)
                        time += (int)a.DueIn.Hours + " hours, ";
                    else
                        time += (int)a.DueIn.Hours + " hour, ";
                }
                if (a.DueIn.Minutes > 0)
                {
                    if (a.DueIn.Minutes > 1)
                        time += (int)a.DueIn.Minutes + " minutes ";
                    else
                        time += (int)a.DueIn.Minutes + " minute ";
                }
                if (a.DueIn.TotalSeconds < 60)
                    time = " less than a minute!";
                asses += a.Name + " - due in " + time + "\n";
                dedupe.Add(a.Name);
            }

            return asses;
        }

        static string Courses()
        {
            var canvasGetter = new CanvasGetter(canvasKey);
            var result = canvasGetter.GetCourses(1).Result;
            string list = "";
            foreach (Course c in result)
            {
                list += c.Name + "\n";
                
            }
            return list;
        }

    }
}