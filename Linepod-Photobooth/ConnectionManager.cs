using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Speech.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using HPI.HCI.Bachelorproject1617.PhotoBooth;
using System.Diagnostics;
using System.Windows;

namespace Hpi.Hci.Bachelorproject1617.PhotoBooth
{
    class ConnectionManager
    {
        public AsynchronousClient asyncClient;
        public Socket client;
        private static BluetoothClient thisDevice;
        private Boolean alreadyPaired = false;
        BluetoothDeviceInfo device;
        private bool BluetoothOn;
        private SpeechSynthesizer Reader;
        SpeechInteraction speechInteraction;
        public ConnectionManager(bool BluetoothOn, SpeechSynthesizer Reader, SpeechInteraction speechInteraction)
        {
            this.BluetoothOn = BluetoothOn;
            this.Reader = Reader;
            this.speechInteraction = speechInteraction;
            if (BluetoothOn)
            {
                if (BluetoothRadio.PrimaryRadio.Mode == RadioMode.Discoverable)
                {
                    thisDevice = new BluetoothClient();
                    BluetoothComponent bluetoothComponent = new BluetoothComponent(thisDevice);
                    bluetoothComponent.DiscoverDevicesProgress += bluetoothComponent_DiscoverDevicesProgress;
                    bluetoothComponent.DiscoverDevicesComplete += bluetoothComponent_DiscoverDevicesComplete;
                    bluetoothComponent.DiscoverDevicesAsync(8, true, true, true, false, thisDevice);
                    Console.WriteLine("Connectable");
                }
                else
                {
                    Reader.Speak("Please turn on your Bluetooth adapter");

                }
            }
            else
            {
                asyncClient = new AsynchronousClient(speechInteraction);
                InitAsyncTCPClient();
            }

        }


        private void InitAsyncTCPClient()
        {
            try
            {

                client = asyncClient.StartClient();
            }
            catch (System.Net.Sockets.SocketException)
            {
                InitAsyncTCPClient();
            }
        }

        private void bluetoothComponent_DiscoverDevicesComplete(object sender, DiscoverDevicesEventArgs e)
        {
            Console.WriteLine("Discovery finished");
        }

        private void bluetoothComponent_DiscoverDevicesProgress(object sender, DiscoverDevicesEventArgs e)
        {
            foreach (BluetoothDeviceInfo device in e.Devices)
            {
                Debug.WriteLine(device.DeviceName + " is a " + device.ClassOfDevice.MajorDevice.ToString());
                if (device.DeviceName.Contains("linepod-photobooth") && !alreadyPaired) //osboxes vs raspberry
                {
                    this.device = device;

                }
            }
        }

        public void SendSvgBluetooth(String svgString, MainWindow mainWindow)
        {
            if (thisDevice.Connected)
            {
                Debug.WriteLine("Connected");
                NetworkStream stream = thisDevice.GetStream();

                if (stream.CanWrite)
                {
                    Guid uuid = System.Guid.NewGuid();
                    int length = Encoding.UTF8.GetBytes(svgString).Length;
                    byte[] rv = new byte[36 + 4 + length];
                    Console.WriteLine("length of svg is " + length);
                    System.Buffer.BlockCopy(Encoding.ASCII.GetBytes(uuid.ToString()), 0, rv, 0, 36);
                    byte[] result = HelperFunctions.IntToByteArray(length);
                    System.Buffer.BlockCopy(result, 0, rv, 36, 4);
                    System.Buffer.BlockCopy(Encoding.UTF8.GetBytes(svgString), 0, rv, 36 + 4, length);
                    stream.Write(rv, 0, 36 + 4 + length);
                    //Console.WriteLine(rv.ToString());

                    mainWindow.pictureTaken = false;
                    mainWindow.AlreadyConvertedToSVG = false;
                }
            }
            else
            {
                Debug.WriteLine("Not Connected");
            }

        }

        public void SendSvgTCP(String svgString, MainWindow mainWindow)
        {
            Console.WriteLine("Sending");
            if (client != null)
            {

                if (client.Connected)
                {
                    Debug.WriteLine("Connected");

                    AsynchronousClient.Send(client, svgString);
                    mainWindow.pictureTaken = false;
                    mainWindow.AlreadyConvertedToSVG = false;

                }
                else
                {
                    Debug.WriteLine("Not Connected");
                }
            }
            else
            {
                Console.WriteLine("Client is null");
            }
        }

        public void BluetoothConnect()
        {

            if (!thisDevice.Connected)
            {

                bool paired = BluetoothSecurity.PairRequest(device.DeviceAddress, "123456");
                if (paired)
                {
                    alreadyPaired = true;
                    Console.WriteLine("Paired!");
                    thisDevice.BeginConnect(device.DeviceAddress, BluetoothService.SerialPort, result => Connected(result, speechInteraction), device);

                }
                else
                {
                    Console.WriteLine("There was a problem pairing.");
                }
            }
            else
            {
                speechInteraction.fsm.Fire(SpeechInteraction.Command.Connected);
                Console.WriteLine("Woohoo we are already connected!");
            }
        }

        private void Connected(IAsyncResult result, SpeechInteraction speechInteraction)
        {

            if (result.IsCompleted)
            {
                if (thisDevice.Connected)
                {

                    Action lambdaConnected = () => speechInteraction.fsm.Fire(SpeechInteraction.Command.Connected);
                    Application.Current.Dispatcher.Invoke(
                    (Delegate)lambdaConnected);
                    // client is connected now :)
                    NetworkStream stream = thisDevice.GetStream();
                    if (stream.CanRead)
                    {
                        byte[] myReadBuffer = new byte[1024];
                        StringBuilder myCompleteMessage = new StringBuilder();
                        int numberOfBytesRead = 0;

                        // Incoming message may be larger than the buffer size. 
                        do
                        {

                            if (stream.DataAvailable)
                            {
                                numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);

                                Console.WriteLine("Received Bytes");
                                Console.WriteLine(BitConverter.ToString(myReadBuffer));
                                int value = 0;
                                for (int i = 0; i < 4; i++)
                                {
                                    value = (value << 4) + (myReadBuffer[i] & 0xff);
                                }
                                Console.WriteLine("value " + value);
                                switch (value)
                                {
                                    case 0:
                                        Console.WriteLine("Received input data from airbar");
                                        break;
                                    case 1:
                                        int status = 0;
                                        for (int i = 4; i < 8; i++)
                                        {
                                            status = (status << 4) + (myReadBuffer[i] & 0xff);
                                        }
                                        Console.WriteLine("Status " + status);
                                        byte[] subArray = new byte[36];
                                        int start = sizeof(int) + sizeof(int);
                                        switch (status)
                                        {
                                            case 0:
                                                Console.WriteLine("Finished printing");
                                                Action lambda = () => speechInteraction.fsm.Fire(SpeechInteraction.Command.Printed);
                                                Application.Current.Dispatcher.Invoke(
                                                (Delegate)lambda);
                                                break;


                                        }
                                        break;
                                }

                            }

                        }

                        while (stream.CanRead && thisDevice.Connected);

                    }
                    else
                    {
                        Console.WriteLine("could not connect");
                    }
                }

            }
            else
            {
                Console.WriteLine("Could not connect");
            }
        }



        /*
                public static void DecodeReceivedData(byte[] myReadBuffer, SpeechInteraction speechInteraction)
                {
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

        /*      Console.WriteLine("Received input data from airbar");
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
                      Action lambda = () => speechInteraction.fsm.Fire(SpeechInteraction.Command.Printed);
                      Application.Current.Dispatcher.Invoke(
                      (Delegate)lambda);
                      break;


              }
              break;
      }
      Console.WriteLine("Stepped through decision tree");

  }
*/


    }
}
