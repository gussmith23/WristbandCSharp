using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Drawing;

namespace WristbandCsharp
{
    class Arduino
    {

        private const int DEFAULT_THETA = 0;
        private const int DEFAULT_INTENSITY = 50;
        private const int DEFAULT_DURATION = 50;
        private ArduinoPort port = null;

        class ArduinoPort : SerialPort
        {
            // Initializes on COM4.
            public ArduinoPort(string portName) : base(portName)
            {
                Open();
                Console.WriteLine("Initialized on {0}.", portName);
            }
        }

        public Arduino(string portName)
        {
            port = new ArduinoPort(portName);
        }

        ~Arduino()
        {
            try
            {
                ReleaseCamera();
            }
            catch (System.IO.IOException ioexception)
            {
                // Port disconnected before camera released.
            }
        }

        
        public void Process(Rectangle rectangle, PointF average, PointF center)
        {

            int thetaPercent = DEFAULT_THETA;
            int intensityPercent = DEFAULT_INTENSITY;
            int durationPercent = DEFAULT_DURATION;

            #region Minimum data sent to Arduino (direction only)

            PointF directionVector = PointF.Subtract(average,new SizeF(center));

            double angle = (Math.Atan2(directionVector.Y, directionVector.X) / (Math.PI * 2));
            thetaPercent = (int) (angle * 100.0);
            if (thetaPercent < 0) thetaPercent += 100;

            #endregion

            try
            {
                SendPacket(thetaPercent, 25, 1);
            }
            catch (System.IO.IOException e)
            {
                throw e;
            }
        }


        private void SendPacket(int thetaPercent, int intensityPercent, int durationPercent)
        {
            try
            {
                port.Write(new byte[] { 255, (byte)thetaPercent, (byte)intensityPercent, (byte)durationPercent, 0 }, 0, 5);
                Console.WriteLine("{0} {1} {2}", thetaPercent, intensityPercent, durationPercent);
                char[] buf = new char[128];
                int i = port.BytesToRead;
                while (i-- > 0) Console.Write(port.ReadChar());
            }
            catch (System.IO.IOException e)
            {
                throw new System.IO.IOException(string.Format("Could not write to port %s.", port.PortName));
            }
        }

        private void ReleaseCamera() 
        {
            try
            {
                port.Close();
            }
            catch (System.IO.IOException ioException)
            {
                
                throw new System.IO.IOException(string.Format("Port disconnected before properly closed"));
            }
        }

    }
}
