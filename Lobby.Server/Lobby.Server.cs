using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Lobby.Contracts;

namespace Lobby.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class GamingLobbyService : IGamingLobbyService
    {
        private readonly Dictionary<string, Player> _players = new Dictionary<string, Player>();
        private readonly Dictionary<string, LobbyRoom> _rooms = new Dictionary<string, LobbyRoom>();
        private readonly List<Message> _messages = new List<Message>();
        private readonly List<SharedFile> _sharedFiles = new List<SharedFile>();
        private readonly Dictionary<string, IGamingLobbyCallback> _callbacks = new Dictionary<string, IGamingLobbyCallback>();
        private readonly object _lock = new object();

        public bool Login(string username)
        {
            lock (_lock)
            {
                if (_players.ContainsKey(username))
                    return false; // Username already exists

                var player = new Player { Username = username };
                _players.Add(username, player);
                _callbacks.Add(username, OperationContext.Current.GetCallbackChannel<IGamingLobbyCallback>());
                return true;
            }
        }

        public void Logout(string username)
        {
            lock (_lock)
            {
                var room = _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.Username == username));
                if (room != null)
                {
                    LeaveRoom(room.RoomName, username);
                }

                _players.Remove(username);
                _callbacks.Remove(username);
            }
        }

        public List<LobbyRoom> GetAvailableRooms()
        {
            lock (_lock)
            {
                return _rooms.Values.ToList();
            }
        }

        public bool CreateRoom(string roomName, string username)
        {
            lock (_lock)
            {
                if (_rooms.ContainsKey(roomName))
                    return false; // Room already exists

                var room = new LobbyRoom { RoomName = roomName };
                room.Players.Add(_players[username]);
                _rooms.Add(roomName, room);

                // Notify all clients (duplex)
                foreach (var callback in _callbacks.Values)
                {
                    try
                    {
                        callback.OnRoomCreated(room);
                    }
                    catch
                    {
                        // Handle client disconnect
                    }
                }
                return true;
            }
        }

        public bool JoinRoom(string roomName, string username)
        {
            lock (_lock)
            {
                if (!_rooms.ContainsKey(roomName) || !_players.ContainsKey(username))
                    return false;

                var room = _rooms[roomName];
                if (room.Players.Any(p => p.Username == username))
                    return false; // Player already in room

                room.Players.Add(_players[username]);

                // Notify all clients in the room (duplex)
                foreach (var player in room.Players)
                {
                    if (_callbacks.ContainsKey(player.Username))
                    {
                        try
                        {
                            _callbacks[player.Username].OnPlayerJoined(_players[username], roomName);
                        }
                        catch
                        {
                            // Handle client disconnect
                        }
                    }
                }
                return true;
            }
        }

        public bool LeaveRoom(string roomName, string username)
        {
            lock (_lock)
            {
                if (!_rooms.ContainsKey(roomName) || !_players.ContainsKey(username))
                    return false;

                var room = _rooms[roomName];
                var player = room.Players.FirstOrDefault(p => p.Username == username);
                if (player == null)
                    return false;

                room.Players.Remove(player);

                // Notify all clients in the room (duplex)
                foreach (var p in room.Players)
                {
                    if (_callbacks.ContainsKey(p.Username))
                    {
                        try
                        {
                            _callbacks[p.Username].OnPlayerLeft(player, roomName);
                        }
                        catch
                        {
                            // Handle client disconnect
                        }
                    }
                }

                // Remove room if empty
                if (!room.Players.Any())
                {
                    _rooms.Remove(roomName);
                    foreach (var callback in _callbacks.Values)
                    {
                        try
                        {
                            callback.OnRoomDeleted(roomName);
                        }
                        catch
                        {
                            // Handle client disconnect
                        }
                    }
                }
                return true;
            }
        }

        public void SendMessage(string roomName, string username, string content)
        {
            lock (_lock)
            {
                if (!_rooms.ContainsKey(roomName) || !_players.ContainsKey(username))
                    return;

                var message = new Message
                {
                    Sender = username,
                    Content = content,
                    RoomName = roomName,
                    IsPrivate = false
                };
                _messages.Add(message);

                // Notify all clients in the room (duplex)
                var room = _rooms[roomName];
                foreach (var player in room.Players)
                {
                    if (_callbacks.ContainsKey(player.Username))
                    {
                        try
                        {
                            _callbacks[player.Username].OnMessageReceived(message);
                        }
                        catch
                        {
                            // Handle client disconnect
                        }
                    }
                }
            }
        }

        public void SendPrivateMessage(string sender, string recipient, string content)
        {
            lock (_lock)
            {
                if (!_players.ContainsKey(sender) || !_players.ContainsKey(recipient))
                    return;

                var message = new Message
                {
                    Sender = sender,
                    Content = content,
                    RoomName = null,
                    IsPrivate = true,
                    Recipient = recipient
                };
                _messages.Add(message);

                // Notify recipient (duplex)
                if (_callbacks.ContainsKey(recipient))
                {
                    try
                    {
                        _callbacks[recipient].OnMessageReceived(message);
                    }
                    catch
                    {
                        // Handle client disconnect
                    }
                }
            }
        }

        public void ShareFile(string roomName, string username, string fileName, byte[] fileContent)
        {
            lock (_lock)
            {
                if (!_rooms.ContainsKey(roomName) || !_players.ContainsKey(username))
                    return;

                var file = new SharedFile
                {
                    FileName = fileName,
                    FileContent = fileContent,
                    RoomName = roomName,
                    Sender = username
                };
                _sharedFiles.Add(file);

                // Notify all clients in the room (duplex)
                var room = _rooms[roomName];
                foreach (var player in room.Players)
                {
                    if (_callbacks.ContainsKey(player.Username))
                    {
                        try
                        {
                            _callbacks[player.Username].OnFileShared(file);
                        }
                        catch
                        {
                            // Handle client disconnect
                        }
                    }
                }
            }
        }

        public List<Message> GetMessages(string roomName)
        {
            lock (_lock)
            {
                return _messages.Where(m => m.RoomName == roomName && !m.IsPrivate).ToList();
            }
        }

        public List<SharedFile> GetSharedFiles(string roomName)
        {
            lock (_lock)
            {
                return _sharedFiles.Where(f => f.RoomName == roomName).ToList();
            }
        }

        public List<Player> GetPlayersInRoom(string roomName)
        {
            lock (_lock)
            {
                return _rooms.ContainsKey(roomName) ? _rooms[roomName].Players.ToList() : new List<Player>();
            }
        }
    }
}