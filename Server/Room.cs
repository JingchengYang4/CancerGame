using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Threading.Tasks;
using SignalRServer.Payloads;
using SignalRServer.Hubs;
using System.Linq;

namespace SignalRServer
{
	public class Room
	{
        IHubContext<MainHub> hub;

        public GameManager manager;

        public Room(IHubContext<MainHub> _hub, GameManager _manager)
        {
            stat = new GameStat()
            {
                cancer = 2f,
                economy = 5f,
                emotion = 5f,
                health = 5f
            };

            hub = _hub;
            manager = _manager;
        }

        public GameStat stat;

        public TurnPayload currentPlayer = new TurnPayload();

        public bool gameStarted = false;
        public int counter = 0;
        public int side = 0;

        public int stage = 0;

        public List<List<string>> players = new List<List<string>>() { new List<string>(), new List<string>() };
        public HashSet<string> cancerPlayers = new HashSet<string>();
        public HashSet<string> patientPlayers = new HashSet<string>();

        public List<string> allPlayers = new List<string>();

        public async void Join(JoinRequestPayload request)
        {
            int side = 0;
            if (players[0].Count <= players[1].Count)
            {
                players[0].Add(request.connectionID);
                patientPlayers.Add(request.connectionID);
            }
            else
            {
                players[1].Add(request.connectionID);
                cancerPlayers.Add(request.connectionID);
                side = 1;
            }

            if (!gameStarted)
            {
                if (players[0].Count * players[1].Count > 0) StartGame();
            }

            allPlayers.Add(request.connectionID);

            //await Clients.AllExcept(request.connectionID).SendAsync("Join", json);
            await UpdateTeamStat();
            await hub.Clients.Client(request.connectionID).SendAsync("Side", side.ToString());
            await hub.Clients.Client(request.connectionID).SendAsync("Stage", stage.ToString());
            await hub.Clients.Client(request.connectionID).SendAsync("Stat", JsonConvert.SerializeObject(stage));
            await hub.Clients.Client(request.connectionID).SendAsync("Turn", JsonConvert.SerializeObject(currentPlayer));
        }
        public async Task UpdateTeamStat()
        {
            //await hub.Clients.Clients(allPlayers).SendAsync("Team", JsonConvert.SerializeObject(new TeamStatus() { cancerCount = players[1].Count, patientCount = players[0].Count }));
            var stat = new TeamStatus();
            for(int i = 0; i < cancerPlayers.Count; i++)
            {
                if (manager.playerNames.ContainsKey(cancerPlayers.ElementAt(i)))
                {
                    stat.cancerPlayers.Add(manager.playerNames[cancerPlayers.ElementAt(i)]);
                }
                else
                {
                    stat.cancerPlayers.Add($"Cancer Player {i + 1}");
                }
            }

            for (int i = 0; i < patientPlayers.Count; i++)
            {

                if (manager.playerNames.ContainsKey(patientPlayers.ElementAt(i)))
                {
                    stat.patientPlayers.Add(manager.playerNames[patientPlayers.ElementAt(i)]);
                }
                else
                {
                    stat.patientPlayers.Add($"Patient Player {i + 1}");
                }
            }

            await hub.Clients.Clients(allPlayers).SendAsync("Team", JsonConvert.SerializeObject(stat));
        }

        public void StartGame()
        {
            Console.WriteLine("Game begins/resumes");
            gameStarted = true;
            NextTurn();
        }

        public async void NextTurn()
        {
            bool success = false;

            if (players[0].Count * players[1].Count <= 0)
            {
                gameStarted = false;
                Console.WriteLine("Game stopped because of insufficient players");
                return;
            }

            while (!success)
            {
                if (counter < players[side].Count)
                {
                    //your turn
                    currentPlayer = new TurnPayload() { connectionID = players[side][counter], counter = counter, side = side };
                    await hub.Clients.All.SendAsync("Turn", JsonConvert.SerializeObject(currentPlayer));
                    counter++;
                    success = true;
                }
                else
                {
                    if (side == 0) side = 1;
                    else side = 0;
                    counter = 0;
                }
            }
        }

        public void Skip(string connectionId)
        {
            if (connectionId == currentPlayer.connectionID)
            {
                NextTurn();
            }
        }

        public void ProcessCard(CardMeta card)
        {
            stat.health += card.health;
            stat.emotion += card.emotion;
            stat.economy += card.economy;
            stat.cancer += card.cancer;

            stat.health = float.Clamp(stat.health, 0, 10);
            stat.emotion = float.Clamp(stat.emotion, 0, 10);
            stat.economy = float.Clamp(stat.economy, 0, 10);
            stat.cancer = float.Clamp(stat.cancer, 0, 5);
        }

        public async Task<bool> EvaluateStat()
        {
            if (stat.cancer > 2 && stage == 0)
            {
                stage = 1;
                await hub.Clients.Clients(allPlayers).SendAsync("Stage", stage.ToString());
            }
            else if (stat.cancer <= 0 && stage == 1)
            {
                stage = 2;
                await hub.Clients.Clients(allPlayers).SendAsync("Stage", stage.ToString());
            }

            if (stat.cancer >= 5 && stat.health == 0 || stat.cancer >= 5 && stat.emotion == 0)
            {
                //cancerWin
                await hub.Clients.Clients(allPlayers).SendAsync("Win", "Cancer");
                Reset();
                return true;
            }
            else if (stat.health >= 10 && stat.emotion >= 5 || stat.health >= 10 && stat.economy > 5)
            {
                //patientWin
                await hub.Clients.Clients(allPlayers).SendAsync("Win", "Patient");
                Reset();
                return true;
            }
            return false;
        }

        public async Task ChangeState(string state)
        {
            stage = int.Parse(state);
            await hub.Clients.Clients(allPlayers).SendAsync("Stage", stage.ToString());
        }

        public async void Reset()
        {
            await Task.Delay(5000);
            stat = new GameStat()
            {
                cancer = 2f,
                economy = 5f,
                emotion = 5f,
                health = 5f
            };
            await hub.Clients.Clients(allPlayers).SendAsync("Stat", JsonConvert.SerializeObject(stat));
            await hub.Clients.Clients(allPlayers).SendAsync("Reset", "");
        }


        public async void ChooseCard(string connectionID, CardMeta meta)
        {
            if (connectionID == currentPlayer.connectionID)
            {
                ProcessCard(meta);

                await hub.Clients.Clients(allPlayers).SendAsync("Stat", JsonConvert.SerializeObject(stat));

                var allExcept = allPlayers.Where(x => x != connectionID);
                await hub.Clients.Clients(allExcept).SendAsync("Card", meta.index.ToString());

                if (await EvaluateStat()) return;

                NextTurn();
            }
        }

        public async void Disconnect(string connectionID)
        {
            if (patientPlayers.Contains(connectionID))
            {
                players[0].Remove(connectionID);
                patientPlayers.Remove(connectionID);
            }
            else
            {
                players[1].Remove(connectionID);
                cancerPlayers.Remove(connectionID);
            }

            if (connectionID == currentPlayer.connectionID)
            {
                NextTurn();
            }

            allPlayers.Remove(connectionID);

            await UpdateTeamStat();
        }
    }
}

