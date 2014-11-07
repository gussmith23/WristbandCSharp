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

namespace WristbandCsharp
{
    public partial class Form1 : Form
    {

        Capture cap;
        Image<Bgr,Byte> image;
        Tracker tracker = null;
        Arduino arduino;
        Boolean tracking = false;

        public Form1()
        {

            InitializeComponent();

            // Combo box 1
            string[] itemNames = Directory.GetFiles("itemsToTrack/", "*.jpg");
            foreach (string s in itemNames)
            {
                string name = System.Text.RegularExpressions.Regex.Replace(s, "itemsToTrack/", "");
                name = System.Text.RegularExpressions.Regex.Replace(name, ".jpg", "");
                comboBox1.Items.Add(name);
            }
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;

            // Combo box 2
            RefreshSerialPortList();

            // Haptic feedback starts disabled
            checkBox1.Enabled = false;


            cap = new Capture();
            Application.Idle += new EventHandler(showFromCam);
            

            
        }

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
            if (comboBox1.SelectedItem == "") return;

            tracker = new Tracker(
                string.Format("itemsToTrack/{0}.jpg", comboBox1.SelectedItem)
                );
        }

        void showFromCam(object sender, EventArgs e)
        {
            image = cap.QueryFrame();
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

    }

}
