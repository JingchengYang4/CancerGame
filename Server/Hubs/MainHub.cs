using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
using SignalRServer.Payloads;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SignalRServer.Hubs
{
    public class MainHub : Hub
    {
        GameManager manager;

        //Dictionary<string, GameManager> rooms = new dic

        string connectionID;

        public MainHub(GameManager _manager)
        {
            manager = _manager;
        }

        public override Task OnConnectedAsync()
        {
            connectionID = Context.ConnectionId;
            Console.WriteLine($"Connected: {Context.ConnectionId}");
            manager.Connected(connectionID);
            return base.OnConnectedAsync();
        }

        public async Task Message(string payload)
        {
            var data = JsonConvert.DeserializeObject<dynamic>(payload);
            string json = JsonConvert.SerializeObject(data);
            await Clients.All.SendAsync("Message", json);
        }

        //the current player chooses a card
        public async Task Card(string payload)
        {
            var card = JsonConvert.DeserializeObject<CardMeta>(payload);
            manager.ChooseCard(Context.ConnectionId, card);
        }

        //the current player skips this round
        public async Task Skip(string connectionID)
        {
            manager.Skip(connectionID);
        }

        //change name of player
        public async Task SetName(string name)
        {
            manager.ChangeName(Context.ConnectionId, name);
        }

        public async Task SetState(string state)
        {
            manager.ChangeState(Context.ConnectionId, state);
        }

        public async Task EndGame(string room)
        {
            manager.EndGame(room);
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"Disconnected: {Context.ConnectionId}");
            manager.Disconnect(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task Join(string payload)
        {
            var request = JsonConvert.DeserializeObject<JoinRequestPayload>(payload);

            manager.Join(request);
        }
    }
}
