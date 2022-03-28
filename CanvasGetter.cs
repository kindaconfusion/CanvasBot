using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CanvasBot
{ 
    public class CanvasGetter
    {
        private string Token;
        private static HttpClient client;

        public CanvasGetter(string token)
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<Assignment>> GetAssignments(long courseId)
        {
            List<Assignment> allAssignments = new List<Assignment>();
            var assresponse = await client.GetAsync("https://canvas.instructure.com/api/v1/courses/" + courseId + "/assignments?per_page=1000");
            if (assresponse.IsSuccessStatusCode)
            {
                var assresult = await assresponse.Content.ReadAsStringAsync();
                List<Assignment> ass = JsonConvert.DeserializeObject<List<Assignment>>(assresult);
                allAssignments.AddRange(ass);
            }
            
            var quizresponse = await client.GetAsync("https://canvas.instructure.com/api/v1/courses/" + courseId + "/quizzes?per_page=1000");
            if (quizresponse.IsSuccessStatusCode)
            {
                var quizresult = await quizresponse.Content.ReadAsStringAsync();
                List<Assignment> quiz = JsonConvert.DeserializeObject<List<Assignment>>(quizresult);
                allAssignments.AddRange(quiz);
            }

            //List<Assignment> sortedAssignments = allAssignments.OrderBy(o => o.Lock_At).ToList();
            return allAssignments;
        }

        public async Task<List<Course>> GetCourses(long userId)
        {
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
            await File.WriteAllTextAsync("courses.json", output);
            return courses;
        }

    }

    public class Assignment
    {
        public string Name { get; set; }

        [JsonProperty("title")] 
        private string Title
        {
            set { Name = value; }
        }
        public string Id { get; set; }
        public string CourseId { get; set; }
        [JsonProperty("due_at")]
        public string Due_At { get; set; }
        public string Lock_At { get; set; }


    }

    public class Course
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "start_at")]
        public string Start_At { get; set; }
    }

    public class User
    {
        public long DiscordId { get; set; }

        public long CanvasId { get; set; }
        public List<Course> Courses { get; set; }
    }

}
