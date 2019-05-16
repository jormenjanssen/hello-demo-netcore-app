using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HelloDemo.Client
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Reading client config");
            var endpointConfigString = GetEndpoint();
            var address = endpointConfigString.Split(':')[0];
            var port = int.Parse(endpointConfigString.Split(':')[1]);

            var cts = new CancellationTokenSource();

            // .Net core way of handling stop
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Client stop requested by ctrl-c/signal event");
                cts.Cancel();
            };

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var client = new TcpClient();
                        await client.ConnectAsync(address, port).ConfigureAwait(false);
                        await HandleConnectionAsync(client, cts.Token).ConfigureAwait(false);
                        Console.WriteLine("Connection disconnected");
                    }
                    catch (SocketException se) when (se.SocketErrorCode == SocketError.HostUnreachable ||
                                                     se.SocketErrorCode == SocketError.ConnectionRefused ||
                                                     se.SocketErrorCode == SocketError.HostNotFound ||
                                                     se.SocketErrorCode == SocketError.NetworkUnreachable ||
                                                     se.SocketErrorCode == SocketError.NetworkDown)

                    {
                        Console.WriteLine($"Socket error not connect to host reason: {se.SocketErrorCode}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Allow this
            }
        }

        private static async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (cancellationToken.Register(client.Close))
                using(var stream = client.GetStream())
                using (client)
                {
                    var buffer = new byte[4096];

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var length = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (length <= 0)
                        {
                            return;
                        }
                       Console.Write(System.Text.Encoding.ASCII.GetString(buffer,0,length));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static string GetEndpoint()
        {
            var machineEndpoint = Environment.GetEnvironmentVariable("ENDPOINT", EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrWhiteSpace(machineEndpoint))
                return machineEndpoint;

            var processEndpoint = Environment.GetEnvironmentVariable("ENDPOINT", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(processEndpoint))
                return processEndpoint;

            var userEndpoint = Environment.GetEnvironmentVariable("ENDPOINT", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(userEndpoint))
                return userEndpoint;

            return null;
        }
    }
}
