using Client.Base;
using OxyPlot;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Comm;
using System.Reflection;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace Scheduler.Plugin
{
    public class JobHistoryViewModel : ViewModelBase<ISchedulerHost>, ISchedulerCallback
    {
        public PlotModel Data { get; private set; }
        private int _id;

        public JobHistoryViewModel(ViewBase parent, int jobID) : base(parent)
        {
            _id = jobID;
            Data = new PlotModel();
            Data.Axes.Add(new DateTimeAxis() { Position = AxisPosition.Bottom, Key = "Date" });
            Data.Axes.Add(new LinearAxis() { Position = AxisPosition.Right, Key = "Counts" });
            Data.Axes.Add(new TimeSpanAxis() { Position = AxisPosition.Left, Key = "Duration" });
        }

        protected override void OnConnect(ISubscription source)
        {
            base.OnConnect(source);
            Channel.RequestStatistics(_id);
        }

        protected override void OnDisconnect(ISubscription source, Exception error)
        {
            base.OnDisconnect(source, error);
            Data.Series.Clear();
        }

        public void JobAdded(JobConfiguration job) { }

        public void JobUpdated(JobConfiguration job) { }

        public void JobDeleted(JobConfiguration job) { }

        public void JobStateUpdated(JobState state) { }

        public void StatisticsHistoryUpdated(List<JobStatistics> stats)
        {
            foreach (JobStatistics stat in stats)
            {
                foreach (PropertyInfo prop in stat.GetType().GetProperties())
                {
                    if (prop.PropertyType != typeof(int) && prop.PropertyType != typeof(TimeSpan)) { continue; }
                    if (prop.Name.Contains("ID")) { continue; }
                    LineSeries ser = (LineSeries)Data.Series.FirstOrDefault(f => f.Title == prop.Name);
                    if (ser == null)
                    {
                        ser = new LineSeries();
                        ser.Title = prop.Name;
                        ser.XAxisKey = "Date";
                        if (prop.PropertyType == typeof(TimeSpan))
                        {
                            ser.YAxisKey = "Duration";
                        }
                        else
                        {
                            ser.YAxisKey = "Counts";
                        }
                        Data.Series.Add(ser);
                    }
                    if (prop.PropertyType == typeof(TimeSpan))
                    {
                        ser.Points.Add(new DataPoint(DateTimeAxis.ToDouble(stat.StartTime), TimeSpanAxis.ToDouble((TimeSpan)prop.GetValue(stat))));
                    }
                    else
                    {
                        ser.Points.Add(new DataPoint(DateTimeAxis.ToDouble(stat.StartTime), (int)prop.GetValue(stat)));
                    }
                }
            }
            Data.InvalidatePlot(true);
        }
    }
}
