using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SignalRServer.Hubs;
using SignalRServer.Payloads;
using System.Linq;

namespace SignalRServer
{
	public class GameManager
	{
        IHubContext<MainHub> hub;

        Dictionary<string, Room> rooms = new Dictionary<string, Room>();

        public Dictionary<string, string> playerNames = new Dictionary<string, string>();

        //player to room
        public Dictionary<string, string> playerRooms = new Dictionary<string, string>();

        public GameManager(IHubContext<MainHub> _hub)
        {
            hub = _hub;
        }

        public void Connected(string connectionID)
        {
            List<string> roomIds = new List<string>();
            foreach(var room in rooms)
            {
                roomIds.Add($"{room.Key}: {room.Value.allPlayers.Count} Players");
            }
            string payload = JsonConvert.SerializeObject(roomIds);
            hub.Clients.Client(connectionID).SendAsync("Rooms", payload);
        }

        public void Join(JoinRequestPayload request)
        {
            if(!rooms.ContainsKey(request.roomID))
            {
                rooms.Add(request.roomID, new Room(hub, this, request.roomID));
            }

            playerRooms.Add(request.connectionID, request.roomID);
            rooms[request.roomID].Join(request);
        }

        public void Skip(string connectionId)
        {
            if(playerRooms.ContainsKey(connectionId))
            {
                rooms[playerRooms[connectionId]].Skip(connectionId);
            }
        }

        public void ChooseCard(string connectionId, CardMeta meta)
        {
            if (playerRooms.ContainsKey(connectionId))
            {
                rooms[playerRooms[connectionId]].ChooseCard(connectionId, meta);
            }
        }

        public void Disconnect(string connectionId)
        {
            if (playerRooms.ContainsKey(connectionId))
            {
                var room = rooms[playerRooms[connectionId]];
                room.Disconnect(connectionId);

                if(room.allPlayers.Count <= 0)
                {
                    rooms.Remove(playerRooms[connectionId]);
                }

                playerRooms.Remove(connectionId);
            }

            if(playerNames.ContainsKey(connectionId))
            {
                playerNames.Remove(connectionId);
            }
        }

        public async void ChangeName(string connectionId, string name)
        {
            if(playerNames.ContainsKey(connectionId))
            {
                playerNames[connectionId] = name;
            }
            else
            {
                playerNames.Add(connectionId, name);
            }

            if(playerRooms.ContainsKey(connectionId))
            {
                await rooms[playerRooms[connectionId]].UpdateTeamStat();
            }
        }

        public async void ChangeState(string connectionId, string state)
        {
            if (playerRooms.ContainsKey(connectionId))
            {
                await rooms[playerRooms[connectionId]].ChangeState(state);
            }
        }

        public async void EndGame(string room)
        {
            if(rooms.ContainsKey(room))
            {
                rooms[room].End();
            }
        }

        public async void CloseRoom(string roomId)
        {
            if (rooms.ContainsKey(roomId))
            {
                var players = rooms[roomId].allPlayers;

                foreach (var player in players)
                {
                    playerRooms.Remove(player);
                }
                await hub.Clients.Clients(players).SendAsync("Close", "");

                rooms.Remove(roomId);

                List<string> roomIds = new List<string>();
                foreach (var room in rooms)
                {
                    roomIds.Add($"{room.Key}: {room.Value.allPlayers.Count} Players");
                }
                string payload = JsonConvert.SerializeObject(roomIds);
                await hub.Clients.Clients(players).SendAsync("Rooms", payload);
            }
        }
    }
}

