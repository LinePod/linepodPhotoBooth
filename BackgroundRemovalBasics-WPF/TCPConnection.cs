using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;



namespace Hpi.Hci.Bachelorproject1617.PhotoBooth {





// State object for receiving data from remote device.
public class StateObject {
    // Client socket.
    public Socket workSocket = null;
    // Size of receive buffer.
    public const int BufferSize = 256;
    // Receive buffer.
    public byte[] buffer = new byte[BufferSize];
    // Received data string.
    public StringBuilder sb = new StringBuilder();
}

public class AsynchronousClient {
    // The port number for the remote device.
    private const int port = 3000;

    // ManualResetEvent instances signal completion.
    private static ManualResetEvent connectDone = 
        new ManualResetEvent(false);
    private static ManualResetEvent sendDone = 
        new ManualResetEvent(false);
    private static ManualResetEvent receiveDone = 
        new ManualResetEvent(false);

    // The response from the remote device.
    private static String response = String.Empty;
    public static Socket StartClient() {
        // Connect to a remote device.
        try {

            IPAddress ipAddress = IPAddress.Parse("169.254.161.241");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            Socket client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.
            client.BeginConnect( remoteEP, 
                new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();

            // Send test data to the remote device.
            //Send(client,"This is a test<EOF>");
            //sendDone.WaitOne();

            // Receive the response from the remote device.
            //Receive(client);
            //receiveDone.WaitOne();

            // Write the response to the console.
            Console.WriteLine("Response received : {0}", response);

            // Release the socket.
            
            return client;
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
        return null; ;
    }

    public static void StopClient(Socket client)
    {
        client.Shutdown(SocketShutdown.Both);
        client.Close();
    }

    private static void ConnectCallback(IAsyncResult ar) {
        try {
            // Retrieve the socket from the state object.
            Socket client = (Socket) ar.AsyncState;

            // Complete the connection.
            client.EndConnect(ar);

            Console.WriteLine("Socket connected to {0}",
                client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.
            connectDone.Set();
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    private static void Receive(Socket client) {
        try {
            // Create the state object.
            AsyncCallback callback = new AsyncCallback(ReceiveCallback);
            
            // Begin receiving the data from the remote device.
            while (client.Connected)
            {
                StateObject state = new StateObject();
                state.workSocket = client;
            
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    callback, state);
            }
            
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    private static void ReceiveCallback( IAsyncResult ar ) {
        try {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject) ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0) {
                // There might be more data, so store the data received so far.
            state.sb.Append(Encoding.ASCII.GetString(state.buffer,0,bytesRead));

                // Get the rest of the data.
                client.BeginReceive(state.buffer,0,StateObject.BufferSize,0,
                    new AsyncCallback(ReceiveCallback), state);
            } else {
                // All the data has arrived; put it in response.
                if (state.sb.Length > 1) {
                    response = state.sb.ToString();
                    Console.WriteLine(response);
                }
                // Signal that all bytes have been received.
                receiveDone.Set();
            }
            byte[] myReadBuffer = state.buffer;
            Console.WriteLine("Casted bytes to int array");
            //int type = bytesAsInts[0];
            //Console.WriteLine("first int " + type);
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                value = (value << 4) + (myReadBuffer[i] & 0xff);
            }
            Console.WriteLine("value " + value);
            switch (value)
            {
                case 0:
                    /*Console.WriteLine("x1 " + bytesAsInts[1]);
                    Console.WriteLine("y1 " + bytesAsInts[2]);
                    Console.WriteLine("x2 " + bytesAsInts[3]);
                    Console.WriteLine("y3 " + bytesAsInts[4]);*/

                    Console.WriteLine("Received input data from airbar");
                    break;
                case 1:
                    int status = 0;
                    for (int i = 4; i < 8; i++)
                    {
                        status = (status << 4) + (myReadBuffer[i] & 0xff);
                    }
                    //int status = bytesAsInts[1];
                    Console.WriteLine("Status " + status);
                    Console.WriteLine("received uuid");
                    byte[] subArray = new byte[36];
                    int start = sizeof(int) + sizeof(int);
                    //Buffer.BlockCopy(myReadBuffer, 0, subArray, start, 36);

                    //String uuid = Encoding.ASCII.GetString(subArray);
                    //Console.WriteLine("received uuid " + uuid);
                    switch (status)
                    {
                        case 0:
                            Console.WriteLine("Finished printing");
                            /*Action lambda = () => speechInteraction.fsm.Fire(SpeechInteraction.Command.Printed);
                            Application.Current.Dispatcher.Invoke(
                            (Delegate)lambda);*/
                            break;


                    }
                    break;
            }
            Console.WriteLine("Stepped through decision tree");

            //MainWindow.DecodeReceivedData(state.buffer,null);
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    public static void Send(Socket client, String data) {
        // Convert the string data to byte data using ASCII encoding.
        
        Guid uuid = System.Guid.NewGuid();
        int length = Encoding.UTF8.GetBytes(data).Length;
        byte[] rv = new byte[36 + 4 + length];
        Console.WriteLine("length of svg is " + length);
        System.Buffer.BlockCopy(Encoding.ASCII.GetBytes(uuid.ToString()), 0, rv, 0, 36);
        byte[] result = MainWindow.IntToByteArray(length);
        System.Buffer.BlockCopy(result, 0, rv, 36, 4);
        System.Buffer.BlockCopy(Encoding.UTF8.GetBytes(data), 0, rv, 36 + 4, length);

        // Begin sending the data to the remote device.
        client.BeginSend(rv, 0, rv.Length,0,
            new AsyncCallback(SendCallback), client);
       
    }

    private static void SendCallback(IAsyncResult ar) {
        try {
            // Retrieve the socket from the state object.
            Socket client = (Socket) ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            // Signal that all bytes have been sent.
            sendDone.Set();
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    }
}
