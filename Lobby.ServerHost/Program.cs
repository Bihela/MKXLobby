using System;
using System.ServiceModel;
using Lobby.Server;

namespace Lobby.ServerHost
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;
            try
            {
                // Create and configure the ServiceHost
                host = new ServiceHost(typeof(GamingLobbyService));
                host.Open();
                Console.WriteLine("Gaming Lobby Service is running at net.tcp://localhost:8000/GamingLobbyService");
                Console.WriteLine("Press any key to stop the service...");

                // Keep the console running
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                // Log full exception details
                Console.WriteLine($"Error starting the service: {ex}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException}");
                }
            }
            finally
            {
                // Ensure the host is closed properly
                if (host != null && host.State != CommunicationState.Closed)
                {
                    try
                    {
                        host.Close();
                        Console.WriteLine("Gaming Lobby Service has stopped.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing the service: {ex}");
                    }
                }
            }
        }
    }
}