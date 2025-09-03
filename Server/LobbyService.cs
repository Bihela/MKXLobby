using DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class LobbyService : ILobbyService
    {
        private static List<Player> players = new List<Player>();
        private static List<Room> rooms = new List<Room>();
        private static Dictionary<string, List<string>> privateMessages = new Dictionary<string, List<string>>();
        // Store files per room: roomName -> List<(fileName, fileData)>
        private static Dictionary<string, List<(string FileName, byte[] FileData)>> roomFiles = new Dictionary<string, List<(string, byte[])>>();

        public bool Login(string username)
        {
            if (players.Any(p => p.Username == username))
            {
                return false; // Username already exists
            }
            players.Add(new Player { Username = username });
            return true;
        }

        public bool Logout(string username)
        {
            var player = players.FirstOrDefault(p => p.Username == username);
            if (player != null)
            {
                players.Remove(player);
                foreach (var room in rooms)
                {
                    room.Players.Remove(player);
                }
                return true;
            }
            return false;
        }

        public List<Room> GetAvailableRooms()
        {
            return rooms;
        }

        public bool CreateRoom(string roomName, string creatorUsername)
        {
            if (rooms.Any(r => r.RoomName == roomName))
            {
                return false;
            }
            var creator = players.FirstOrDefault(p => p.Username == creatorUsername);
            if (creator != null)
            {
                var newRoom = new Room { RoomName = roomName };
                newRoom.Players.Add(creator);
                rooms.Add(newRoom);
                return true;
            }
            return false;
        }

        public bool JoinRoom(string roomName, string username)
        {
            var room = rooms.FirstOrDefault(r => r.RoomName == roomName);
            var player = players.FirstOrDefault(p => p.Username == username);

            if (room != null && player != null && !room.Players.Contains(player))
            {
                room.Players.Add(player);
                return true;
            }
            return false;
        }

        public bool LeaveRoom(string roomName, string username)
        {
            var room = rooms.FirstOrDefault(r => r.RoomName == roomName);
            if (room != null)
            {
                var player = room.Players.FirstOrDefault(p => p.Username == username);
                if (player != null)
                {
                    room.Players.Remove(player);
                    if (room.Players.Count == 0)
                    {
                        rooms.Remove(room);
                        roomFiles.Remove(roomName); // Clean up files when room is empty
                    }
                    return true;
                }
            }
            return false;
        }

        public List<Player> GetPlayersInRoom(string roomName)
        {
            var room = rooms.FirstOrDefault(r => r.RoomName == roomName);
            return room?.Players ?? new List<Player>();
        }

        public void BroadcastMessage(string roomName, string username, string message)
        {
            var room = rooms.FirstOrDefault(r => r.RoomName == roomName);
            if (room != null)
            {
                string fullMessage = $"{username}: {message}";
                room.Messages.Add(fullMessage);
                Console.WriteLine(fullMessage);
            }
        }

        public List<string> GetMessages(string roomName)
        {
            var room = rooms.FirstOrDefault(r => r.RoomName == roomName);
            return room?.Messages ?? new List<string>();
        }

        public void SendPrivateMessage(string senderUsername, string receiverUsername, string message)
        {
            var sender = players.FirstOrDefault(p => p.Username == senderUsername);
            var receiver = players.FirstOrDefault(p => p.Username == receiverUsername);
            var room = rooms.FirstOrDefault(r => r.Players.Contains(sender) && r.Players.Contains(receiver));
            if (room != null && sender != null && receiver != null)
            {
                string fullMessageToReceiver = $"Private from {senderUsername}: {message}";
                string fullMessageToSender = $"Private to {receiverUsername}: {message}";
                if (!privateMessages.ContainsKey(receiverUsername))
                {
                    privateMessages[receiverUsername] = new List<string>();
                }
                privateMessages[receiverUsername].Add(fullMessageToReceiver);
                if (!privateMessages.ContainsKey(senderUsername))
                {
                    privateMessages[senderUsername] = new List<string>();
                }
                privateMessages[senderUsername].Add(fullMessageToSender);
            }
        }

        public List<string> GetPrivateMessages(string username)
        {
            return privateMessages.ContainsKey(username) ? privateMessages[username] : new List<string>();
        }

        // File sharing methods
        public void UploadFile(string roomName, string username, byte[] fileData, string fileName)
        {
            var room = rooms.FirstOrDefault(r => r.RoomName == roomName);
            if (room != null && room.Players.Any(p => p.Username == username))
            {
                if (!roomFiles.ContainsKey(roomName))
                {
                    roomFiles[roomName] = new List<(string, byte[])>();
                }
                roomFiles[roomName].Add((fileName, fileData));
                Console.WriteLine($"{username} uploaded {fileName} to {roomName}");
            }
        }

        public List<string> GetFileNames(string roomName)
        {
            return roomFiles.ContainsKey(roomName) ? roomFiles[roomName].Select(f => f.FileName).ToList() : new List<string>();
        }

        public byte[] DownloadFile(string roomName, string fileName)
        {
            if (roomFiles.ContainsKey(roomName))
            {
                var file = roomFiles[roomName].FirstOrDefault(f => f.FileName == fileName);
                return file.FileData;
            }
            return null;
        }
    }
}