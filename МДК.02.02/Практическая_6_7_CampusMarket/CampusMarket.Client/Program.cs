using CampusMarket.Client.Services;
using CampusMarket.Client.UI;

Console.OutputEncoding = System.Text.Encoding.UTF8;
// Адрес шлюза можно переопределить: CAMPUS_API_URL
var baseUrl = Environment.GetEnvironmentVariable("CAMPUS_API_URL") ?? "http://localhost:5080/api/v1/";
var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
await new ConsoleMenu(new CampusApiClient(http)).RunAsync();
