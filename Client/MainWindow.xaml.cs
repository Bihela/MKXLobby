using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using DataBase;
using Server;

namespace Client
{
    public partial class MainWindow : Window
    {
        private ILobbyService client;
        private string currentRoom;
        private ChannelFactory<ILobbyService> channelFactory;
        private DispatcherTimer pollingTimer;
        private Dictionary<string, PrivateChatWindow> privateChatWindows;
        private Dictionary<string, int> lastMessageCount; 
        private HashSet<string> recentlyClosedWindows; 

        public MainWindow()
        {
            InitializeComponent();
            InitializeClient();
            InitializePolling();
            privateChatWindows = new Dictionary<string, PrivateChatWindow>();
            lastMessageCount = new Dictionary<string, int>(); 
            recentlyClosedWindows = new HashSet<string>();
            LoadAvailableRooms();
            RoomListBox.IsEnabled = false;
            JoinRoomButton.IsEnabled = false;
            CreateRoomBut.IsEnabled = false;
        }

        private void InitializeClient()
        {
            try
            {
                var binding = new NetTcpBinding
                {
                    MaxReceivedMessageSize = 10485760,
                    MaxBufferSize = 10485760,
                    MaxBufferPoolSize = 10485760,
                    ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
                    {
                        MaxArrayLength = 10485760,
                        MaxStringContentLength = 10485760
                    }
                };
                binding.Security.Mode = SecurityMode.None;
                var endpoint = new EndpointAddress("net.tcp://localhost:8100/Service");
                channelFactory = new ChannelFactory<ILobbyService>(binding, endpoint);
                client = channelFactory.CreateChannel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing client: {ex.Message}");
            }
        }

        private void InitializePolling()
        {
            pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            pollingTimer.Tick += PollingTimer_Tick;
        }

        private void PollingTimer_Tick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentRoom))
            {
                LoadAvailableRooms();
            }
            else
            {
                LoadPlayersInRoom(currentRoom);
                UpdateRoomMessages();
                UpdateSharedFiles();
                UpdatePrivateMessages();
            }
        }

        private void RecreateClient()
        {
            try
            {
                if (client != null)
                {
                    ((IClientChannel)client).Close();
                    ((IClientChannel)client).Dispose();
                }
                if (channelFactory != null)
                {
                    channelFactory.Close();
                }
                InitializeClient();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recreating client: {ex.Message}");
            }
        }

        private T ExecuteWithFaultHandling<T>(Func<T> action, string errorMessage)
        {
            try
            {
                return action();
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}");
                RecreateClient();
                return default(T);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}");
                return default(T);
            }
        }

        private void ExecuteWithFaultHandling(Action action, string errorMessage)
        {
            try
            {
                action();
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}");
                RecreateClient();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}");
            }
        }

        private void LoadAvailableRooms()
        {
            ExecuteWithFaultHandling(() =>
            {
                List<Room> availableRooms = client.GetAvailableRooms();
                RoomListBox.ItemsSource = availableRooms;
                RoomListBox.DisplayMemberPath = "RoomName";
            }, "Error loading rooms");
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            bool success = ExecuteWithFaultHandling(() => client.Login(username), "Error logging in");
            if (success)
            {
                MessageBox.Show("Login successful");
                RoomListBox.IsEnabled = true;
                JoinRoomButton.IsEnabled = true;
                CreateRoomBut.IsEnabled = true;
                UsernameTextBox.IsEnabled = false;
                LoginButn.IsEnabled = false;
                pollingTimer.Start();
                LoadAvailableRooms();
            }
            else
            {
                MessageBox.Show("Username already exists");
            }
        }

        private void CreateRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var roomName = RoomNameTextBox.Text;
            var username = UsernameTextBox.Text;
            bool success = ExecuteWithFaultHandling(() => client.CreateRoom(roomName, username), "Error creating room");
            if (success)
            {
                MessageBox.Show("Room created and joined");
                currentRoom = roomName;
                LoadAvailableRooms();
                JoinRoomButton.IsEnabled = false;
                logOutbtn.IsEnabled = false;
                CreateRoomBut.IsEnabled = false;
                LoadPlayersInRoom(roomName);
                UpdateSharedFiles();
                UpdateRoomMessages();
            }
            else
            {
                MessageBox.Show("Room name already exists or failed to create");
            }
        }

        private void JoinRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRoom = RoomListBox.SelectedItem as Room;
            var username = UsernameTextBox.Text;
            if (selectedRoom != null)
            {
                bool success = ExecuteWithFaultHandling(() => client.JoinRoom(selectedRoom.RoomName, username), "Error joining room");
                if (success)
                {
                    MessageBox.Show("Joined room");
                    currentRoom = selectedRoom.RoomName;
                    JoinRoomButton.IsEnabled = false;
                    logOutbtn.IsEnabled = false;
                    CreateRoomBut.IsEnabled = false;
                    LoadPlayersInRoom(selectedRoom.RoomName);
                    UpdateSharedFiles();
                    UpdateRoomMessages();
                }
                else
                {
                    MessageBox.Show("Failed to join room");
                }
            }
        }

        private void LoadPlayersInRoom(string roomName)
        {
            ExecuteWithFaultHandling(() =>
            {
                List<Player> playersInRoom = client.GetPlayersInRoom(roomName);
                PlayerListBox.ItemsSource = playersInRoom;
                PlayerListBox.DisplayMemberPath = "Username";
                PrivateMessageRecipientComboBox.ItemsSource = playersInRoom
                    .Where(p => p.Username != UsernameTextBox.Text)
                    .Select(p => p.Username)
                    .ToList();
            }, "Error loading players");
        }

        private void refreshList_Click(object sender, RoutedEventArgs e)
        {
            LoadAvailableRooms();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            bool success = ExecuteWithFaultHandling(() => client.Logout(username), "Error logging out");
            if (success)
            {
                MessageBox.Show("Logout successful");
                pollingTimer.Stop();
                foreach (var window in privateChatWindows.Values)
                {
                    window.Close();
                }
                privateChatWindows.Clear();
                recentlyClosedWindows.Clear();
                lastMessageCount.Clear(); 
                UsernameTextBox.IsEnabled = true;
                LoginButn.IsEnabled = true;
                RoomListBox.IsEnabled = false;
                JoinRoomButton.IsEnabled = false;
                CreateRoomBut.IsEnabled = false;
                currentRoom = null;
                ChatListBox.ItemsSource = null;
                PlayerListBox.ItemsSource = null;
            }
            else
            {
                MessageBox.Show("Username is not there");
            }
        }

        private void LeaveRoBtn_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            if (!string.IsNullOrEmpty(currentRoom))
            {
                bool success = ExecuteWithFaultHandling(() => client.LeaveRoom(currentRoom, username), "Error leaving room");
                if (success)
                {
                    MessageBox.Show("Left the room");
                    foreach (var window in privateChatWindows.Values)
                    {
                        window.Close();
                    }
                    privateChatWindows.Clear();
                    recentlyClosedWindows.Clear();
                    lastMessageCount.Clear(); 
                    currentRoom = null;
                    LoadAvailableRooms();
                    logOutbtn.IsEnabled = true;
                    JoinRoomButton.IsEnabled = true;
                    CreateRoomBut.IsEnabled = true;
                    ChatListBox.ItemsSource = null;
                    PlayerListBox.ItemsSource = null;
                }
                else
                {
                    MessageBox.Show("Failed to leave the room");
                }
            }
            else
            {
                MessageBox.Show("You are not in any room");
            }
        }

        private void SendRoomMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var message = RoomMessageTextBox.Text;
            var username = UsernameTextBox.Text;
            if (!string.IsNullOrEmpty(currentRoom) && !string.IsNullOrEmpty(message))
            {
                ExecuteWithFaultHandling(() =>
                {
                    client.BroadcastMessage(currentRoom, username, message);
                    RoomMessageTextBox.Clear();
                    UpdateRoomMessages();
                }, "Error sending room message");
            }
        }

        private void UpdateRoomMessages()
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                ExecuteWithFaultHandling(() =>
                {
                    UpdateUnifiedChat();
                }, "Error refreshing room messages");
            }
        }

        private void SendPrivateMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var recipientUsername = PrivateMessageRecipientComboBox.SelectedItem?.ToString();
            var senderUsername = UsernameTextBox.Text;
            if (recipientUsername == senderUsername)
            {
                MessageBox.Show("You cannot send a private message to yourself.");
                return;
            }
            if (!string.IsNullOrEmpty(recipientUsername))
            {
                if (!privateChatWindows.ContainsKey(recipientUsername))
                {
                    var chatWindow = new PrivateChatWindow(client, senderUsername, recipientUsername);
                    chatWindow.Closed += (s, args) =>
                    {
                        privateChatWindows.Remove(recipientUsername);
                        recentlyClosedWindows.Add(recipientUsername);
                        lastMessageCount[recipientUsername] = client.GetPrivateMessages(senderUsername)
                            .Count(m => m.StartsWith($"Private from {recipientUsername}:") || m.StartsWith($"Private to {recipientUsername}:"));
                        MessageBox.Show($"Private chat window with {recipientUsername} closed.");
                    };
                    privateChatWindows[recipientUsername] = chatWindow;
                    recentlyClosedWindows.Remove(recipientUsername);
                    lastMessageCount.Remove(recipientUsername); 
                    chatWindow.Show();
                }
                else
                {
                    privateChatWindows[recipientUsername].Activate();
                }
            }
        }

        private void UpdatePrivateMessages()
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                ExecuteWithFaultHandling(() =>
                {
                    var username = UsernameTextBox.Text;
                    var allMessages = client.GetPrivateMessages(username);
                    foreach (var recipient in privateChatWindows.Keys.ToList())
                    {
                        if (privateChatWindows[recipient].IsLoaded)
                        {
                            privateChatWindows[recipient].UpdateMessages();
                            lastMessageCount[recipient] = allMessages
                                .Count(m => m.StartsWith($"Private from {recipient}:") || m.StartsWith($"Private to {recipient}:"));
                        }
                        else
                        {
                            privateChatWindows.Remove(recipient);
                            recentlyClosedWindows.Add(recipient);
                            lastMessageCount[recipient] = allMessages
                                .Count(m => m.StartsWith($"Private from {recipient}:") || m.StartsWith($"Private to {recipient}:"));
                        }
                    }
                    var senders = allMessages
                        .Where(m => m.StartsWith("Private from"))
                        .Select(m => m.Split(':')[0].Replace("Private from ", "").Trim())
                        .Distinct()
                        .Where(s => s != username && !privateChatWindows.ContainsKey(s));
                    foreach (var sender in senders)
                    {
                        var players = client.GetPlayersInRoom(currentRoom);
                        if (players.Any(p => p.Username == sender))
                        {
                            var messageCount = allMessages
                                .Count(m => m.StartsWith($"Private from {sender}:") || m.StartsWith($"Private to {sender}:"));
                            if (!recentlyClosedWindows.Contains(sender) ||
                                (lastMessageCount.ContainsKey(sender) && messageCount > lastMessageCount[sender]))
                            {
                                if (!privateChatWindows.ContainsKey(sender))
                                {
                                    var chatWindow = new PrivateChatWindow(client, username, sender);
                                    chatWindow.Closed += (s, args) =>
                                    {
                                        privateChatWindows.Remove(sender);
                                        recentlyClosedWindows.Add(sender);
                                        lastMessageCount[sender] = allMessages
                                            .Count(m => m.StartsWith($"Private from {sender}:") || m.StartsWith($"Private to {sender}:"));
                                        MessageBox.Show($"Private chat window with {sender} closed.");
                                    };
                                    privateChatWindows[sender] = chatWindow;
                                    lastMessageCount[sender] = messageCount; 
                                    chatWindow.Show();
                                }
                            }
                        }
                    }
                }, "Error refreshing private messages");
            }
        }

        private void refreshPRoom_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                LoadPlayersInRoom(currentRoom);
                UpdateSharedFiles();
            }
        }

        private void ShareFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images|*.jpg;*.png|Text Files|*.txt"
                };
                if (dialog.ShowDialog() == true)
                {
                    ExecuteWithFaultHandling(() =>
                    {
                        var fileData = File.ReadAllBytes(dialog.FileName);
                        client.UploadFile(currentRoom, UsernameTextBox.Text, fileData, dialog.SafeFileName);
                        UpdateSharedFiles();
                        MessageBox.Show($"File {dialog.SafeFileName} shared successfully");
                    }, "Error sharing file");
                }
            }
            else
            {
                MessageBox.Show("You must be in a room to share files.");
            }
        }

        private void UpdateSharedFiles()
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                ExecuteWithFaultHandling(() =>
                {
                    UpdateUnifiedChat();
                }, "Error refreshing files");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                ExecuteWithFaultHandling(() =>
                {
                    var fileName = e.Uri.ToString();
                    var fileData = client.DownloadFile(currentRoom, fileName);
                    if (fileData != null)
                    {
                        var saveDialog = new Microsoft.Win32.SaveFileDialog
                        {
                            FileName = fileName,
                            Filter = "All Files|*.*"
                        };
                        if (saveDialog.ShowDialog() == true)
                        {
                            File.WriteAllBytes(saveDialog.FileName, fileData);
                            MessageBox.Show($"File {fileName} downloaded successfully");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"File {fileName} not found");
                    }
                }, "Error downloading file");
            }
        }

        private void UpdateUnifiedChat()
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                var messages = client.GetMessages(currentRoom);
                var fileNames = client.GetFileNames(currentRoom);
                var chatItems = new List<ChatItem>();
                chatItems.AddRange(messages.Select(m => new ChatItem { Type = "Message", Content = m }));
                chatItems.AddRange(fileNames.Select(f => new ChatItem { Type = "File", Content = $"File shared: {f}", FileName = f }));
                ChatListBox.ItemsSource = chatItems;
                if (chatItems.Count > 0)
                {
                    ChatListBox.ScrollIntoView(chatItems[chatItems.Count - 1]);
                }
            }
        }
    }

    public class ChatItem
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public string FileName { get; set; }
    }
}