#pragma warning disable IDE1006 // Naming Styles

//using NetMQ.Sockets;
using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace EmbeddedInstance
{

    public class UnityInstancePipe /*: IDisposable*/
    {

        //const string StartOfFile = "<Message>";
        //const string EndOfFile = "</Message>";

        //public delegate void OnMessageReceived(string json);
        //public OnMessageReceived onMessageReceived;

        //NamedPipeClientStream clientPipe;
        //NamedPipeServerStream serverPipe;
        //public bool isConnected =>
        //    ((PipeStream)clientPipe ?? serverPipe)?.IsConnected ?? false;

        ///// <summary>
        ///// <para>On client: Send json to server.</para>
        ///// <para>On server: Send json to client.</para>
        ///// </summary>
        //public void Send(string json)
        //{
        //    var bytes = Encoding.Unicode.GetBytes(json);
        //    Send(bytes);
        //}

        ///// <summary>
        ///// <para>On client: Send bytes to server.</para>
        ///// <para>On server: Send bytes to client.</para>
        ///// </summary>
        //public void Send(byte[] bytes)
        //{
        //    serverPipe?.Write(bytes, 0, bytes.Length);
        //    clientPipe?.Write(bytes, 0, bytes.Length);
        //    serverPipe?.Flush();
        //    clientPipe?.Flush();
        //}


        //private UnityInstancePipe(string id, bool isServer, Action onDisconnected = null)
        //{

        //    Task.Factory.StartNew(() =>
        //    {

        //        if (isServer)
        //        {
        //            using var responder = new ResponseSocket();
        //            responder.Bind("tcp://*:5555");
        //            responder.ReceiveReady += Responder_ReceiveReady;
        //        }
        //        else
        //        {

        //        }

        //    });

        //}

        //private void Responder_ReceiveReady(object sender, NetMQ.NetMQSocketEventArgs e)
        //{

        //}

        //internal static UnityInstancePipe Server(string id, Action onDisconnected) => new UnityInstancePipe(id, isServer: true, onDisconnected);
        //internal static UnityInstancePipe Client(string id) => new UnityInstancePipe(id, isServer: false);

        //public void Dispose()
        //{
        //    serverPipe?.Dispose();
        //    clientPipe?.Dispose();
        //    serverPipe = null;
        //    clientPipe = null;
        //}

    }

}
