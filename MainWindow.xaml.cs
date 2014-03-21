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
        public MainWindow()
        {
            InitializeComponent();
            automators = new List<Automator>();
            for (int i = 0; i < 20; i++)
            {
                automators.Add(new Automator()
                    {
                NewVehicleProbability = 1,
                BusQuota=0.2,
                BusLength=12,
                CarLength=5,
                RowCount=5,
                RoadLength=400,
                MaxAcceleration=2,
                MaxSpeed = 11,
                GreenInterval = 100,
                RedInterval = 0,
                GreenIntervalAtEnd = 50,
                RedIntervalAtEnd = 20,
                ChangeRowProbability = 1,
                MaxBusCapacity = 70,
                MaxCarCapacity = 5
            });
                automators[i].Init();
            }
            automator = new Automator() 
            {
                NewVehicleProbability = 0.6,
                BusQuota=0.2,
                BusLength=12,
                CarLength=5,
                RowCount=4,
                RoadLength=400,
                MaxAcceleration=2,
                MaxSpeed = 11,
                GreenInterval = 70,
                RedInterval = 70,
                GreenIntervalAtEnd = 30,
                RedIntervalAtEnd = 30,
                ChangeRowProbability = 0.5
            };
            automator.Init();
            var model = new PlotModel();
            Road.Model = model;
            //PlotRowChanges();
            PlotManFlow();
            //PlotSpeedAndDensity();
            WriteToPngFile("man flow.png");
            //PlotSpeed();
            //PlotDensity();
            /*
            var timer = new Timer(2000);
            timer.Elapsed += timer_Elapsed;
            Task.Factory.StartNew(() =>
                {                    
                    timer.Start();
                });
            */
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

        void PlotManFlow()
        {
            var points = new LineSeries("Man Flow");
            for (int i = 0; i < automators.Count; i++)
            {
                automators[i].BusQuota = (double)i / automators.Count;
            }
            for (int i = 0; i < 200; i++)
            {
                var mens = new List<double>();
                foreach (var ar in automators)
                {
                    ar.Iterate();
                    if (i < 20)
                        continue;
                    mens.Add(ar.GetMenFlow());
                }                
                if (i > 20)
                {
                    points.Points.Add(new DataPoint(i, mens.Average()));                  
                }                
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
