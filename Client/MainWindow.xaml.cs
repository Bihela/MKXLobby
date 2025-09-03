using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataBase;
using Server;
using System.IO;

namespace Client
{
    public partial class MainWindow : Window
    {
        private ILobbyService client;
        private string currentRoom;
        private ChannelFactory<ILobbyService> channelFactory;

        public MainWindow()
        {
            InitializeComponent();
            InitializeClient();
            LoadAvailableRooms();
            RoomListBox.IsEnabled = false;
            JoinRoomButton.IsEnabled = false;
            CreateRoomBut.IsEnabled = false;
        }

        private void InitializeClient()
        {
            try
            {
                // Configure NetTcpBinding to match server
                var binding = new NetTcpBinding
                {
                    MaxReceivedMessageSize = 10485760, // 10 MB
                    MaxBufferSize = 10485760,         // 10 MB
                    MaxBufferPoolSize = 10485760,     // 10 MB
                    ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
                    {
                        MaxArrayLength = 10485760,    // 10 MB for arrays
                        MaxStringContentLength = 10485760 // 10 MB for strings
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
                RefreshFilesButton_Click(sender, e);
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
                    RefreshFilesButton_Click(sender, e);
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
                PrivateMessageRecipientComboBox.ItemsSource = playersInRoom.Select(p => p.Username).ToList();
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
                UsernameTextBox.IsEnabled = true;
                LoginButn.IsEnabled = true;
                RoomListBox.IsEnabled = false;
                JoinRoomButton.IsEnabled = false;
                CreateRoomBut.IsEnabled = false;
                currentRoom = null;
                RoomMessagesListBox.ItemsSource = null;
                PlayerListBox.ItemsSource = null;
                PrivateMessagesListBox.ItemsSource = null;
                SharedFilesListBox.ItemsSource = null;
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
                    currentRoom = null;
                    LoadAvailableRooms();
                    logOutbtn.IsEnabled = true;
                    JoinRoomButton.IsEnabled = true;
                    CreateRoomBut.IsEnabled = true;
                    RoomMessagesListBox.ItemsSource = null;
                    PlayerListBox.ItemsSource = null;
                    PrivateMessagesListBox.ItemsSource = null;
                    SharedFilesListBox.ItemsSource = null;
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
                    RefreshRoomMessagesButton_Click(sender, e);
                }, "Error sending room message");
            }
        }

        private void RefreshRoomMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                ExecuteWithFaultHandling(() =>
                {
                    List<string> messages = client.GetMessages(currentRoom);
                    RoomMessagesListBox.ItemsSource = messages;
                    if (messages.Count > 0)
                    {
                        RoomMessagesListBox.ScrollIntoView(RoomMessagesListBox.Items[RoomMessagesListBox.Items.Count - 1]);
                    }
                }, "Error refreshing room messages");
            }
        }

        private void SendPrivateMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var message = PrivateMessageTextBox.Text;
            var recipientUsername = PrivateMessageRecipientComboBox.SelectedItem?.ToString();
            var senderUsername = UsernameTextBox.Text;
            if (recipientUsername == senderUsername)
            {
                MessageBox.Show("You cannot send a private message to yourself.");
                return;
            }
            if (!string.IsNullOrEmpty(recipientUsername) && !string.IsNullOrEmpty(message))
            {
                ExecuteWithFaultHandling(() =>
                {
                    client.SendPrivateMessage(senderUsername, recipientUsername, message);
                    PrivateMessageTextBox.Clear();
                    RefreshPrivateMessagesButton_Click(sender, e);
                }, "Error sending private message");
            }
        }

        private void RefreshPrivateMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteWithFaultHandling(() =>
            {
                var username = UsernameTextBox.Text;
                List<string> privateMessages = client.GetPrivateMessages(username);
                PrivateMessagesListBox.ItemsSource = privateMessages;
                if (privateMessages.Count > 0)
                {
                    PrivateMessagesListBox.ScrollIntoView(PrivateMessagesListBox.Items[PrivateMessagesListBox.Items.Count - 1]);
                }
            }, "Error refreshing private messages");
        }

        private void refreshPRoom_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                LoadPlayersInRoom(currentRoom);
                RefreshFilesButton_Click(sender, e);
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
                        RefreshFilesButton_Click(sender, e);
                        MessageBox.Show($"File {dialog.SafeFileName} shared successfully");
                    }, "Error sharing file");
                }
            }
            else
            {
                MessageBox.Show("You must be in a room to share files.");
            }
        }

        private void RefreshFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentRoom))
            {
                ExecuteWithFaultHandling(() =>
                {
                    var fileNames = client.GetFileNames(currentRoom);
                    SharedFilesListBox.ItemsSource = fileNames;
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
    }
}