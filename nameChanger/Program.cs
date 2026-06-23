using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string lockfilePath = "";

        Process[] lolProcesses = Process.GetProcessesByName("LeagueClientUx");

        if (lolProcesses.Length > 0)
        {
            try
            {
                string exePath = lolProcesses[0].MainModule.FileName;
                string lolDirectory = Path.GetDirectoryName(exePath);
                lockfilePath = Path.Combine(lolDirectory, "lockfile");
            }
            catch (Exception)
            {
                Console.WriteLine("Permission error: Try opening this program as an administrator.");
                Console.ReadLine();
                return;
            }
        }

        if (string.IsNullOrEmpty(lockfilePath) || !File.Exists(lockfilePath))
        {
            Console.WriteLine("Lockfile wasn't found. Is League open?.");
            Console.ReadLine();
            return;
        }

        string lockfileContent;

        try
        {
            using (var fileStream = new FileStream(lockfilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream))
            {
                lockfileContent = streamReader.ReadToEnd();
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Lockfile wasn't found. Is League open?");
            Console.ReadLine();
            return;
        }

        string[] lockfileData = lockfileContent.Split(':');
        string port = lockfileData[2];
        string password = lockfileData[3];
        string protocol = lockfileData[4];

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        };

        using var client = new HttpClient(handler);
        client.BaseAddress = new Uri($"{protocol}://127.0.0.1:{port}");
        var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        Console.WriteLine("Name: ");
        string name = Console.ReadLine();

        Console.WriteLine("Tag: ");
        string tag = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(tag))
        {
            Console.WriteLine("Insert a valid name/tag.");
            Console.WriteLine("\nPress Enter.");
            Console.ReadLine();
        }
        else if (name.Length > 16)
        {
            Console.WriteLine("Name length is bigger than 16.");
            Console.WriteLine("\nPress Enter.");
            Console.ReadLine();
        }
        else if (tag.Length > 5)
        {
            Console.WriteLine("Tag length is bigger than 5");
            Console.WriteLine("\nPress Enter.");
            Console.ReadLine();
        }
        else
        {
            var body = new
            {
                gameName = name,
                tagLine = tag
            };

            string jsonPayload = JsonSerializer.Serialize(body);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/lol-summoner/v1/save-alias", content);

            string resultText = await response.Content.ReadAsStringAsync();
            Console.WriteLine(resultText);

            Console.WriteLine("\nPress Enter.");
            Console.ReadLine();
        }
    }
}