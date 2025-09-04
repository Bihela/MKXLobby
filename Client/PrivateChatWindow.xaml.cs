using Server;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Client
{
    public partial class PrivateChatWindow : Window
    {
        private readonly ILobbyService _client;
        private readonly string _senderUsername;
        public string RecipientUsername { get; }

        public PrivateChatWindow(ILobbyService client, string senderUsername, string recipientUsername)
        {
            InitializeComponent();
            _client = client;
            _senderUsername = senderUsername;
            RecipientUsername = recipientUsername;
            Title = $"Private Chat with {RecipientUsername}"; // Set window title
            UpdateMessages(); // Initial load
        }

        // Update messages for this recipient
        public void UpdateMessages()
        {
            try
            {
                var allMessages = _client.GetPrivateMessages(_senderUsername);
                // Filter messages for this specific recipient
                var filteredMessages = allMessages.FindAll(m =>
                    m.StartsWith($"Private from {RecipientUsername}:") ||
                    m.StartsWith($"Private to {RecipientUsername}:"));
                PrivateMessagesListBox.ItemsSource = filteredMessages;
                if (filteredMessages.Count > 0)
                {
                    PrivateMessagesListBox.ScrollIntoView(filteredMessages[filteredMessages.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating private messages: {ex.Message}");
            }
        }

        private void SendPrivateMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var message = PrivateMessageTextBox.Text;
            if (!string.IsNullOrEmpty(message) && message != "Type a message...")
            {
                try
                {
                    _client.SendPrivateMessage(_senderUsername, RecipientUsername, message);
                    PrivateMessageTextBox.Clear();
                    UpdateMessages();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending private message: {ex.Message}");
                }
            }
        }
    }
}