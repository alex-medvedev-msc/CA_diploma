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
        public int GreenInterval { get; set; }
        public int RedInterval { get; set; }
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
            random = new Random();            
        }
        public void Iterate()
        {            
            FillFirstColumn();
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = RoadLength-1; j >=0; j--)
                {
                    if (!mRoad[i, j].isFirst || j + mRoad[i,j].Speed >= RoadLength)
                    {
                        continue; 
                    }
                    var forwardAutoIndex = SearchForwardAuto(i, j);
                    int speed = Math.Min(MaxSpeed,mRoad[i,j].Speed+MaxAcceleration);
                    if (forwardAutoIndex != -1)
                    {
                        speed = GetSpeed(forwardAutoIndex - j - 1, mRoad[i, j].Speed, mRoad[i, forwardAutoIndex].Speed);
                    }
                    if (speed > mRoad[i,j].Speed + MaxAcceleration)
                        speed = Math.Min(MaxSpeed, mRoad[i, j].Speed + MaxAcceleration);
                    
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
            
            timeStep++;
        }

        int GetSpeed(int distance, int speed, int speedOfForwardAuto)
        {
            int sum = (speedOfForwardAuto + speedOfForwardAuto % 2) * (speedOfForwardAuto + speedOfForwardAuto % 2) / 4 + distance;
            int maxSafeSeed = (int)Math.Floor(2 * Math.Sqrt(sum) - speed % 2);
            return maxSafeSeed;
        }
        int SearchForwardAuto(int i, int j)
        {
            int forwardAutoIndex = j;            
            for (int k = j + 1; k < RoadLength; k++)
            {
                if (mRoad[i, k].Type != AutoType.None)
                    return k;
            }
            return -1;
        }

        //Массив из трех переменных, слева спереди, спереди, справа спереди
        int[] GetFreeCells(int i, int j)
        {            
            return null;            
        }

        void FillFirstColumn()
        {
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
