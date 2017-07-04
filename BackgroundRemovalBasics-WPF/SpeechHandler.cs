using HPI.HCI.Bachelorproject1617.PhotoBooth;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Hpi.Hci.Bachelorproject1617.PhotoBooth
{
    public class SpeechHandler
    {
        public SpeechRecognitionEngine speechEngine;
        public SpeechInteraction speechInteraction;
        public RecognizerInfo ri;
        public KinectSensor sensor;
        public SpeechHandler(SpeechInteraction speechInteraction, KinectSensor sensor)
        {
            this.sensor = sensor;
            this.speechInteraction = speechInteraction;
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                String result = e.Result.Semantics.Value.ToString();
                Console.WriteLine(result);
                switch (result)
                {

                    case "OUTLINES":

                        speechInteraction.fsm.Fire(SpeechInteraction.Command.Outlines);

                        break;
                    case "SKELETON":
                        speechInteraction.fsm.Fire(SpeechInteraction.Command.Skeleton);

                        break;
                    case "BACK":
                        speechInteraction.fsm.Fire(SpeechInteraction.Command.Back);

                        break;
                    case "PRINT":
                    case "YES":
                        speechInteraction.fsm.Fire(SpeechInteraction.Command.Print);
                        break;
                    case "TEST":
                        speechInteraction.fsm.Fire(SpeechInteraction.Command.Test);
                        break;


                }
            }
        }


        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase))
                { //&& "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                    return recognizer;
                }
            }

            return null;
        }

        public void InitializeSpeechRecognizer()
        {
            ri = GetKinectRecognizer();
            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);
                var directions = new Choices();
                directions.Add(new SemanticResultValue("outlines", "OUTLINES"));
                directions.Add(new SemanticResultValue("skeleton", "SKELETON"));
                //directions.Add(new SemanticResultValue("print both", "BOTH"));
                directions.Add(new SemanticResultValue("take picture", "OUTLINES"));
                directions.Add(new SemanticResultValue("back", "BACK"));
                //directions.Add(new SemanticResultValue("print", "PRINT"));
                directions.Add(new SemanticResultValue("test", "TEST"));
                directions.Add(new SemanticResultValue("yes", "YES"));


                //german
                directions.Add(new SemanticResultValue("bereit", "OUTLINES"));
                directions.Add(new SemanticResultValue("zurück", "BACK"));
                directions.Add(new SemanticResultValue("drucken", "PRINT"));
                directions.Add(new SemanticResultValue("ja", "YES"));


                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);

                var g = new Grammar(gb);
                speechEngine.LoadGrammar(g);
                speechEngine.SpeechRecognized += SpeechRecognized;
                speechEngine.SpeechRecognitionRejected += SpeechRejected;

                speechEngine.SetInputToAudioStream(
                    sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

                speechEngine.RecognizeAsync(RecognizeMode.Multiple);

            }
        }



        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Debug.WriteLine("Speech rejected");
        }

        public void StopSpeechHandler()
        {
            this.speechEngine.SpeechRecognized -= SpeechRecognized;
            this.speechEngine.SpeechRecognitionRejected -= SpeechRejected;
            this.speechEngine.RecognizeAsyncStop();
        }
    }
}
