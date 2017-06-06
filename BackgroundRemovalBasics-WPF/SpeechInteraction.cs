using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Appccelerate.StateMachine;
using System.Speech.Synthesis;
using Microsoft.Speech.Recognition;
using System.Timers;
using System.Threading;

namespace HPI.HCI.Bachelorproject1617.PhotoBooth
{

    public class SpeechInteraction
    {
        bool isSpeaking = false;
        public bool outlines = false;
        //String NoPersonRecognizedRepeat = @"Huhu, is someone there? I don't see anyone... Don't be shy, just step about 1 meter in front of the kinect-camera and we can do an awesome tactile snapshot of you! If you don't get recognized, try to look more like a see star by spreading your arms away from you";
        //String NoPersonRecognizedRepeat = @"Huhu, is someone there? Just step about 1 meter in front of the kinect-camera and we can do an awesome tactile snapshot of you! If you want get a mate and do a cool pose together";
        String NoPersonRecognizedRepeat = @"Huhu, is someone there? Just step about 1 meter in front of the kinect-camera and we can do an awesome tactile snapshot of you!";
        //String PersonRecognizedTransition = @"Woop woop! I recognized a person!! I would suggest to print a picture of your silhouette now, what do you think? If you want that too, just say 'outlines' . Otherwise we can also print you as a stick-figure, which looks also pretty cool if you do a crazy gesture. If you want to try that, just say 'skeleton'";
        String PersonRecognizedTransition = @"Woop woop! I recognized a person!! Please tell me if you want to print your outlines or your skeleton?";
        String PersonLeft = @"Oohh, where did you go? I can't see you anymore";
        //String PersonRecognizedRepeat = @"hey, are you still there? Don't forget, I would suggest to print your silhouette now, what do you think? If you want that too, just say 'outlines' . Otherwise we can also print you as a stick-figure, which looks also pretty cool if you do a crazy gesture. If you want to do that, just say 'skeleton'";
        String PersonRecognizedRepeat = @"hey, are you still there? Don't forget, if you want to print your silhouette now, just say 'outlines' . Otherwise we can also print you as a stick-figure if you say 'skeleton'"; 
        String PictureTaking1 = "Alright, I will take a picture in 3...2...1...";
        Prompt PictureTaking1Prompt = new Prompt("Alright, I will take a picture in 3...2...1...");
        //String PictureTaking2 = @"Awesome shot! Do you want me to print it now? If so, just say 'print'. Otherwise you can also go back to take another picture.";
        String PictureTaking2 = @"Awesome shot! Do you want me to print it now?";
        Prompt PictureTaking2Prompt = new Prompt("Awesome shot! Do you want me to print it now?");
        //String PictureTakenRepeat = @"Do you want me to print the picture now? If yes just say 'print'";
        String PictureTakenRepeat = @"Do you want me to print the picture? ";
        String BackToPersonRecognized = @"going back... if you want to take a picture of your silhouette just say 'outlines', otherwise you can also print a picture of yourself as a stick-person by saying 'skeleton'";
        String ConnectingString = @"connecting to Linepod";
        String PrintingString = "printing";
        String PrintedString = @"Woohoo, our Linepod just finished printing. Next person please.";
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
                    

                    /*while (reader.GetCurrentlySpokenPrompt() != PictureTaking1Prompt) ;
                    while (reader.GetCurrentlySpokenPrompt() == PictureTaking1Prompt) ;*/
                    //Console.WriteLine("Finished utterance");
                    //PictureSkeleton(null, null);
                    StartTimer(45000);
                    //reader.SpeakCompleted += PictureOutlines;
                    //System.Threading.Thread.Sleep(5000);
                    //take picture outlines
                })
                .On(Command.Skeleton).Goto(ProcessState.PictureTaken).Execute(() =>
                {
                    outlines = false;
                    new Thread(PreparePicture).Start();

                    StartTimer(45000);
                    /*isSpeaking = true;
                    outlines = false;
                    reader.SpeakCompleted += PictureSkeleton;*/
                    //take picture skeleton
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
           
            reader.SpeakAsyncCancelAll();
            reader.SpeakAsync(text);
            Console.WriteLine("Finished speaking");
            
            

        }

        public void SpeakText(Prompt text)
        {

            reader.SpeakAsyncCancelAll();
            reader.SpeakAsync(text);
            Console.WriteLine("Finished speaking");



        }


        void reader_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            if (mainWindow.speechEngine != null)
            {
                mainWindow.speechEngine.RecognizeAsyncCancel();
                
            }
        }


        void reader_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            
            if (mainWindow.speechEngine != null)
            {
                mainWindow.speechEngine.RecognizeAsyncCancel();
                //System.Threading.Thread.Sleep(1000);
                try
                {
                    mainWindow.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("InvalidOperationException");
                }

                
                
            }/*
            if (isSpeaking)
            {
                if (outlines)
                {
                    PictureOutlines();

                }
                else
                {
                    PictureSkeleton();
                }
            }*/
        }

        private void PreparePicture()
        {
            SpeakText(PictureTaking1Prompt);
            //while (reader.GetCurrentlySpokenPrompt() != PictureTaking1Prompt) ;
            //while (reader.GetCurrentlySpokenPrompt() == PictureTaking1Prompt) ;
            
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
