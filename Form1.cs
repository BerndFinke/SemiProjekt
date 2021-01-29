using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SemiProjekt.Classes;
using System.Configuration;

namespace SemiProjekt
{
    public partial class Form1 : Form
    {

        List<HumanDataBase> Humans = new List<HumanDataBase>();
        private double MeterPerPanelPixel = Convert.ToDouble(ConfigurationManager.AppSettings["MeterPerPanelPixel"]);
        private int NumOfHumans = Convert.ToInt32(ConfigurationManager.AppSettings["AnzahlPersonen"]); 
        private double MaxSpeed = Convert.ToDouble(ConfigurationManager.AppSettings["MaxSpeed"]);
        private double MinSpeed = Convert.ToDouble(ConfigurationManager.AppSettings["MinSpeed"]);
        private double MaxRadius = Convert.ToDouble(ConfigurationManager.AppSettings["MaxRadius"]);
        private double MinRadius = Convert.ToDouble(ConfigurationManager.AppSettings["MinSpeed"]);
        private double MaxDestinationRadius = Convert.ToDouble(ConfigurationManager.AppSettings["MaxDestinationRadius"]);

        bool bShowDiestinations = false;
        bool bShowRadiusFar = true;
        bool bShowRadiusNear = true;
        bool bShowMaxDestRadius = false;



        bool IsInitiated = false;


        public Form1()
        {
            InitializeComponent();


            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            this.Height = Convert.ToInt32(ConfigurationManager.AppSettings["ScreenY"]);
            this.Width = Convert.ToInt32(ConfigurationManager.AppSettings["ScreenX"]);

            double panelWidth = this.Size.Width * MeterPerPanelPixel;
            double panelHeight = this.Size.Height * MeterPerPanelPixel;

            HumanDataBase human = new HumanDataBase(panelWidth, panelHeight, MinSpeed, MaxSpeed, MaxRadius, MinRadius, MaxDestinationRadius);
            Humans.Add(human);

            timer1.Enabled = true;
            timer1.Start();

        }


        private void timer1_Tick(object sender, EventArgs e)
        {

            double panelWidth = this.Size.Width * MeterPerPanelPixel;
            double panelHeight = this.Size.Height * MeterPerPanelPixel;

            if (Humans.Count < NumOfHumans)
            {
                HumanDataBase human = new HumanDataBase(panelWidth, panelHeight, MinSpeed, MaxSpeed, MaxRadius, MinRadius, MaxDestinationRadius);
                Humans.Add(human);
            }
            else if (IsInitiated == false)
            {
                Random rnd = new Random();
                int idx = rnd.Next(0, Humans.Count);
                Humans[idx].SetInfection();
                IsInitiated = true;
            }

            int healthy = 0;
            int infected = 0;
            int opensic = 0;
            OutputTxT.Text = "";
            foreach (HumanDataBase human in Humans)
            {
                HumanDataBase.InfectionStat stat = human.Go();
                switch( stat)
                {
                    case HumanDataBase.InfectionStat.Healthy:
                        healthy++;
                        break;

                    case HumanDataBase.InfectionStat.Infected:
                        infected++;
                        break;

                    case HumanDataBase.InfectionStat.OpenSick:
                        opensic++;
                        break;

                }
            }

            label1.Text = "Gesund: " + healthy.ToString();
            label2.Text = "Infiziert: " + infected.ToString();
            label3.Text = "Krank: " + opensic.ToString();


            this.Invalidate();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = this.CreateGraphics();
            // Make a big red pen.
            Pen pred = new Pen(Color.Red, 1);
            Pen pgrn = new Pen(Color.Green, 1);

            System.Drawing.Graphics formGraphics;
            formGraphics = this.CreateGraphics();

            Dictionary<string, HumanDataBase.Status> HumanDict = Humans.FirstOrDefault().GetCollection();
            foreach (KeyValuePair<string, HumanDataBase.Status> stat in HumanDict)
            {
                //float panelX = (float)(stat.Value.CurrentPos.X / MeterPerPanelPixel);
                //float panelY = (float)(stat.Value.CurrentPos.Y / MeterPerPanelPixel);
                //float radius = (float)(0.25 / (MeterPerPanelPixel));

                //g.DrawEllipse(p, panelX - radius, panelY - radius, radius + radius, radius + radius);

                int ipanelX = (int)(stat.Value.CurrentPos.X / MeterPerPanelPixel);
                int ipanelY = (int)(stat.Value.CurrentPos.Y / MeterPerPanelPixel);
                int iradius = (int)(0.25 / (MeterPerPanelPixel)) < 3 ? 3 : (int)(0.25 / (MeterPerPanelPixel));

                int iarrowX = (int)(Math.Cos(stat.Value.Angle / 360 * 2 * Math.PI) * 10);
                int iarrowY = (int)(Math.Sin(stat.Value.Angle / 360 * 2 * Math.PI) * 10);

                System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Gray);
                switch (stat.Value.InfectionStatus)
                {
                    case HumanDataBase.InfectionStat.Healthy:
                        myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Green);
                        break;

                    case HumanDataBase.InfectionStat.Infected:
                        myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow);
                        break;

                    case HumanDataBase.InfectionStat.OpenSick:
                        myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
                        break;

                    case HumanDataBase.InfectionStat.Excluded:
                        myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
                        break;
                }


                formGraphics.FillEllipse(myBrush, new Rectangle(ipanelX - iradius, ipanelY - iradius, iradius + iradius, iradius + iradius));
                if (iarrowX > -20 && iarrowX < 20 && iarrowY > -20 && iarrowY < 20)
                    formGraphics.DrawLine(pgrn, new Point(ipanelX, ipanelY), new Point(ipanelX + iarrowX, ipanelY + iarrowY));

                if (bShowDiestinations)
                {
                    int ipanel2X = (int)(stat.Value.Destination.X / MeterPerPanelPixel);
                    int ipanel2Y = (int)(stat.Value.Destination.Y / MeterPerPanelPixel);

                    if (iarrowX > -20 && iarrowX < 20 && iarrowY > -20 && iarrowY < 20)
                        formGraphics.DrawLine(pgrn, new Point(ipanelX, ipanelY), new Point(ipanel2X, ipanel2Y));
                }

                if (bShowRadiusFar)
                {
                    int rfradius = (int)(MaxRadius / (MeterPerPanelPixel));
                    formGraphics.DrawEllipse(pgrn, new Rectangle(ipanelX - rfradius, ipanelY - rfradius, rfradius + rfradius, rfradius + rfradius));
                }

                if (bShowRadiusNear)
                {
                    int rnradius = (int)(MinRadius / (MeterPerPanelPixel));
                    formGraphics.DrawEllipse(pred, new Rectangle(ipanelX - rnradius, ipanelY - rnradius, rnradius + rnradius, rnradius + rnradius));
                }

                if (bShowMaxDestRadius)
                {
                    int ipanel3X = (int)(stat.Value.Origin.X / MeterPerPanelPixel);
                    int ipanel3Y = (int)(stat.Value.Origin.Y / MeterPerPanelPixel);
                    int rnradius = (int)(MaxDestinationRadius / (MeterPerPanelPixel));
                    formGraphics.DrawEllipse(pred, new Rectangle(ipanel3X - rnradius, ipanel3Y - rnradius, rnradius + rnradius, rnradius + rnradius));
                }
            }

            base.OnPaint(e);
        }


        private void Form1_ResizeEnd(object sender, EventArgs e)
        {

        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {


        }

    }
}
