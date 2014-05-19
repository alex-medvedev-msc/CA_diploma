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
        
        public int MaxAcceleration { get; set; }
                
        public double NewVehicleProbability { get; set; }
        public double BusQuota { get; set; }
       
        public bool IsTrafficLightGreen { get; set; }
        public bool IsTrafficLightGreenAtEnd { get; set; }
        public int GreenInterval { get; set; }
        public int RedInterval { get; set; }
        public int GreenIntervalAtEnd { get; set; }
        public int RedIntervalAtEnd { get; set; }

        public int StationStart { get; set; }
        public int StationEnd { get; set; }
        public int AverageTimeOnStation { get; set; }
        public bool NeedTrouble { get; set; }


        Cell[,] mRoad;
        Cell[,] mRoad2;
        public Cell[,] Cells
        {
            get
            {
                return mRoad;
            }
        }
        public static Random random = new Random();
        int timeStep;
        int intervalDuration;
        int intervalDurationAtEnd;

        #region Statistics
        public double GetDensity()
        {
            var autoCount = 0;
            foreach (var c in mRoad)
            {
                if (c.Type == AutoType.Car || c.Type == AutoType.Bus)
                    autoCount++;
            }
            return ((double)autoCount) / (RowCount * RoadLength);
        }

        public double GetAverageSpeed()
        {
            var autoCount = 0;
            double speed = 0;
            foreach (var c in mRoad)
            {
                if (c.isFirst)
                {
                    autoCount++;
                    speed += c.Speed;
                }
            }
            return speed / autoCount; 
        }

        public double GetChangedRowPart()
        {
            var autoCount = 0;
            foreach (var c in mRoad)
            {
                if (c.isFirst)
                    autoCount++;
            }
            return (double)changedRowCount / autoCount;
        }
        int addedMen;
        int removedMen;
        int carsFlow;
        public double GetMenFlow()
        {            
            return removedMen;
        }
        public int GetAutoFlow()
        {
            return carsFlow;
        }
        public double GetAverageTotalFlow()
        {
            return this.GetAverageSpeed() * this.GetDensity();
        }

        #endregion

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
            if (NeedTrouble)
                AddParkedCars();
            IsTrafficLightGreen = true;
            IsTrafficLightGreenAtEnd = false;
            EnableTrafficLightAtIndex(RoadLength - 1, true);
            //random = new Random();            
        }
        public void Iterate()
        {
            addedMen = 0;
            removedMen = 0;
            FillFirstColumn();
            changedRowCount = 0;
            carsFlow=0;
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = RoadLength-1; j >=0; j--)
                {
                    if (mRoad[i, j].Type == AutoType.Trouble)
                        mRoad2[i, j].Type = AutoType.Trouble;
                    if (!mRoad[i, j].isFirst)
                    {
                        continue; 
                    }                    
                    if (j > StationStart / 2 && j <= StationEnd && mRoad[i,j].Type == AutoType.Bus)
                    {
                        continue;
                        if (i > 0)
                        {
                            var busBackAutoIndexLeft = SearchBackAuto(mRoad, i - 1, j);
                            var busBackSpeedLeft = SanitizeSpeed(i - 1, busBackAutoIndexLeft, j);
                            var busForwardAutoIndexLeft = SearchForwardAuto(mRoad, i - 1, j);
                            var forwardSpeedLeft = SanitizeSpeed(i, j, busForwardAutoIndexLeft);
                            if ((busBackAutoIndexLeft == -1 ||
                                mRoad[i - 1, busBackAutoIndexLeft].Speed - MaxAcceleration < busBackSpeedLeft)
                                &&forwardSpeedLeft >= Math.Max(mRoad[i,j].Speed - MaxAcceleration,0))
                            {
                                var busSpeed = SanitizeBusSpeed(i, j, StationEnd);
                                if (busForwardAutoIndexLeft != -1 && busForwardAutoIndexLeft < StationEnd)
                                    busSpeed = SanitizeBusSpeed(i, j, busForwardAutoIndexLeft);
                                //GeneralMoveBus(i, j, busSpeed, false, true);
                            }
                        }
                        else
                        {
                            var busForwardAutoIndex = SearchForwardAuto(mRoad, i, j);
                            var busSpeed = SanitizeBusSpeed(i, j, StationEnd);
                            if (busForwardAutoIndex != -1 && busForwardAutoIndex < StationEnd)
                                busSpeed = SanitizeSpeed(i, j, busForwardAutoIndex);
                            //GeneralMoveBus(i, j, busSpeed, false, false);
                        }
                        continue;
                    }                    

                    var forwardAutoIndex = SearchForwardAuto(mRoad,i, j);
                    int speed = SanitizeSpeed(i, j, forwardAutoIndex);
                    var forwardAutoIndexLeft = j+1; 
                    var backAutoIndexLeft = j-1;
                    int speedLeft = 0;
                    int speedRight = 0;
                    int backSpeedLeft = 0;
                    int backSpeedRight = 0;
                    bool canMoveLeft = false;
                    var length = mRoad[i,j].Type == AutoType.Bus ? BusLength : CarLength;
                    if (i > 0 && CheckLane(i-1,j,length))
                    {                        
                        forwardAutoIndexLeft = SearchForwardAuto(mRoad,i - 1, j);
                        backAutoIndexLeft = SearchBackAuto(mRoad,i - 1, j);
                        if (i==1 || 
                            (i > 1 && CheckSignalOff(i - 2, backAutoIndexLeft, forwardAutoIndexLeft, LaneChange.Right)))
                        {
                            speedLeft = SanitizeSpeed(i - 1, j, forwardAutoIndexLeft);
                            backSpeedLeft = SanitizeSpeed(i - 1, backAutoIndexLeft, j);
                            if ((backAutoIndexLeft == -1 ||
                                mRoad[i - 1, backAutoIndexLeft].Speed <= backSpeedLeft)
                                && speedLeft > speed)
                            {
                                canMoveLeft = true;
                            }
                        }
                    }
                    var forwardAutoIndexRight = j+1;
                    var backAutoIndexRight = j-1;
                    bool canMoveRight = false;
                    if (i < RowCount - 1 && CheckLane(i+1,j,length))
                    {
                        forwardAutoIndexRight = SearchForwardAuto(mRoad,i + 1, j);
                        backAutoIndexRight = SearchBackAuto(mRoad,i + 1, j);
                        if (i == RowCount - 2 || 
                            (i < RowCount - 2 && CheckSignalOff(i + 2, backAutoIndexRight, forwardAutoIndexRight, LaneChange.Left)))
                        {
                            speedRight = SanitizeSpeed(i + 1, j, forwardAutoIndexRight);
                            backSpeedRight = SanitizeSpeed(i + 1, backAutoIndexRight, j);
                            if ((backAutoIndexRight == -1 ||
                                mRoad[i + 1, backAutoIndexRight].Speed <= backSpeedRight)
                                && speedRight > speed)
                            {
                                canMoveRight = true;
                            }
                        }
                    }                   
                    GeneralRandomizedMoveAuto(canMoveLeft, canMoveRight, i, j, speed);
                }
            }

            //Copy road back
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < RoadLength; j++)
                {
                    mRoad[i, j].isFirst = mRoad2[i, j].isFirst;
                    mRoad[i, j].Speed = mRoad2[i, j].Speed;
                    mRoad[i, j].Type = mRoad2[i, j].Type;
                    mRoad[i, j].ManCount = mRoad2[i, j].ManCount;
                    mRoad[i, j].StationLimit = mRoad2[i, j].StationLimit;
                    mRoad[i, j].Signal = mRoad2[i, j].Signal;
                    mRoad2[i,j] = new Cell();
                }
            }
            
            NextTime();            
        }

        bool CheckSignalOff(int i, int startJ, int endJ, LaneChange direction)
        {
            if (startJ == -1)
                startJ = 0;
            if (endJ == -1)
                endJ = RoadLength - 1;
            for (int k = startJ; k < endJ; k++)
            {
                if (mRoad[i, k].Signal == direction)
                    return false;
            }
            return true;
        }

        bool CheckLane(int i, int j, int len)
        {
            for (int k = j; k > j - len; k--)
            {
                if (mRoad[i, k].Type != AutoType.None)
                    return false;
            }
            return true;
        }
        

        void AddParkedCars()
        {
            mRoad[RowCount - 1, K].Type = AutoType.Trouble;
           // return;
            for (int k = RoadLength / 2; k < RoadLength - 1; k += 2)
            {
                mRoad[RowCount - 1, k].Type = AutoType.Trouble;
            }
        }
        /*
        //Вызывается только до и во время остановки, после используется функция для движения автомобиля
        void GeneralMoveBus(int i, int j, int speed, bool canMoveRight, bool canMoveLeft)
        {
            if (j >= StationStart && j <= StationEnd)
            {
                if (mRoad[i, j].StationLimit > 0)
                {
                    mRoad[i, j].StationLimit--;
                }
                else
                {
                    if (canMoveLeft)
                        MoveAuto(i, j, speed, i - 1);
                    else
                        MoveAuto(i, j, speed, i);
                }
            }
            else
            {
                if (canMoveRight)
                {
                    MoveAuto(i, j, speed, i + 1);
                }
                else
                {
                    MoveAuto(i, j, speed, i);
                }
            }                      
        }
        */
        int SanitizeBusSpeed(int i, int j, int forwardAutoIndex)
        {
            if (j == -1)
            {
                return Math.Min(MaxSpeed, 0 + MaxAcceleration);
            }
            int speed = Math.Min(MaxSpeed, mRoad[i, j].Speed + MaxAcceleration);
            if (forwardAutoIndex != -1)
            {
                speed = GetSpeed(forwardAutoIndex - j - 1, mRoad[i, j].Speed, 0);
                if (speed > mRoad[i, j].Speed + MaxAcceleration || speed > MaxSpeed)
                    speed = Math.Min(MaxSpeed, mRoad[i, j].Speed + MaxAcceleration);
            }
            return speed;
        }
        void GeneralRandomizedMoveAuto(bool canMoveLeft, bool canMoveRight,
                                        int i, int j, int speed)
        {
            
            var changeP = random.NextDouble();
            /*if (mRoad[i, j].Type == AutoType.Bus && 
                j >= StationStart/2 &&
                j <= StationStart)
            {
                MoveAuto(i, j, speed, i - 1);
            }*/
            int leftRightSpeed = speed - 1 >= 0 ? speed - 1 : 0;
            if (canMoveLeft && canMoveRight)
            {
                if (changeP < ChangeRowProbability / 2)
                {
                    if (mRoad[i, j].Signal == LaneChange.Left)
                        MoveAuto(i, j, leftRightSpeed, i - 1, LaneChange.None);
                    else
                        MoveAuto(i, j, speed, i, LaneChange.Left);
                }
                else if (changeP < ChangeRowProbability)
                {
                    if (mRoad[i,j].Signal == LaneChange.Right)
                        MoveAuto(i, j, leftRightSpeed, i + 1,LaneChange.None);
                    else
                        MoveAuto(i, j, speed, i, LaneChange.Right);
                }
                else
                    MoveAuto(i, j, speed, i,LaneChange.None);
            }
            else if (canMoveLeft)
            {
                if (changeP < ChangeRowProbability / 2)
                {
                    if (mRoad[i, j].Signal == LaneChange.Left)
                        MoveAuto(i, j, leftRightSpeed, i - 1, LaneChange.None);
                    else
                        MoveAuto(i, j, speed, i, LaneChange.Left);
                }
                else
                    MoveAuto(i, j, speed, i,LaneChange.None);
            }
            else if (canMoveRight)
            {
                if (changeP < ChangeRowProbability / 2)
                {
                    if (mRoad[i, j].Signal == LaneChange.Right)
                        MoveAuto(i, j, leftRightSpeed, i + 1, LaneChange.None);
                    else
                        MoveAuto(i, j, speed, i, LaneChange.Right);
                }
                else
                    MoveAuto(i, j, speed, i,LaneChange.None);
            }
            else
                MoveAuto(i, j, speed, i,LaneChange.None);
        }
                
        int changedRowCount;
        void MoveAuto(int i, int j,int speed,int newI,LaneChange newSignal)
        {
            if (i != newI)
                changedRowCount++;
            if (j + speed >= RoadLength-1)
            {
                removedMen += mRoad[i, j].ManCount;
                carsFlow++;
                return;
            } 
            int len = mRoad[i, j].Type == AutoType.Bus ? BusLength : CarLength;
            mRoad2[newI, j + speed].isFirst = true;
            mRoad2[newI, j + speed].Type = mRoad[i, j].Type;
            mRoad2[newI, j + speed].Speed = speed;
            mRoad2[newI, j + speed].ManCount = mRoad[i, j].ManCount;
            mRoad2[newI, j + speed].StationLimit = mRoad[i, j].StationLimit;
            mRoad2[newI, j + speed].Signal = newSignal;           
            for (int k = j - 1; k >= j - len + 1; k--)
            {
                mRoad2[newI, k + speed].Type = mRoad[i, j].Type;
                mRoad2[newI, k + speed].Speed = speed;
                mRoad2[newI, k + speed].Signal = newSignal;
            }
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
            EnableTrafficLightAtIndex(RoadLength - 1, !IsTrafficLightGreenAtEnd);
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
                return Math.Min(MaxSpeed, mRoad[i, forwardAutoIndex].Speed + MaxAcceleration);
            
            int speed = Math.Min(MaxSpeed, mRoad[i, j].Speed + MaxAcceleration);
            if (forwardAutoIndex != -1)            
                speed = GetSpeed(forwardAutoIndex - j - 1, mRoad[i, j].Speed, 
                                    mRoad[i, forwardAutoIndex].Speed);
            
           // if (speed > forwardAutoIndex - j - 1)
               // throw new ArgumentException("speed exception: speed = " + speed
                 //   + " i = " + i + " j = " + j + " distance = " + (forwardAutoIndex - j - 1));
            return speed;
        }
        public int K { get; set; }
        public int D1 { get; set; }
        public int GetSpeed(int distance, int speed, int speedOfForwardAuto)
        {
            var syncDistance = D1 + K * speed;
            if (distance <= syncDistance)
            {
                var delta = speedOfForwardAuto - speed;
                if (delta > 0)
                    return Math.Min(MaxSpeed, speed + Math.Min(MaxAcceleration, delta));
                else
                    return Math.Max(0,speed + Math.Max(-MaxAcceleration, delta-1));
            }
            else
            {
                return Math.Min(MaxSpeed, speed + random.Next(MaxAcceleration) + 1);
            }
        }
        int SearchBackAuto(Cell[,] road,int i, int j)
        {
            for (int k = j - 1; k >= 0; k--)
            {
                if (road[i, k].Type == AutoType.Car || road[i,k].Type == AutoType.Bus)
                    return k;
            }
            
            return -1;
        }
        int SearchForwardAuto(Cell[,] road,int i, int j)
        {           
            for (int k = j + 1; k < RoadLength; k++)
            {
                if (road[i, k].Type != AutoType.None)
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
                            mRoad[i, BusLength - 1].ManCount = random.Next(MaxBusCapacity + 1);
                            addedMen += mRoad[i, BusLength - 1].ManCount;
                            for (int j = BusLength - 2; j >= 0; j--)
                            {
                                mRoad[i, j].Type = AutoType.Bus;
                            }
                        }
                        else if (p < NewVehicleProbability)
                        {
                            mRoad[i, BusLength - 1].Type = AutoType.Car;
                            mRoad[i, BusLength - 1].isFirst = true;
                            mRoad[i, BusLength - 1].ManCount = random.Next(MaxCarCapacity + 1);
                            addedMen += mRoad[i, BusLength - 1].ManCount;
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
