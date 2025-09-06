using Server;
using System;
using System.ServiceModel;
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
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing PrivateChatWindow: {ex.Message}");
                return;
            }
            _client = client;
            _senderUsername = senderUsername;
            RecipientUsername = recipientUsername;
            Title = $"Private Chat with {RecipientUsername}";
            UpdateMessages();
            Closing += PrivateChatWindow_Closing;
        }

        private void PrivateChatWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during window closing: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}");
            }
        }

        public void UpdateMessages()
        {
            ExecuteWithFaultHandling(() =>
            {
                var allMessages = _client.GetPrivateMessages(_senderUsername);
                var filteredMessages = allMessages.FindAll(m =>
                    m.StartsWith($"Private from {RecipientUsername}:") ||
                    m.StartsWith($"Private to {RecipientUsername}:"));
                PrivateMessagesListBox.ItemsSource = filteredMessages;
                if (filteredMessages.Count > 0)
                {
                    PrivateMessagesListBox.ScrollIntoView(filteredMessages[filteredMessages.Count - 1]);
                }
            }, "Error updating private messages");
        }

        private void SendPrivateMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var message = PrivateMessageTextBox.Text;
            if (!string.IsNullOrEmpty(message) && message != "Type a message...")
            {
                ExecuteWithFaultHandling(() =>
                {
                    _client.SendPrivateMessage(_senderUsername, RecipientUsername, message);
                    PrivateMessageTextBox.Clear();
                    UpdateMessages();
                }, "Error sending private message");
            }
        }
    }
}