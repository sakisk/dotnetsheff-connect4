using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace dotnetsheff.Connect4
{
    class Program
    {
        
        static async Task Main(string[] args)
        {
            var teamId = (await Register()).Trim('"');

            await StartGame(teamId);
            await Play(teamId);
            
            Console.WriteLine(teamId); 
            Console.ReadKey();
        }

        private static async Task Play(string teamID)
        {
            var gameResponse = await GetGameState(teamID);
            var player = gameResponse.YellowPlayerID.ToString().Trim('"') == teamID ? 2 : 1; 
            
            while (!(gameResponse.CurrentState == GameState.YellWon || gameResponse.CurrentState == GameState.RedWon || gameResponse.CurrentState == GameState.Draw))
            {
                switch (gameResponse.CurrentState)
                {
                    case GameState.GameNotStarted: 
                        Move(teamID);
                        break;
                    case GameState.RedToPlay when player == 1:
                        Move(teamID);
                        break; 
                    case GameState.YellowToPlay when player == 2:
                        Move(teamID);
                        break; 
                    default: break;
                }
                
                gameResponse = await GetGameState(teamID);
            }
            
            Console.WriteLine("Game Over!");
        }

        private static async Task Move(string teamId)
        {
           
            using var client = new HttpClient();
            var body = new Dictionary<string, string>
            {
                {"playerID", teamId},
                {"Password", "password"},
                {"ColumnNumber", new Random().Next(1,7).ToString()}
            };
            
            var content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            
            await client.PostAsync("https://connect4core.azurewebsites.net/api/MakeMove", content); 
        }

        private static async Task<GameStateResponse> GetGameState(string teamID)
        {
            using var client = new HttpClient();
            
            var content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.GetAsync($"https://connect4core.azurewebsites.net/api/GameState/{teamID}" );

            var res = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<GameStateResponse>(await response.Content.ReadAsStringAsync());
        }

        private static async Task StartGame(string teamId)
        {
            using var client = new HttpClient();
            var body = new Dictionary<string, string>
            {
                {"playerID", teamId},
            };
            
            var content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            
            await client.PostAsync("https://connect4core.azurewebsites.net/api/NewGame", content);
        }

        private static async Task<string> Register()
        {
            //c0c85ae2-af92-4a45-812c-2b1d9597846c
            using var client = new HttpClient();
            var credentials = new Dictionary<string, string>
            {
                {"teamName", "TeamAwesome"},
                {"password", "password"},
            };

            var content = new StringContent(JsonSerializer.Serialize(credentials), System.Text.Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync("https://connect4core.azurewebsites.net/api/register", content);
            
            return await response.Content.ReadAsStringAsync();
        }
    }
    
    public enum Cell
    {
        Empty = 0,
        Red = 1,
        Yellow = 2
    }

    public enum GameState
    {
        GameNotStarted = 0,
        RedWon = 1,
        YellWon = 2,
        RedToPlay = 3,
        YellowToPlay = 4,
        Draw = 5
    }

    
    public class GameStateResponse
    {
        public GameState CurrentState { get; set; }
        public Cell[,] Cells; 
        public Guid YellowPlayerID { get; set; }
        public Guid RedPlayerID { get; set; }
        public Guid ID { get; set; }
    }
}
