using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Lobby.Contracts
{
    [DataContract]
    public class Player
    {
        [DataMember]
        public string Username { get; set; }
    }

    // Data contract for a Lobby Room
    [DataContract]
    public class LobbyRoom
    {
        [DataMember]
        public string RoomName { get; set; }

        [DataMember]
        public List<Player> Players { get; set; } = new List<Player>();
    }

    // Data contract for a Message
    [DataContract]
    public class Message
    {
        [DataMember]
        public string Sender { get; set; }

        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public string RoomName { get; set; }

        [DataMember]
        public bool IsPrivate { get; set; }

        [DataMember]
        public string Recipient { get; set; } // Used for private messages
    }

    // Data contract for a Shared File
    [DataContract]
    public class SharedFile
    {
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public byte[] FileContent { get; set; }

        [DataMember]
        public string RoomName { get; set; }

        [DataMember]
        public string Sender { get; set; }
    }

    // Callback contract for duplex communication
    [ServiceContract]
    public interface IGamingLobbyCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnPlayerJoined(Player player, string roomName);

        [OperationContract(IsOneWay = true)]
        void OnPlayerLeft(Player player, string roomName);

        [OperationContract(IsOneWay = true)]
        void OnRoomCreated(LobbyRoom room);

        [OperationContract(IsOneWay = true)]
        void OnRoomDeleted(string roomName);

        [OperationContract(IsOneWay = true)]
        void OnMessageReceived(Message message);

        [OperationContract(IsOneWay = true)]
        void OnFileShared(SharedFile file);
    }

    // Service contract for the gaming lobby
    [ServiceContract(CallbackContract = typeof(IGamingLobbyCallback))]
    public interface IGamingLobbyService
    {
        // User Management
        [OperationContract]
        bool Login(string username);

        [OperationContract]
        void Logout(string username);

        // Lobby Room Management
        [OperationContract]
        List<LobbyRoom> GetAvailableRooms();

        [OperationContract]
        bool CreateRoom(string roomName, string username);

        [OperationContract]
        bool JoinRoom(string roomName, string username);

        [OperationContract]
        bool LeaveRoom(string roomName, string username);

        // Message Distribution
        [OperationContract]
        void SendMessage(string roomName, string username, string content);

        [OperationContract]
        void SendPrivateMessage(string sender, string recipient, string content);

        // File Sharing
        [OperationContract]
        void ShareFile(string roomName, string username, string fileName, byte[] fileContent);

        // Get updates for polling clients
        [OperationContract]
        List<Message> GetMessages(string roomName);

        [OperationContract]
        List<SharedFile> GetSharedFiles(string roomName);

        [OperationContract]
        List<Player> GetPlayersInRoom(string roomName);
    }
}