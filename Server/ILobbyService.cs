using DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    [ServiceContract]
    public interface ILobbyService
    {
        [OperationContract]
        bool Login(string username);

        [OperationContract]
        bool Logout(string username);

        [OperationContract]
        List<Room> GetAvailableRooms();

        [OperationContract]
        bool CreateRoom(string roomName, string creatorUsername);

        [OperationContract]
        bool JoinRoom(string roomName, string username);

        [OperationContract]
        bool LeaveRoom(string roomName, string username);

        [OperationContract]
        List<Player> GetPlayersInRoom(string roomName);

        [OperationContract]
        void BroadcastMessage(string roomName, string username, string message);

        [OperationContract]
        List<string> GetMessages(string roomName);

        [OperationContract]
        void SendPrivateMessage(string senderUsername, string receiverUsername, string message);

        [OperationContract]
        List<string> GetPrivateMessages(string username);

        //file sharing
        [OperationContract]
        void UploadFile(string roomName, string username, byte[] fileData, string fileName);

        [OperationContract]
        List<string> GetFileNames(string roomName);

        [OperationContract]
        byte[] DownloadFile(string roomName, string fileName);
    }
}