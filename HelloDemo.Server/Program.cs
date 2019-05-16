using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HelloDemo.Server
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Reading server config");
            var port = GetPort();

            if (port <= 0)
            {
                Console.WriteLine("Port env variable not set");
                Environment.Exit(-1);
            }

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine($"Starting server on port: {port}");
            Console.WriteLine($"Server started on port: {port} waiting for incoming clients");
            var cts = new CancellationTokenSource();

            // .Net core way of handling stop
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Server stop requested by ctrl-c/signal event");
                cts.Cancel();
            };


            // ReSharper disable once MethodSupportsCancellation
            await StartAcceptingAsync(listener, cts.Token).ConfigureAwait(false);

            Console.WriteLine($"Server stopped");
        }

        private static async Task StartAcceptingAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            var connections = new Dictionary<Task, string>();

            void RemoveConnection(Task task, string connection)
            {
                lock (connections)
                {
                    connections.Remove(task);
                }

                Console.WriteLine($"Removed connection: {connection}");
            }

            void AddConnection(TcpClient client, string connection)
            {
                var connectionTask = HandleClientAsync(client, cancellationToken).ContinueWith(c => RemoveConnection(c, connection));

                lock (connections)
                {
                    connections.Add(connectionTask, connection);
                }

                Console.WriteLine($"Added connection: {connection}");
            }

            bool IsShutdown()
            {
                lock (connections)
                {
                    return connections.Count == 0;
                }
            }

            using (cancellationToken.Register(() =>
            {
                // Stop the server always
                listener.Stop();
            }))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var client = await listener.AcceptSocketWithCancellationAsync(cancellationToken).ConfigureAwait(false);
                        AddConnection(client, client.Client.RemoteEndPoint.ToString());
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore this because this only happens when we're stopped.
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failure while accepting socket: {e}");
                    }
                }

                // Poll for socket shutdowns.
                PollHelper.PollUntil(IsShutdown, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var connection = client.Client.RemoteEndPoint;

            using (client)
            {
                try
                {
                    var x = 1;

                    using (var sw = new StreamWriter(client.GetStream()))
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            await sw.WriteLineAsync($"Hello demo: {connection} this is your {x} message").ConfigureAwait(false);
                            await sw.FlushAsync().ConfigureAwait(false);
                            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                            x++;
                        }

                        await sw.WriteLineAsync("Server is shutting down !").ConfigureAwait(false);
                        await sw.FlushAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Allow this
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Socket error: {e} on connection: {connection}");
                }
            }
        }

        static int GetPort()
        {
            var machinePort = Environment.GetEnvironmentVariable("PORT", EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrWhiteSpace(machinePort))
                return int.Parse(machinePort);

            var processPort = Environment.GetEnvironmentVariable("PORT", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(processPort))
                return int.Parse(processPort);

            var userPort = Environment.GetEnvironmentVariable("PORT", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(userPort))
                return int.Parse(userPort);

            return 0;
        }
    }
}
