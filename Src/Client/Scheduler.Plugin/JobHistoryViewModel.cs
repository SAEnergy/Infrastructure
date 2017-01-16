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
        public PlotModel DurationData { get; private set; }
        public PlotModel CountData { get; private set; }
        private int _id;

        public JobHistoryViewModel(ViewBase parent, int jobID) : base(parent)
        {
            _id = jobID;
            DurationData = new PlotModel();
            DurationData.Axes.Add(new DateTimeAxis() { Position = AxisPosition.Bottom, Key = "Date" });
            DurationData.Axes.Add(new TimeSpanAxis() { Position = AxisPosition.Left, Key = "Duration" });
            CountData = new PlotModel();
            CountData.Axes.Add(new DateTimeAxis() { Position = AxisPosition.Bottom, Key = "Date" });
            CountData.Axes.Add(new LinearAxis() { Position = AxisPosition.Left, Key = "Counts" });

            bool isInternalChange = false;
            Axis d1 = DurationData.Axes.First(a => a.Key == "Date");
            Axis d2 = CountData.Axes.First(a => a.Key == "Date");

            d1.AxisChanged += (s, e) =>
            {
                if (isInternalChange)
                {
                    return;
                }

                isInternalChange = true;
                d2.Zoom(d1.ActualMinimum, d1.ActualMaximum);
                CountData.InvalidatePlot(false);
                isInternalChange = false;
            };

            d2.AxisChanged += (s, e) =>
            {
                if (isInternalChange)
                {
                    return;
                }

                isInternalChange = true;
                d1.Zoom(d2.ActualMinimum, d2.ActualMaximum);
                DurationData.InvalidatePlot(false);
                isInternalChange = false;
            };
        }

        protected override void OnConnect(ISubscription source)
        {
            base.OnConnect(source);
            Channel.RequestStatistics(_id);
        }

        protected override void OnDisconnect(ISubscription source, Exception error)
        {
            base.OnDisconnect(source, error);
            DurationData.Series.Clear();
            CountData.Series.Clear();
        }

        public void JobAdded(JobConfiguration job) { }

        public void JobUpdated(JobConfiguration job) { }

        public void JobDeleted(JobConfiguration job) { }

        public void JobStateUpdated(JobState state) { }

        public void StatisticsHistoryUpdated(List<JobStatistics> stats)
        {
            this.BeginInvoke(() =>
            {
                foreach (JobStatistics stat in stats)
                {
                    foreach (PropertyInfo prop in stat.GetType().GetProperties())
                    {
                        if (prop.Name.Contains("ID")) { continue; }

                        if (prop.PropertyType == typeof(int))
                        {
                            LineSeries ser = (LineSeries)CountData.Series.FirstOrDefault(f => f.Title == prop.Name);
                            if (ser == null)
                            {
                                ser = new LineSeries();
                                ser.Title = prop.Name;
                                CountData.Series.Add(ser);
                            }
                            ser.Points.Add(new DataPoint(DateTimeAxis.ToDouble(stat.StartTime), (int)prop.GetValue(stat)));
                        }
                        if (prop.PropertyType == typeof(TimeSpan))
                        {
                            LineSeries ser = (LineSeries)DurationData.Series.FirstOrDefault(f => f.Title == prop.Name);
                            if (ser == null)
                            {
                                ser = new LineSeries();
                                ser.Title = prop.Name;
                                DurationData.Series.Add(ser);
                            }
                            ser.Points.Add(new DataPoint(DateTimeAxis.ToDouble(stat.StartTime), TimeSpanAxis.ToDouble((TimeSpan)prop.GetValue(stat))));
                        }
                    }
                }
                DurationData.InvalidatePlot(true);
                CountData.InvalidatePlot(true);
            });
        }
    }
}
