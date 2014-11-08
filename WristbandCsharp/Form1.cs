using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV.UI;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO.Ports;
using System.IO;
using Speech;
using System.Threading;

namespace WristbandCsharp
{
    public partial class Form1 : Form
    {

        Capture cap;
        Image<Bgr,Byte> image;
        Tracker tracker = null;
        Arduino arduino;
        Boolean tracking = false;
        SpeechEngine speechEngine = null;
        Thread speechThread;
        AsyncSpeechWorker speechWorker;
        private ROIStreaming.ROIReceiver roiRec;

        public Form1()
        {

            InitializeComponent();

            IntiailizeROIReceiver();

            // Combo box 1
            string[] itemNames = Directory.GetFiles("itemsToTrack/", "*.jpg");
            foreach (string s in itemNames)
            {
                string name = System.Text.RegularExpressions.Regex.Replace(s, "itemsToTrack/", "");
                name = System.Text.RegularExpressions.Regex.Replace(name, ".jpg", "");
                comboBox1.Items.Add(name);
            }
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.SelectedIndex = 0;

            // Combo box 2
            RefreshSerialPortList();
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.SelectedIndex = 0;

            // Haptic feedback starts disabled
            checkBox1.Enabled = false;


            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            cap = new Capture(2);
            cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 1080.0);
            cap.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 1920.0);
            Application.Idle += new EventHandler(showFromCam);
            

            
        }


        // ROI RECEIVER CODE -----------------------------
        private void IntiailizeROIReceiver()
        {
            roiRec = null;
            roiRec = new ROIStreaming.ROIReceiver(8000);
            roiRec.NewROIsReceived += roiRec_NewROIsReceived;

            if (roiRec != null) Console.WriteLine("RoiReceiver initialized!");
        }

        void roiRec_NewROIsReceived(List<Tuple<string, Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>>> newROIs)
        {
            if (newROIs.Count > 0)
            {
                HandleROI(newROIs[0].Item1, newROIs[0].Item2);
            }
        }

        private void HandleROI(string p, Image<Bgr, byte> image)
        {
            tracker = new Tracker(image);
        }
        // END ROI RECEIVER CODE ---------------------------------

        private void imageBox1_Click(object sender, EventArgs e)
        {

        }

        public void setImage(Image<Bgr, byte> img)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Return if nothing selected.
            if ((string)comboBox1.SelectedItem == "") return;

            tracker = new Tracker(
                string.Format("itemsToTrack/{0}.jpg", comboBox1.SelectedItem)
                );
        }

        void showFromCam(object sender, EventArgs e)
        {
            image = null;
            while (image == null) image = cap.QueryFrame();
            Image<Bgr, Byte> returnimage = image;
            if (tracker != null)
            {
                try
                {
                    returnimage = tracker.process(image);
                }
                catch (Exception exception)
                {

                }

                // Send to Arduino
                if (checkBox1.Checked)
                {
                    try
                    {
                        arduino.Process(tracker.roi, tracker.centerOfObject, new PointF(pictureBox1.Width / 2, pictureBox1.Height / 2));
                    }
                    // COM Port died.
                    catch (System.IO.IOException ioException)
                    {
                        Console.WriteLine(ioException.Message);
                        DestroySerial();
                    }
                }
            }
            pictureBox1.Image = returnimage.ToBitmap();

            
            // Speech
            if (checkBox2.Checked)
            {
                // Get direction to force in
                int direction = Tracker.findDirection(tracker.centerOfObject, new PointF(pictureBox1.Width / 2, pictureBox1.Height / 2));

                switch (direction)
                {
                    case 0:
                        speechWorker.setDirection("right");
                        break;
                    case 1:
                        speechWorker.setDirection("up");
                        break;
                    case 2:
                        speechWorker.setDirection("left");
                        break;
                    case 3:
                        speechWorker.setDirection("down");
                        break;
                    case -1:
                        speechWorker.setDirection("");
                        break;
                }
            }

            if (checkBox3.Checked)
            {
                // Get direction to force in
                int direction = Tracker.findDirection(tracker.centerOfObject, new PointF(pictureBox1.Width / 2, pictureBox1.Height / 2));

                switch (direction)
                {
                    case 0:
                        label5.Text = "right";
                        break;
                    case 1:
                        label5.Text = "up";
                        break;
                    case 2:
                        label5.Text = "left";
                        break;
                    case 3:
                        label5.Text = "down";
                        break;
                    case -1:
                        label5.Text = "";
                        break;
                }

            }

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked) tracker.trackWithCMT = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked) tracker.trackWithCMT = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tracker = null;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            arduino = new Arduino((string)comboBox2.SelectedItem);
            checkBox1.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DestroySerial();
        }

        private void RefreshSerialPortList()
        {
            // Combo box 2 - Arduino selection
            comboBox2.Items.Clear();
            comboBox2.Items.AddRange(SerialPort.GetPortNames());
        }

        private void button6_Click(object sender, EventArgs e)
        {
            RefreshSerialPortList();
        }

        private void DestroySerial()
        {
            arduino = null;
            checkBox1.Checked = false;
            checkBox1.Enabled = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                speechWorker = new AsyncSpeechWorker();
                speechThread = new Thread(speechWorker.doWork);
                speechThread.Start();
                // Wait
                while(!speechThread.IsAlive);
            }
            else
            {
                speechWorker.requestStop();
                speechThread.Join();
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            label5.Visible = checkBox3.Checked;
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = !checkBox4.Checked;
            button1.Enabled = !checkBox4.Checked;
            button2.Enabled = !checkBox4.Checked;
        }

    }

}
