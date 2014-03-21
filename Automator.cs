using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PractiseVisualizer
{

    public class Automator
    {
        public double ChangeRowProbability {get; set;}
        public int RowCount { get; set; }
        public int RoadLength { get; set; }
        public int MaxSpeed { get; set; }
        public int BusLength { get; set; }
        public int CarLength { get; set; }
        public int MaxBusCapacity { get; set; }
        public int MaxCarCapacity { get; set; }
        public int AverageTimeOnStation { get; set; }
        public int MaxAcceleration { get; set; }
                
        public double NewVehicleProbability { get; set; }
        public double BusQuota { get; set; }
       
        public bool IsTrafficLightGreen { get; set; }
        public bool IsTrafficLightGreenAtEnd { get; set; }
        public int GreenInterval { get; set; }
        public int RedInterval { get; set; }
        public int GreenIntervalAtEnd { get; set; }
        public int RedIntervalAtEnd { get; set; }
        Cell[,] mRoad;
        Cell[,] mRoad2;
        public Cell[,] Cells
        {
            get
            {
                return mRoad;
            }
        }
        Random random;
        int timeStep;
        int intervalDuration;
        int intervalDurationAtEnd;
        public void Init()
        {
            mRoad = new Cell[RowCount, RoadLength];
            mRoad2 = new Cell[RowCount, RoadLength];
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < RoadLength; j++)
                {
                    mRoad[i, j] = new Cell();
                    mRoad2[i, j] = new Cell();
                }
            }
            IsTrafficLightGreen = true;
            IsTrafficLightGreenAtEnd = false;
            EnableTrafficLightAtIndex(RoadLength - 1, true);
            random = new Random();            
        }
        public void Iterate()
        {            
            FillFirstColumn();
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = RoadLength-1; j >=0; j--)
                {
                    if (mRoad[i, j].Type == AutoType.Trouble)
                        mRoad2[i, j].Type = AutoType.Trouble;
                    if (!mRoad[i, j].isFirst || j + mRoad[i,j].Speed >= RoadLength)
                    {
                        continue; 
                    }
                    var forwardAutoIndex = SearchForwardAuto(i, j);
                    int speed = SanitizeSpeed(i, j, forwardAutoIndex);
                    var forwardAutoIndexLeft = 1; 
                    var backAutoIndexLeft = 1;
                    int speedLeft = 0;
                    int speedRight = 0;
                    int backSpeedLeft = 0;
                    int backSpeedRight = 0;
                    bool canMoveLeft = false;
                    if (i > 0)
                    {
                        forwardAutoIndexLeft = SearchForwardAuto(i - 1, j);
                        backAutoIndexLeft = SearchBackAuto(i - 1, j);
                        
                            speedLeft = SanitizeSpeed(i - 1, j, forwardAutoIndexLeft);
                        
                            backSpeedLeft = SanitizeSpeed(i - 1, backAutoIndexLeft, j);
                        
                    }
                    var forwardAutoIndexRight = 1;
                    var backAutoIndexRight = 1;
                    bool canMoveRight = false;
                    if (i < RowCount - 1)
                    {
                        forwardAutoIndexRight = SearchForwardAuto(i + 1, j);
                        backAutoIndexRight = SearchBackAuto(i + 1, j);
                        
                            speedRight = SanitizeSpeed(i + 1, j, forwardAutoIndexRight);
                        
                            backSpeedRight = SanitizeSpeed(i + 1, backAutoIndexRight, j);
                    }
                    
                    
                    
                    int len = mRoad[i, j].Type == AutoType.Bus ? BusLength : CarLength;
                    mRoad2[i, j + mRoad[i,j].Speed].isFirst = true;
                    mRoad2[i, j + mRoad[i, j].Speed].Type = mRoad[i, j].Type;
                    mRoad2[i, j + mRoad[i, j].Speed].Speed = speed;
                    for (int k = j - 1; k >= j - len + 1; k--)
                    {
                        mRoad2[i, k + mRoad[i, j].Speed].Type = mRoad[i, j].Type;
                        mRoad2[i, k + mRoad[i, j].Speed].Speed = speed;
                    }
                }
            }
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < RoadLength; j++)
                {
                    mRoad[i, j].isFirst = mRoad2[i, j].isFirst;
                    mRoad[i, j].Speed = mRoad2[i, j].Speed;
                    mRoad[i, j].Type = mRoad2[i, j].Type;
                    mRoad2[i,j] = new Cell();
                }
            }
            NextTime();
            
        }

        void NextTime()
        {
            timeStep++;
            intervalDuration++;
            if (IsTrafficLightGreen && intervalDuration > GreenInterval)
            {
                intervalDuration = 0;
                IsTrafficLightGreen = false;
            }
            else if (!IsTrafficLightGreen && intervalDuration > RedInterval)
            {
                intervalDuration = 0;
                IsTrafficLightGreen = true;
            }
            intervalDurationAtEnd++;
            if (IsTrafficLightGreenAtEnd && intervalDurationAtEnd > GreenIntervalAtEnd)
            {
                intervalDurationAtEnd = 0;
                IsTrafficLightGreenAtEnd = false;
                EnableTrafficLightAtIndex(RoadLength - 1, true);
            }
            else if (!IsTrafficLightGreenAtEnd && intervalDurationAtEnd > RedIntervalAtEnd)
            {
                intervalDurationAtEnd = 0;
                IsTrafficLightGreenAtEnd = true;
                EnableTrafficLightAtIndex(RoadLength - 1, false);
            } 
        }

        void EnableTrafficLightAtIndex(int j,bool enabled)
        {
            //FIXME: possible bug when there is already a auto
            for (int i = 0; i < RowCount; i++)
            {
                if (enabled)                
                    mRoad[i, j].Type = AutoType.Trouble;                
                else
                    mRoad[i, j].Type = AutoType.None;
            }
        }

        int SanitizeSpeed(int i, int j, int forwardAutoIndex)
        {
            if (j == -1)
            {
                return Math.Min(MaxSpeed, mRoad[i, forwardAutoIndex].Speed + MaxAcceleration);
            }
            int speed = Math.Min(MaxSpeed, mRoad[i, j].Speed + MaxAcceleration);
            if (forwardAutoIndex != -1)
            {
                speed = GetSpeed(forwardAutoIndex - j - 1, mRoad[i, j].Speed, mRoad[i, forwardAutoIndex].Speed);
                if (speed > mRoad[i, j].Speed + MaxAcceleration || speed > MaxSpeed)
                    speed = Math.Min(MaxSpeed, mRoad[i, j].Speed + MaxAcceleration);
            }
            return speed;
        }
        
        int GetSpeed(int distance, int speed, int speedOfForwardAuto)
        {
            int sum = ((speedOfForwardAuto + speedOfForwardAuto % MaxAcceleration) * (speedOfForwardAuto/2 + 1)) / 2 + distance;
            int t = speed%MaxAcceleration;
            double sqr = Math.Sqrt(Math.Pow(MaxAcceleration+t,2)-4*MaxAcceleration*(t-2*sum));
            int maxSafeSeed = (int)Math.Floor((sqr-MaxAcceleration-t)/2);
            if (maxSafeSeed <= speed)
            {
                return Math.Max(maxSafeSeed - MaxAcceleration,0);
            }
            else
            {
                return Math.Min(speed+MaxAcceleration,maxSafeSeed);
            }            
        }
        int SearchBackAuto(int i, int j)
        {
            for (int k = j - 1; k >= 0; k--)
            {
                if (mRoad[i, k].Type == AutoType.Car || mRoad[i,k].Type == AutoType.Bus)
                    return k;
            }
            return -1;
        }
        int SearchForwardAuto(int i, int j)
        {                       
            for (int k = j + 1; k < RoadLength; k++)
            {
                if (mRoad[i, k].Type != AutoType.None)
                    return k;
            }
            return -1;
        }
        
        void FillFirstColumn()
        {
            if (!IsTrafficLightGreen)
                return;
            for (int i = 0; i < RowCount; i++)
            {
                if (mRoad[i, BusLength-1].Type == AutoType.None)
                {   
                        var p = random.NextDouble();
                        if (p < NewVehicleProbability * BusQuota)
                        {
                            mRoad[i, BusLength-1].Type = AutoType.Bus;
                            mRoad[i, BusLength-1].isFirst = true;
                            for (int j = BusLength - 2; j >= 0; j--)
                            {
                                mRoad[i, j].Type = AutoType.Bus;
                            }
                        }
                        else if (p < NewVehicleProbability)
                        {
                            mRoad[i, BusLength-1].Type = AutoType.Car;
                            mRoad[i, BusLength - 1].isFirst = true;
                            for (int j = BusLength - 2; j >= BusLength - CarLength; j--)
                            {
                                mRoad[i, j].Type = AutoType.Car;
                            }
                        }
                }
            }
        }
    }
}
