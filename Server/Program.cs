using System;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hey, welcome to the Mortal Kombat X Lobby Server!");
            ServiceHost host = null;
            try
            {
                // Configure NetTcpBinding with increased limits
                NetTcpBinding tcp = new NetTcpBinding
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
                tcp.Security.Mode = SecurityMode.None; // Simplify for local testing

                host = new ServiceHost(typeof(LobbyService));
                host.AddServiceEndpoint(typeof(ILobbyService), tcp, "net.tcp://0.0.0.0:8100/Service");
                host.Open();
                Console.WriteLine("System Online");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                if (host != null && host.State != CommunicationState.Closed)
                {
                    host.Close();
                }
            }
        }
    }
}