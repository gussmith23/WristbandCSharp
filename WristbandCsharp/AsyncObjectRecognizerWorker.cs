using ObjectSpeechRecognizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WristbandCsharp
{
    public class AsyncObjectRecognizerWorker
    {
        ObjectRecognizer or;
        public delegate void pickItem(string itemToPick);
        pickItem p;

        public AsyncObjectRecognizerWorker(ObjectRecognizer or, pickItem pickItem)
        {
            this.or = or;
            or.SpeechRecognized += or_SpeechRecognized;
            p = pickItem;

        }

        void or_SpeechRecognized(string Text, float Confidence)
        {
            if (Confidence > 50.0)
            {
                p(Text);
                shouldStop = true;
            }
        }

        public void doWork()
        {
            while(!shouldStop)
            {
                or.Recognize();
            }
        }


        private volatile bool shouldStop = false;
        public void requestStop()
        {
            shouldStop = true;
        }
    }
}
