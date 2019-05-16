using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HelloDemo.Server
{
    public static class SocketHelper
    {
        public static async Task<TcpClient> AcceptSocketWithCancellationAsync(this TcpListener tcpListener, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<int>();
            var acceptTask = Task.Run(() => tcpListener.AcceptTcpClient(), cancellationToken);

            using (cancellationToken.Register(() => { tcs.TrySetCanceled(cancellationToken); }))
            {
                var resultTask = await Task.WhenAny(acceptTask, tcs.Task).ConfigureAwait(false);

                if (resultTask == acceptTask)
                {
                    tcs.TrySetResult(0);
                    return await acceptTask.ConfigureAwait(false);
                }
                
                throw new OperationCanceledException(cancellationToken);
            }

        }
    }
}
