using System.Windows;
using System.Windows.Controls;

namespace Client
{
    public class ChatItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MessageTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChatItem chatItem)
            {
                return chatItem.Type == "Message" ? MessageTemplate : FileTemplate;
            }
            return null;
        }
    }
}