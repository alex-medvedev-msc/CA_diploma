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
        public MainWindow()
        {
            InitializeComponent();
            automator = new Automator() 
            {
                NewVehicleProbability = 0.6,
                BusQuota=0.2,
                BusLength=12,
                CarLength=5,
                RowCount=5,
                RoadLength=100,
                MaxAcceleration=2,
                MaxSpeed = 11,
                GreenInterval = 15,
                RedInterval = 8,
                GreenIntervalAtEnd = 15,
                RedIntervalAtEnd = 20
            };
            automator.Init();
            var model = new PlotModel();
            Road.Model = model;
            var timer = new Timer(2000);
            timer.Elapsed += timer_Elapsed;
            Task.Factory.StartNew(() =>
                {                    
                    timer.Start();
                });
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
