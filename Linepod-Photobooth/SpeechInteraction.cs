using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Appccelerate.StateMachine;
using System.Speech.Synthesis;
using Microsoft.Speech.Recognition;
using System.Timers;
using System.Threading;
using Hpi.Hci.Bachelorproject1617.PhotoBooth;

namespace HPI.HCI.Bachelorproject1617.PhotoBooth
{
    public class SpeechInteraction
    
    {
        public SpeechHandler speechHandler;
        bool isSpeaking = false;
        public bool outlines = false;
        //String NoPersonRecognizedRepeat = @"Huhu, is someone there? Just step about 1 meter in front of the kinect-camera and we can do an awesome tactile snapshot of you!";
        const String NoPersonRecognizedRepeat = @"Hallo, ist da jemand? Komm ins Bild, dann kann ich ein cooles, fühlbares Foto von dir machen!";

        //String PersonRecognizedTransition = @"Woop woop! I recognized a person!! Please tell me if you want to print your outlines or your skeleton?";
        const String PersonRecognizedTransition = @"Wuhuu, ich habe eine Person erkannt. Möchtest du jetzt ein Foto von deinem Umriss machen?";
        
        //String PersonLeft = @"Oohh, where did you go? I can't see you anymore";
        const String PersonLeft = @"Wo bist du hin? Ich kann dich nicht mehr sehen!";
        //String PersonRecognizedRepeat = @"hey, are you still there? Don't forget, if you want to print your silhouette now, just say 'outlines' . Otherwise we can also print you as a stick-figure if you say 'skeleton'"; 
        const String PersonRecognizedRepeat = @"hey, noch da? Nicht vergessen: wenn du magst, mache ich ein taktiles Foto von deinem Umriss? Möchtest du das tun?"; 
        //String PictureTaking1 = "Alright, I will take a picture in 3...2...1...";
        const String PictureTaking1 = "Alles klar, nehme das Bild auf in 3...2...1...";
        //Prompt PictureTaking1Prompt = new Prompt("Alright, I will take a picture in 3...2...1...");
        Prompt PictureTaking1Prompt = new Prompt("Alles klar, nehme das Bild auf in 3...2...1...");
        //String PictureTaking2 = @"Awesome shot! Do you want me to print it now?";
        const String PictureTaking2 = @"Gutes Bild! Soll ich das Foto nun drucken?";
        //String PictureTakenRepeat = @"Do you want me to print the picture? ";
        const String PictureTakenRepeat = @"Hey, noch da? Soll ich das Foto nun drucken? ";
        //String BackToPersonRecognized = @"going back... if you want to take a picture of your silhouette just say 'outlines', otherwise you can also print a picture of yourself as a stick-person by saying 'skeleton'";
        const String BackToPersonRecognized = @"gehe zurück... Möchtest du ein neues Bild machen?";
        //String ConnectingString = @"connecting to Linepod";
        const String ConnectingString = @"verbinde mit dem Linepod";
        //String PrintingString = "printing";
        const String PrintingString = "drucke...";
        //String PrintedString = @"Woohoo, our Linepod just finished printing. Next person please.";
        const String PrintedString = @"Wuhu, unser Linepod ist fertig mit drucken. Viel Spaß mit dem Bild.";
        System.Timers.Timer timer;

        public enum ProcessState
        {
            NoPersonRecognized,
            PersonRecognized,
            PictureTaken,
            Connected,
            Printed
        }

        public enum Command
        {
            Back,
            Help,
            Print,
            Outlines,
            Skeleton,
            FramesReady,
            FramesNotReady,
            Repeat,
            Connected,
            Printed,
            Test,
            Yes
        }

        public PassiveStateMachine<ProcessState, Command> fsm;

        SpeechSynthesizer reader;
        private Hpi.Hci.Bachelorproject1617.PhotoBooth.MainWindow mainWindow;


        public SpeechInteraction(Hpi.Hci.Bachelorproject1617.PhotoBooth.MainWindow mainWindow, SpeechSynthesizer reader)
        {
            this.mainWindow = mainWindow;


            this.reader = reader;

            this.reader.SpeakStarted += reader_SpeakStarted;
            this.reader.SpeakCompleted += reader_SpeakCompleted;
            
            //SpeakText(@"Welcome to this magical Kinect Photobooth, the camera that produces tactile snapshots of your gestures. If you want a picture just position yourself 1 meter in front of the Kinect-Camera and spread your arms away from you to get recognized");
            //SpeakText(@"Hey there, if you want a tactile snapshot of your gesture just position yourself 1 meter in front of the Kinect-Camera and spread your arms away from you to get recognized. I can detect two persons at the time so get a mate and let's go");

            fsm = new PassiveStateMachine<ProcessState, Command>();
            fsm.In(ProcessState.NoPersonRecognized)
                .On(Command.Repeat).Execute(() =>
                {
                    SpeakText(NoPersonRecognizedRepeat);
                    StartTimer(35000);
                }) //reader.Speak(NoPersonRecognizedRepeat)
                .On(Command.FramesReady).Goto(ProcessState.PersonRecognized).Execute(() =>
                {
                    SpeakText(PersonRecognizedTransition);
                    Console.WriteLine("Frames ready");
                    StartTimer(35000);
                }).On(Command.Test).Goto(ProcessState.PictureTaken);
            fsm.In(ProcessState.PersonRecognized)
                .On(Command.Repeat).Execute(() => { SpeakText(PersonRecognizedRepeat); 
                    StartTimer(35000); 
                })
                .On(Command.FramesNotReady).Goto(ProcessState.NoPersonRecognized).Execute(() => { 
                    SpeakText(PersonLeft);
                    Console.WriteLine("Frames not ready");
                    StartTimer(35000);
                })
                .On(Command.Outlines).Goto(ProcessState.PictureTaken).Execute(() =>
                {
                    outlines = true;
                    new Thread(PreparePicture).Start();
                    
                    StartTimer(45000);
                 
                })
                .On(Command.Skeleton).Goto(ProcessState.PictureTaken).Execute(() =>
                {
                    outlines = false;
                    new Thread(PreparePicture).Start();

                    StartTimer(45000);
                 
                });
            fsm.In(ProcessState.PictureTaken)
                .On(Command.Print).Goto(ProcessState.Connected).Execute(() => {
                    SpeakText(ConnectingString);
                    if (mainWindow.BluetoothOn)
                    {
                        mainWindow.BluetoothConnect();
                    }
                    else
                    {
                        fsm.Fire(Command.Connected);
                    }
                })
                .On(Command.Yes).Goto(ProcessState.Connected).Execute(() =>
                {
                    SpeakText(ConnectingString);
                    if (mainWindow.BluetoothOn)
                    {
                        mainWindow.BluetoothConnect();
                    }
                    else
                    {
                        fsm.Fire(Command.Connected);
                    }
                })
                .On(Command.Repeat).Execute(() =>
                {
                    SpeakText(PictureTakenRepeat);
                    StartTimer(35000);
                })
                .On(Command.Back).Goto(ProcessState.PersonRecognized).Execute(() =>
                {
                    SpeakText(BackToPersonRecognized);
                    mainWindow.pictureTaken = false;
                    mainWindow.AlreadyConvertedToSVG = false;
                    StartTimer(35000);
                });
            fsm.In(ProcessState.Connected)
                .On(Command.Connected).Goto(ProcessState.Printed).Execute(() => { 
                    SpeakText(PrintingString);
                    if (mainWindow.BluetoothOn)
                    {
                        mainWindow.SendSvgBluetooth(mainWindow.svgImage);
                    }
                    else
                    {
                        mainWindow.SendSvgTCP(mainWindow.svgImage);
                    }
                    
                });
            fsm.In(ProcessState.Printed)
                .On(Command.Printed).Goto(ProcessState.NoPersonRecognized).Execute(() => { SpeakText(PrintedString); });

            fsm.In(ProcessState.Connected);
            fsm.Initialize(ProcessState.NoPersonRecognized);
            fsm.Start();
            StartTimer(35000);
            

        }

        private void StartTimer(int time)
        {
            if (timer != null)
            {
                timer.Dispose();
            }
            timer = new System.Timers.Timer(time);
            timer.AutoReset = false;
            timer.Elapsed += new ElapsedEventHandler(HandleTimer);
            timer.Start();
        }

        private void PlayClickSound()
        {
            System.IO.Stream str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.camera_shutter_sound;
            System.Media.SoundPlayer snd = new System.Media.SoundPlayer(str);
            snd.Play();
        }


   
        public void SpeakText(String text)
        {
           
            System.IO.Stream str = null;
                         
           switch (text)
            {
        
                case NoPersonRecognizedRepeat:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.hey_ist_da_jemand;
                    break;
                case PersonRecognizedTransition:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.woop_woop_erkenne_person;
                    break;
                case PersonLeft:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.wo_bist_du_hin;
                    break;
                case PersonRecognizedRepeat:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.ich_habe_eine_person_erkannt;
                    break;
                case PictureTaking1:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.alles_klar_nehme_bild_auf;
                    break;
                /*case PictureTaking1Prompt.ToString():
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.;
                    break;*/
                 case PictureTaking2:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.gutes_bild_soll_ich_drucken;
                    break;
                case PictureTakenRepeat:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.hey_noch_da_soll_ich_nun_drucken;
                    break;
                case BackToPersonRecognized:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.gehe_zurück;
                    break;
                case ConnectingString:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.verbinde_mit_linepod;
                    break;
                 case PrintingString:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.drucke;
                    break;
                case PrintedString:
                    str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.fertig_mit_drucekn;
                    break;
            }
                 
            System.Media.SoundPlayer snd = new System.Media.SoundPlayer(str);
            snd.Play();

            //reader.SpeakAsyncCancelAll();
            //reader.SpeakAsync(text);
            Console.WriteLine("Finished speaking");
            
        }

        public void SpeakText(Prompt text)
        {
            /*try
            {
                reader.SpeakAsyncCancelAll();
                reader.SpeakAsync(text);
                Console.WriteLine("Finished speaking");

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occurred");
            }*/
            System.IO.Stream str = Hpi.Hci.Bachelorproject1617.PhotoBooth.Properties.Resources.alles_klar_nehme_bild_auf;
            
            System.Media.SoundPlayer snd = new System.Media.SoundPlayer(str);
            snd.Play();

             
        }



        void reader_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            if (speechHandler.speechEngine != null)
            {
                speechHandler.speechEngine.RecognizeAsyncCancel();
                
            }
        }


        void reader_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {

            if (speechHandler.speechEngine != null)
            {
                speechHandler.speechEngine.RecognizeAsyncCancel();
                //System.Threading.Thread.Sleep(1000);
                try
                {
                    speechHandler.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("InvalidOperationException");
                }

            }
        }

        private void PreparePicture()
        {
            PictureTaking1Prompt = new Prompt("Alright, I will take a picture in 3...2...1...");
            SpeakText(PictureTaking1Prompt);
            if (outlines)
            {
                PictureOutlines(null, null);
            }
            else
            {
                PictureSkeleton(null, null);
            }
       
        }


        private void PictureSkeleton(object sender, SpeakCompletedEventArgs e)
        {
            Console.WriteLine("Taking pic of skel");

            System.Threading.Thread.Sleep(5000);
            PlayClickSound();
            mainWindow.TakePictureSkeleton();
            System.Threading.Thread.Sleep(1000);
            
            SpeakText(PictureTaking2);
            
        }


        private void PictureOutlines(object sender, SpeakCompletedEventArgs e)
        {
            System.Threading.Thread.Sleep(5000);
            
            PlayClickSound();
            mainWindow.TakePictureOutlines();
            System.Threading.Thread.Sleep(1000);

            SpeakText(PictureTaking2);
            
        }



        private void HandleTimer(object source, ElapsedEventArgs evt)
        {
            fsm.Fire(SpeechInteraction.Command.Repeat);
        }

    }
}
