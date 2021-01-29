using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace SemiProjekt.Classes
{
    class HumanDataBase
    {
        public struct Position
        {
            public double X;
            public double Y;
        }

        public struct Status
        {
            public Position Origin;
            public Position CurrentPos;
            public Position Destination;
            public double Speed;
            public double Angle;
            public double StopRadius;
            public double StopAngle;
            public double InfectionLevel;
            public double RecvInfections;
            public DateTime InfectionTimeStamp;
            public InfectionStat InfectionStatus;
        }

        public struct Configuration
        {
            public double MaxDestinationRadius;
            public double MaxSpeed;
            public double MinSpeed;
            public double Speed;
            public double RadiusFar;
            public double RadiusNear;
            public double Acceleration;
            public double DeltaAngel;
            public double WideAngel;
            public double InfectRadius;
            public double IncarnationTime;


        }

        public enum InfectionStat
        {
            Healthy = 0,
            Infected = 1,
            Infecting = 2,
            OpenSick = 3,
            Excluded = 4
        }
            
            
            
            


        private string HumanID;
        private double maxX;
        private double maxY;
        private double TimeDelay;
        private DateTime TimeStamp;
        private DateTime LastMovTime;

        private Status hStatus = new Status();
        private Configuration iConfig = new Configuration();

        public static Dictionary<string, Status> HumanCollection = new Dictionary<string, Status>();

        public Dictionary<string, Status> GetCollection()
        {
            return HumanCollection;
        }

        public HumanDataBase(double MaxX, double MaxY, double MinSpeed, double MaxSpeed, double RadiusFar, double RadiusNear, double MaxDestinationRadius)
        {
            var guid = Guid.NewGuid();
            HumanID = guid.ToString();

            maxX = MaxX;
            maxY = MaxY;
            TimeStamp = DateTime.Now;

            Random rnd = new Random();

            iConfig.MaxDestinationRadius = MaxDestinationRadius;
            iConfig.MaxSpeed = MaxSpeed;
            iConfig.MinSpeed = MinSpeed;
            iConfig.Speed = Convert.ToDouble(rnd.Next(Convert.ToInt32(iConfig.MinSpeed) * 1000, Convert.ToInt32(iConfig.MaxSpeed) * 1000)) / 1000;

            iConfig.RadiusFar = RadiusFar;
            iConfig.RadiusNear = RadiusNear;
            iConfig.Acceleration = Convert.ToDouble(rnd.Next(100, 500))/1000 ;
            iConfig.DeltaAngel = 5;
            iConfig.WideAngel = 30;
            iConfig.InfectRadius = 2.0;
            iConfig.IncarnationTime = 5;


            hStatus.CurrentPos.X = rnd.Next(0, Convert.ToInt32(maxX));
            hStatus.CurrentPos.Y = rnd.Next(0, Convert.ToInt32(maxY));

            hStatus.Origin.X = hStatus.CurrentPos.X;
            hStatus.Origin.Y = hStatus.CurrentPos.Y;

            //hStatus.Destination.X = rnd.Next(0, Convert.ToInt32(maxX));
            //hStatus.Destination.Y = rnd.Next(0, Convert.ToInt32(maxY));
            UpdateDestination();

            hStatus.Angle = rnd.Next(-180, 180);
            hStatus.Speed = 0;
            hStatus.InfectionStatus = InfectionStat.Healthy;
            hStatus.InfectionLevel = 0;
            hStatus.RecvInfections = 0;
            hStatus.InfectionTimeStamp = DateTime.Now;

            //double MinDist = Math.Pow(iConfig.Speed, 2) / (2 * iConfig.Acceleration) + 0.03;
            //if (iConfig.RadiusFar < MinDist)
            //    iConfig.RadiusFar = (float)MinDist;


            Status humanstat = new Status();

            HumanCollection.Add(HumanID, humanstat);

            return;
        }

        public void SetInfection()
        {
            if(hStatus.RecvInfections < 10)
                hStatus.RecvInfections = 10;
        }
 
        public InfectionStat Go()
        {
            TimeDelay = (DateTime.Now - TimeStamp).TotalMilliseconds;
            TimeStamp = DateTime.Now;
            
            if (TimeDelay < 1000)
            {
                #region Berechung Richtung - Winkel
                UpdateAngel();
                #endregion

                #region Berechnung Geschwindigkeit
                UpdateSpeed();
                #endregion

                #region Berechnung neue Position - Laufen 
                UpdatePosition();
                #endregion

                UpdateInfection();

                InfectOthers();

                UpdateCollection();

            }

            return hStatus.InfectionStatus;
        }

        public void UpdateAngel()
        {
            double DeltaX;
            double DeltaY;
            double Radius;
            double Angel;

            //Personen im Umkreis finden
            double lastrad = iConfig.RadiusFar;
            foreach (KeyValuePair<string, HumanDataBase.Status> stat in HumanCollection)
            {
                if (stat.Key != HumanID)
                {
                    DeltaX = stat.Value.CurrentPos.X - hStatus.CurrentPos.X;
                    DeltaY = stat.Value.CurrentPos.Y - hStatus.CurrentPos.Y;
                    Radius = Math.Sqrt(Math.Pow(DeltaX, 2) + Math.Pow(DeltaY, 2));
                    Angel = Math.Acos(DeltaX / Radius) * 180 / Math.PI;
                    if (DeltaY < 0)
                        Angel *= -1;

                    if (Radius < lastrad) //wenn die Person näher ist als die vorherige - neue Person merken
                    {

                        double DiffAngle = GetAngleDiffBetween(hStatus.Angle, Angel);
                        double RelRadius = 100 * (iConfig.RadiusFar - Radius) / (iConfig.RadiusFar - iConfig.RadiusNear);

                        if (Math.Abs(DiffAngle) < 30)
                        {
                            //Ziel befindet sich von aktueller Richtung links 
                            if (DiffAngle < 0)
                            {
                                //hStatus.Angle += iConfig.WideAngel;
                                hStatus.Angle = DiffAngle + RelRadius;
                                if (hStatus.Angle > 180)
                                    hStatus.Angle = hStatus.Angle - 360;
                            }
                            else //Ziel ist rechts vorn vor mir  
                            {
                                //hStatus.Angle = hStatus.Angle - iConfig.WideAngel;
                                hStatus.Angle = DiffAngle - RelRadius;
                                if (hStatus.Angle < -180)
                                    hStatus.Angle = hStatus.Angle + 360;
                            }
                            lastrad = Radius;
                            hStatus.StopRadius = Radius;
                            hStatus.StopAngle = Angel;

                        }
                        else if (Math.Abs(DiffAngle) < 90)
                        {
                            //Ziel befindet sich von aktueller Richtung links 
                            if (DiffAngle < 0)
                            {
                                //hStatus.Angle += iConfig.DeltaAngel;
                                hStatus.Angle = DiffAngle + RelRadius;
                                if (hStatus.Angle > 180)
                                    hStatus.Angle = hStatus.Angle - 360;
                            }
                            else //Ziel ist rechts vorn vor mir  
                            {
                                //hStatus.Angle = hStatus.Angle - iConfig.DeltaAngel;
                                hStatus.Angle = DiffAngle - RelRadius;
                                if (hStatus.Angle < -180)
                                    hStatus.Angle = hStatus.Angle + 360;
                            }
                            lastrad = Radius;
                            hStatus.StopRadius = Radius;
                            hStatus.StopAngle = Angel;
                        }


                    }
                }
            }

            //wenn nichts im Weg ist den Winkel zum Ziel berechnen
            if (lastrad >= iConfig.RadiusFar)
            {
                DeltaX = hStatus.Destination.X - hStatus.CurrentPos.X;
                DeltaY = hStatus.Destination.Y - hStatus.CurrentPos.Y;
                Radius = Math.Sqrt(Math.Pow(DeltaX, 2) + Math.Pow(DeltaY, 2));
                if (Radius < iConfig.RadiusFar)
                    hStatus.StopRadius = iConfig.RadiusFar + 1;
                else
                    hStatus.StopRadius = Radius;


                double DestAngle = Math.Acos(DeltaX / Radius) * 180 / Math.PI;
                if (DeltaY < 0)
                    DestAngle *= -1;

                double DiffAngle = GetAngleDiffBetween(hStatus.Angle, DestAngle);
                hStatus.StopAngle = DestAngle;

                //Ziel befindet sich von aktueller Richtung links 
                if (DiffAngle < 0)
                {
                    if (Math.Abs( DiffAngle) > iConfig.DeltaAngel)
                        hStatus.Angle = hStatus.Angle - iConfig.DeltaAngel;
                    else
                        hStatus.Angle = DestAngle;

                    if (hStatus.Angle < -180)
                        hStatus.Angle = hStatus.Angle + 360;
                }
                else if (DiffAngle > 0) //Ziel ist rechts vorn vor mir  
                {
                    if (Math.Abs(DiffAngle) > iConfig.DeltaAngel)
                        hStatus.Angle = hStatus.Angle + iConfig.DeltaAngel;
                    else
                        hStatus.Angle = DestAngle;

                    if (hStatus.Angle > 180)
                        hStatus.Angle = hStatus.Angle - 360;
                }


            }

            return;
        }


        private void UpdateDestination()
        {
            
            iConfig.Speed = Convert.ToDouble(RandomClass.Between(Convert.ToInt32(iConfig.MinSpeed) * 1000, Convert.ToInt32(iConfig.MaxSpeed) * 1000)) / 1000;
            iConfig.Acceleration = Convert.ToDouble(RandomClass.Between(100, 500)) / 1000;

            //Berechnung des neuen Ziels unter Beachtung der maximalen Bewegungsfreiheit MaxDestinationRadius
            double diffX = Convert.ToDouble(RandomClass.Between(Convert.ToInt32(iConfig.MaxDestinationRadius * -1000), Convert.ToInt32(iConfig.MaxDestinationRadius * 1000))) / 1000;
            double diffY = Convert.ToDouble(RandomClass.Between(Convert.ToInt32(iConfig.MaxDestinationRadius * -1000), Convert.ToInt32(iConfig.MaxDestinationRadius * 1000))) / 1000;

            double Radius = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2));
            if (Radius > iConfig.MaxDestinationRadius)
            {
                diffX = diffX * iConfig.MaxDestinationRadius / Radius;
                diffY = diffY * iConfig.MaxDestinationRadius / Radius;
            }

            hStatus.Destination.X = hStatus.Origin.X + diffX;
            hStatus.Destination.Y = hStatus.Origin.Y + diffY;

            if (hStatus.Destination.X > maxX) hStatus.Destination.X = maxX - iConfig.RadiusNear;
            if (hStatus.Destination.X < 0) hStatus.Destination.X = iConfig.RadiusNear;
            if (hStatus.Destination.Y > maxY) hStatus.Destination.Y = maxY - iConfig.RadiusNear;
            if (hStatus.Destination.Y < 0) hStatus.Destination.Y = iConfig.RadiusNear;

            double MinDist = Math.Pow(hStatus.Speed, 2) / (2 * iConfig.Acceleration) + 0.03;

        }

        public double UpdateSpeed()
        {

            if (hStatus.StopRadius < iConfig.RadiusNear && Math.Abs(GetAngleDiffBetween(hStatus.Angle, hStatus.StopAngle)) < 90 )
            {
                hStatus.Speed = 0;
            }
            else
            {
                double DeltaX = hStatus.Destination.X - hStatus.CurrentPos.X;
                double DeltaY = hStatus.Destination.Y - hStatus.CurrentPos.Y;
                double Radius = Math.Sqrt(Math.Pow(DeltaX, 2) + Math.Pow(DeltaY, 2));

                if ((hStatus.StopRadius < iConfig.RadiusFar && hStatus.Speed > 0) || (Radius < iConfig.RadiusFar && hStatus.Speed > 0))
                {
                    hStatus.Speed -= (iConfig.Acceleration * TimeDelay / 1000);
                }

                if (hStatus.StopRadius > iConfig.RadiusFar && Radius > iConfig.RadiusFar && hStatus.Speed < iConfig.Speed)
                {
                    hStatus.Speed += (iConfig.Acceleration * TimeDelay / 1000);
                }
            }

            if (hStatus.Speed < 0)
                hStatus.Speed = 0;

            if (hStatus.Speed > iConfig.Speed)
                hStatus.Speed = iConfig.Speed; 

            return hStatus.Speed;
        }

        public void UpdatePosition()
        {
            if (hStatus.Speed == 0)
            {
                double StopTime = (DateTime.Now - LastMovTime).TotalSeconds;
                if ((hStatus.Destination.X < hStatus.CurrentPos.X + 0.02 && hStatus.Destination.X > hStatus.CurrentPos.X - 0.02) || (StopTime > 20))
                {
                    UpdateDestination();
                    LastMovTime = DateTime.Now;
                }
            }
            else
            {
                double DeltaRadius = TimeDelay * hStatus.Speed / 1000;
                double DeltaX = DeltaRadius * Math.Cos(hStatus.Angle / 360 * 2 * Math.PI);
                double DeltaY = DeltaRadius * Math.Sin(hStatus.Angle / 360 * 2 * Math.PI);

                double DistX = hStatus.Destination.X - hStatus.CurrentPos.X;
                double DistY = hStatus.Destination.Y - hStatus.CurrentPos.Y;
                double DstRad = Math.Sqrt(Math.Pow(DistX, 2) + Math.Pow(DistY, 2));


                if (DstRad > DeltaRadius)
                {
                    hStatus.CurrentPos.X = hStatus.CurrentPos.X + DeltaX;
                    hStatus.CurrentPos.Y = hStatus.CurrentPos.Y + DeltaY;
                }
                else
                {
                    hStatus.CurrentPos.X = hStatus.CurrentPos.X + DistX;
                    hStatus.CurrentPos.Y = hStatus.CurrentPos.Y + DistY;
                }
                LastMovTime = DateTime.Now;
            }

        }


        public string StatusTxt()
        {
            string text = string.Format("X:{0} Y:{1} Dest({2}/{3} Speed {4} Angel {5}", 
                Convert.ToInt32(hStatus.CurrentPos.X),
                Convert.ToInt32(hStatus.CurrentPos.Y),
                Convert.ToInt32(hStatus.Destination.X),
                Convert.ToInt32(hStatus.Destination.Y), 
                hStatus.Speed, 
                hStatus.Angle);
            return text;
            
        }

        protected void UpdateCollection()
        {
            hStatus.RecvInfections = HumanCollection[HumanID].RecvInfections;

            Status tempstat = new Status();  //HumanCollection[HumanID];
            tempstat.CurrentPos.X = hStatus.CurrentPos.X;
            tempstat.CurrentPos.Y = hStatus.CurrentPos.Y;
            tempstat.Destination.X = hStatus.Destination.X;
            tempstat.Destination.Y = hStatus.Destination.Y;
            tempstat.Speed = hStatus.Speed;
            tempstat.Angle = hStatus.Angle;
            tempstat.Origin.X = hStatus.Origin.X;
            tempstat.Origin.Y = hStatus.Origin.Y;
            tempstat.InfectionLevel = hStatus.InfectionLevel;
            tempstat.InfectionStatus = hStatus.InfectionStatus;
            tempstat.InfectionTimeStamp = hStatus.InfectionTimeStamp;
           
            HumanCollection[HumanID] = tempstat;

            return;

        }

        public void InfectOthers()
        {
            double DeltaX;
            double DeltaY;
            double Radius;

            //Personen im Umkreis finden

            if (hStatus.InfectionStatus != InfectionStat.Healthy)
            {
                double lastrad = iConfig.RadiusFar;

                for(int idx = 0;idx < HumanCollection.Count;idx++)
                {
                    KeyValuePair<string, HumanDataBase.Status> stat = HumanCollection.ElementAt(idx);
                    if (stat.Key != HumanID)
                    {
                        DeltaX = stat.Value.CurrentPos.X - hStatus.CurrentPos.X;
                        DeltaY = stat.Value.CurrentPos.Y - hStatus.CurrentPos.Y;
                        Radius = Math.Sqrt(Math.Pow(DeltaX, 2) + Math.Pow(DeltaY, 2));

                        if (Radius < iConfig.InfectRadius) //wenn die Person näher ist als die vorherige - neue Person merken
                        {
                            double RelRadius = (iConfig.InfectRadius - Radius) / (iConfig.InfectRadius);
                            Status tempstat = new Status();
                            tempstat = HumanCollection[stat.Key];
                            tempstat.RecvInfections += (RelRadius * hStatus.InfectionLevel);
                            HumanCollection[stat.Key] = tempstat;

                        }
                    }
                }
            }


        }

        public void UpdateInfection()
        {
            switch(hStatus.InfectionStatus)
            {
                case InfectionStat.Healthy:

                    if(hStatus.RecvInfections > 0 && hStatus.InfectionLevel == 0)
                    {
                        hStatus.InfectionTimeStamp = DateTime.Now;
                        hStatus.InfectionStatus = InfectionStat.Infected;
                        hStatus.InfectionLevel = hStatus.RecvInfections;
                    }
                    break;

                case InfectionStat.Infected:

                    if (hStatus.RecvInfections > 0)
                    {
                        hStatus.InfectionLevel = hStatus.InfectionLevel + hStatus.RecvInfections;
                    }

                    if (hStatus.InfectionLevel > 0)
                    {
                        hStatus.InfectionLevel = hStatus.InfectionLevel + 1;
                    }

                    //if (hStatus.InfectionLevel > 50  && (DateTime.Now - hStatus.InfectionTimeStamp).TotalDays >= iConfig.IncarnationTime)
                    if (hStatus.InfectionLevel > 50 && (DateTime.Now - hStatus.InfectionTimeStamp).TotalMinutes >= iConfig.IncarnationTime)
                    {
                            hStatus.InfectionStatus = InfectionStat.OpenSick;
                    }

                    break;

                case InfectionStat.OpenSick:

                    if (hStatus.RecvInfections > 0)
                    {
                        hStatus.InfectionLevel = hStatus.InfectionLevel + hStatus.RecvInfections;
                    }

                    if (hStatus.InfectionLevel > 0)
                    {
                        hStatus.InfectionLevel = hStatus.InfectionLevel - 1;
                    }
                    break;

                case InfectionStat.Excluded:


                    break;

            }

        }

        private double GetAngleDiffBetween(double CurrAngle, double OtherAngle )
        {
            double NormalizedCurrentAngle = CurrAngle >= 0 ? CurrAngle : CurrAngle + 360;
            double NormalizedOtherAngle = OtherAngle >= 0 ? OtherAngle : OtherAngle + 360;
            double DiffAngle = NormalizedOtherAngle - NormalizedCurrentAngle;

            if (DiffAngle > 180)
                DiffAngle = DiffAngle - 360;

            if (DiffAngle < -180)
                DiffAngle = DiffAngle + 360;

            return DiffAngle;
        }


    }
}
