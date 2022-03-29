using System.Diagnostics;
using System.Net.Http.Headers;
using CanvasBot.Classes;
using Newtonsoft.Json;

namespace CanvasBot
{ 
    public class CanvasHandler
    {
        private static HttpClient client;
        private static List<User> users;

        public CanvasHandler(string token)
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<CanvasAssignment>> GetAssignments(long courseId)
        {
            List<CanvasAssignment> allAssignments = new List<CanvasAssignment>();
            var assresponse = await client.GetAsync($"https://canvas.instructure.com/api/v1/courses/{courseId}/assignments?per_page=1000");
            if (assresponse.IsSuccessStatusCode)
            {
                var assresult = await assresponse.Content.ReadAsStringAsync();
                List<CanvasAssignment> ass = JsonConvert.DeserializeObject<List<CanvasAssignment>>(assresult);
                allAssignments.AddRange(ass);
            }
            
            var quizresponse = await client.GetAsync($"https://canvas.instructure.com/api/v1/courses/{courseId}/quizzes?per_page=1000");
            if (quizresponse.IsSuccessStatusCode)
            {
                var quizresult = await quizresponse.Content.ReadAsStringAsync();
                List<CanvasAssignment> quiz = JsonConvert.DeserializeObject<List<CanvasAssignment>>(quizresult);
                allAssignments.AddRange(quiz);
            }

            //List<Assignment> sortedAssignments = allAssignments.OrderBy(o => o.Lock_At).ToList();
            return allAssignments;
        }

        public async Task<User> GetUserInfo(long userId)
        {
            var check = users.Find(o => o.CanvasId == userId);
            if (check != null)
            {
                return check;
            }

            string json = File.ReadAllText("courses.json");
            var load = JsonConvert.DeserializeObject<User>(json);
            if(load.CanvasId == userId)
            {
                return load;
            }


            var response = await client.GetAsync("https://canvas.instructure.com/api/v1/courses?per_page=1000");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            //Debug.WriteLine(result);
            //Debug.WriteLine(output);
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;
            settings.NullValueHandling = NullValueHandling.Ignore;
            List<Course> courses = JsonConvert.DeserializeObject<List<Course>>(result, settings);
            for (int i=0; i<courses.Count; i++)
            {
                try
                {
                    if (string.IsNullOrEmpty(courses[i].Name))
                    {
                        courses.RemoveAt(i);
                        i--;
                        continue;
                    }
                    DateTime startDate = DateTime.Parse(courses[i].Start_At);
                    //Debug.WriteLine(startDate);
                    if (startDate.Month == 8 && DateTime.Now.Month < 6)
                    {
                        courses.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (startDate.Month == 1 && DateTime.Now.Month > 5)
                    {
                        courses.RemoveAt(i);
                        i--;
                        continue;
                    }
                    if (startDate.Month == 12 && DateTime.Now.Month > 5)
                    {
                        courses.RemoveAt(i);
                        i--;
                        continue;
                    }
                    //Debug.WriteLine(c.Name);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("we fucked up");
                }

            }
            User user = new User();
            user.DiscordId = 1;
            user.CanvasId = 1;
            user.Courses = courses;
            string output = JsonConvert.SerializeObject(user);
            File.WriteAllTextAsync("courses.json", output);
            users.Add(user);

            return user;
        }

    }


}
