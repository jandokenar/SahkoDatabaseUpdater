using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace SahkoDatabaseUpdater
{
    public class SahkoUpdate // hakee tunnin välein sen tunnin hinnan ja tallentaa sen
    {
        [FunctionName("SahkoUpdate")]

        // ajetaan minuutin välein (debug syyt), mutta haetaan uutta dataa vain jos tunti muuttuu
        public void Run([TimerTrigger("* * * * *")] TimerInfo myTimer, ILogger log)
        {
            string fileNameStr = @"C:\Temp\data.txt";
            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            string hourStr = DateTime.Now.ToString("HH");
            string lastLineStr = string.Empty;

            if (File.Exists(fileNameStr))
            {
                lastLineStr = File.ReadLines(fileNameStr).Last(); // luetaan viimeisen rivin data
            }

            if (lastLineStr.StartsWith(todayStr + " " + hourStr)) // tarkastetaan löytyykö tunnin data jo
            {
                log.LogInformation("This hour value is already stored to database.");
            }
            else
            {
                log.LogInformation("Storing this hour value to database");
   
                string url = @"https://api.porssisahko.net/v1/price.json?date=" + todayStr + "&hour=" + hourStr;

                // luodaan pyyntö apiin
                using var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = httpClient.Send(request);
                using var reader = new StreamReader(response.Content.ReadAsStream());
                var responseBody = reader.ReadToEnd();

                //kirjoitetaan data tiedostoon
                File.AppendAllText(fileNameStr, "\r\n" + todayStr + " " + hourStr + " " + responseBody.Substring(9, responseBody.Length - 10));
            }
        }
    }

    public static class JsonApi // api joka palauttaa viikon takaiset hinnat json muodossa
    {
        [FunctionName("PricesJson7days")]

        public static async Task<IActionResult> Run( // json api joka palauttaa viimeisen 7 päivän hinnaston
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {

            string readJSON = string.Empty;
            string fileNameStr = @"C:\Temp\data.txt";
            DateTime dateRow = new DateTime();

            readJSON += "{\"prices\":["; //json header

            const Int32 BufferSize = 128;
            using (var fileStream = File.OpenRead(fileNameStr))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null) // luetaan tiedosto rivi riviltä
                {
                    // log.LogInformation(line);
                    dateRow = DateTime.Parse(line.Substring(0, 13) + ":00");

                    if ((DateTime.Now - dateRow).TotalDays < 7) // näytä vain viikon vanhaa dataa
                    {
                        readJSON += "{\"price\":" + line.Substring(14, line.Length - 14) + ",\"timeDate\":\"" + line.Substring(0, 13) + "\"},";
                    }
                }
                readJSON = readJSON.Substring(0, readJSON.Length - 2); // viimeistele jsonin loppu
            }
            readJSON += "}]}"; // json end

            return new OkObjectResult(readJSON);
        }
    }
}

