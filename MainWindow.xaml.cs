using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Axes;

using System.Timers;

namespace PractiseVisualizer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Automator automator;
        List<Automator> automators;
        const int totalIterations = 400;
        const int warmUpIterations = 50;
        public MainWindow()
        {
            InitializeComponent();
            automators = new List<Automator>();
            for (int i = 0; i < 50; i++)
            {
                automators.Add(new Automator()
                    {
                        NewVehicleProbability = 0.5,
                        BusQuota = 0.1,
                        BusLength = 12,
                        CarLength = 5,
                        RowCount = 4,
                        RoadLength = 500,
                        MaxAcceleration = 2,
                        MaxSpeed = 11,
                        GreenInterval = 2000,
                        RedInterval = 1,
                        GreenIntervalAtEnd = 2000,
                        RedIntervalAtEnd = 1,
                        ChangeRowProbability = 1,
                        MaxBusCapacity = 70,
                        MaxCarCapacity = 5,
                        D1 = 1,
                        K = 3,
                        NeedTrouble = true
            });
                automators[i].Init();
            }
            automator = new Automator() 
            {
                NewVehicleProbability = 0.3,
                BusQuota = 0.2,
                BusLength = 12,
                AverageTimeOnStation = 30,
                CarLength = 5,
                RowCount = 4,
                RoadLength = 200,
                MaxAcceleration = 2,
                MaxSpeed = 11,
                GreenInterval = 100,
                RedInterval = 20,
                GreenIntervalAtEnd = 50,
                RedIntervalAtEnd = 40,
                ChangeRowProbability = 1,
                MaxBusCapacity = 70,
                MaxCarCapacity = 5,
                D1 = 2,
                K = 4,
                NeedTrouble = false,
                StationStart = 100,
                StationEnd = 150
            };
            automator.Init();
            var model = new PlotModel();
            Road.Model = model;
            //PlotManFlow();
            //PlotFundamentalDiagram();
            //PlotRowChanges();
            //PlotDensityAndChangeRows();
            //PlotSpeedAndDensity();
            //WriteToPngFile("new_densityChangeRows_trouble_nolight.png");
            //PlotSpeed();
            //PlotDensity();

            NextIteration.Click += NextIteration_Click;
            var speeds = new int[]
            {
                automator.GetSpeed(0,1,1),
                automator.GetSpeed(0,0,0),
                automator.GetSpeed(0,0,5),
                automator.GetSpeed(2,1,1),
                automator.GetSpeed(5,2,0)
            };
            var timer = new Timer(2000);
            timer.Elapsed += timer_Elapsed;
            Task.Factory.StartNew(() =>
                {                    
                    //timer.Start();
                });            
        }

        void NextIteration_Click(object sender, RoutedEventArgs e)
        {
            automator.Iterate();
            Dispatcher.Invoke(() =>
            {
                PlotRoad(automator.Cells);
            });
        }

        void WriteToPngFile(string fileName)
        {
            const string directory = @"C:\Users\ag\Google Диск\Diplom\PracticeGraphs";
            string path = System.IO.Path.Combine(directory, fileName);
            using (var stream = System.IO.File.Create(path))
            {
                var pngExporter = new OxyPlot.Wpf.PngExporter();
                pngExporter.Export(Road.Model, stream);
            }
        }

        int iterations = 0;
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (iterations == 30)
                ((Timer)sender).Stop();
            automator.Iterate();
            Dispatcher.Invoke(() =>
                {
                    PlotRoad(automator.Cells);
                });
            iterations++;
        }


        void PlotFundamentalDiagram()
        {
            var greenGreenPoints = new ScatterSeries();
            var greenRedPoints = new ScatterSeries();
            var redGreenPoints = new ScatterSeries();
            var redRedPoints = new ScatterSeries();
            greenGreenPoints.MarkerFill = OxyColors.DarkGreen;
            greenRedPoints.MarkerFill = OxyColors.LightGreen;
            redGreenPoints.MarkerFill = OxyColors.OrangeRed;
            redRedPoints.MarkerFill = OxyColors.DarkRed;
            for (int i = 0; i < totalIterations; i++)
            {
                var densities = new List<double>();
                var flows = new List<double>();
                foreach (var ar in automators)
                {
                    ar.Iterate();                    
                    if (i < warmUpIterations)
                        continue;
                    densities.Add(ar.GetDensity());
                    flows.Add(ar.GetAverageTotalFlow());
                }
                if (i >= warmUpIterations)
                {
                    if (automators[0].IsTrafficLightGreen && automators[0].IsTrafficLightGreenAtEnd)                    
                        greenGreenPoints.Points.Add(new ScatterPoint(densities.Average(), flows.Average(), 2));
                    else if (!automators[0].IsTrafficLightGreen && automators[0].IsTrafficLightGreenAtEnd)
                        redGreenPoints.Points.Add(new ScatterPoint(densities.Average(), flows.Average(), 2));
                    else if (!automators[0].IsTrafficLightGreen && !automators[0].IsTrafficLightGreenAtEnd)
                        redRedPoints.Points.Add(new ScatterPoint(densities.Average(), flows.Average(), 2));
                    else if (automators[0].IsTrafficLightGreen && !automators[0].IsTrafficLightGreenAtEnd)
                        greenRedPoints.Points.Add(new ScatterPoint(densities.Average(), flows.Average(), 2));
                    }
            }
            Road.Model.Series.Add(greenGreenPoints);
            Road.Model.Series.Add(greenRedPoints);
            Road.Model.Series.Add(redRedPoints);
            Road.Model.Series.Add(redGreenPoints);
            Road.Model.Axes.Add(new LinearAxis(AxisPosition.Bottom, "Плотность") { TitleFontSize = 20 });
            Road.Model.Axes.Add(new LinearAxis(AxisPosition.Left, "Поток") { TitleFontSize = 20 });
            Road.Model.RefreshPlot(true);  
        }

        void PlotDensityAndChangeRows()
        {
            var points = new ScatterSeries();
                        
            for (int i = 0; i < totalIterations; i++)
            {
                var densities = new List<double>();
                var changeRows = new List<double>();
                foreach (var ar in automators)
                {
                    ar.Iterate();
                    if (i < warmUpIterations)
                        continue;
                    densities.Add(ar.GetDensity());
                    changeRows.Add(ar.GetChangedRowPart());                    
                }
                if (i >= warmUpIterations)
                    points.Points.Add(new ScatterPoint(densities.Average(), changeRows.Average(),2));
            }           
            Road.Model.Series.Add(points);

            Road.Model.Axes.Add(new LinearAxis(AxisPosition.Bottom, "Плотность") { TitleFontSize = 20 });
            Road.Model.Axes.Add(new LinearAxis(AxisPosition.Left, "Доля перестроившихся машин") { TitleFontSize = 20 });
            Road.Model.RefreshPlot(true);
        }

        const int MAX_ITERATIONS = 400;
        void PlotManFlow()
        {
            var points = new LineSeries("Man Flow");
            for (int i = 0; i < automators.Count; i++)
            {
                automators[i].BusQuota = (double)i / automators.Count;
            }
            var manFlowList = new double[automators.Count];
            for (int ai = 0; ai < automators.Count; ai++)
            {                
                for (int i = 0; i < MAX_ITERATIONS; i++)
                {
                    automators[ai].Iterate();
                    manFlowList[ai] += automators[ai].GetMenFlow();
                }                
            }
            for (int i = 0; i < manFlowList.Length; i++)
            {
                points.Points.Add(new DataPoint((double)i / manFlowList.Length, manFlowList[i] / MAX_ITERATIONS));
            }
            
            Road.Model.Series.Add(points);
            Road.Model.RefreshPlot(true);  
        }

        void PlotRowChanges()
        {
            var points = new LineSeries("Row Changes");         

            for (int i = 0; i < 200; i++)
            {
                var rowChanges = new List<double>();
                foreach (var ar in automators)
                {
                    ar.Iterate();
                    if (i < 20)
                        continue;
                    rowChanges.Add(ar.GetChangedRowPart());
                }
                if (i >= 20)
                    points.Points.Add(new DataPoint(i, rowChanges.Average()));
            }            
            Road.Model.Series.Add(points);
            Road.Model.RefreshPlot(true); 
        }

        void PlotSpeedAndDensity()
        {            
            var points = new ScatterSeries();
            var unsorted = new List<ScatterPoint>();
            
            for (int i = 0; i < 200; i++)
            {
                var densities = new List<double>();
                var speeds = new List<double>();
                foreach (var ar in automators)
                {
                    ar.Iterate();
                    if (i < 20)
                        continue;
                    densities.Add(ar.GetDensity());
                    speeds.Add(ar.GetAverageSpeed());                    
                }
                if (i >= 20)
                    unsorted.Add(new ScatterPoint(densities.Average(), speeds.Average(),1.5));
            }
            var sorted = unsorted.OrderBy(dp => dp.X).ToList();
            foreach (var s in sorted)
                points.Points.Add(s);
            Road.Model.Series.Add(points);

            Road.Model.Axes.Add(new LinearAxis(AxisPosition.Bottom, "Плотность") { TitleFontSize = 20 });
            Road.Model.Axes.Add(new LinearAxis(AxisPosition.Left, "Скорость") { TitleFontSize = 20 });
            Road.Model.RefreshPlot(true); 
        }

        void PlotDensity()
        {
            var points = new LineSeries("Density");
            for (int i = 0; i < 200; i++)
            {
                automator.Iterate();
                points.Points.Add(new DataPoint(i, automator.GetDensity()));
            }
            
            Road.Model.Series.Add(points);
            Road.Model.RefreshPlot(true);
        }

        void PlotSpeed()
        {
            var points = new LineSeries("Speed");
            for (int i = 0; i < 200; i++)
            {
                automator.Iterate();
                points.Points.Add(new DataPoint(i, automator.GetAverageSpeed()));
            }
            
            Road.Model.Series.Add(points);
            Road.Model.RefreshPlot(true); 
        }

        void PlotRoad(Cell[,] cells)
        {
            Road.Model.Series.Clear();
            var series = new RectangleBarSeries();
            series.Items.Add(new RectangleBarItem(100, 0, 100, 1));
            series.Items.Add(new RectangleBarItem(100, 5, 99, 5));
            series.Items.Add(new RectangleBarItem(0, 5, 1, 5));
              
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                for (int j = 0; j < cells.GetLength(1); j++)
                {
                    if (cells[i, j].Type != AutoType.None)
                    {
                        var item = new RectangleBarItem(j, i, j + 1, i + 1);
                        switch (cells[i, j].Type)
                        {
                            case AutoType.Bus:
                                item.Color = OxyColors.LightYellow;
                                    if (cells[i, j].isFirst)
                                        item.Color = OxyColors.Yellow;
                                break;
                            case AutoType.Car:
                                if (cells[i, j].isFirst)
                                {
                                    item.Color = OxyColors.LightBlue;
                                }
                                else
                                {
                                    item.Color = OxyColors.LightCyan;
                                }
                                break;
                            case AutoType.Trouble:
                                item.Color = OxyColors.Red;
                                break;
                            default:
                                break; 
                        }                        
                        series.Items.Add(item);
                    }
                }
            }           
            Road.Model.Series.Add(series);
            Road.Model.RefreshPlot(true);
        }
    }
}
