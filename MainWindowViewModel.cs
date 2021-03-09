﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpasticityClient
{
    class MainWindowViewModel: INotifyPropertyChanged
    {
        private int _numDataExported;
        private ChartModel _emgChartModel;
        private string _portName;

        public List<string> PortNames { get; internal set; }

        public ChartModel EMGChartModel
        {
            get { return _emgChartModel; }
            set
            {
                _emgChartModel = value;
                NotifyPropertyChanged("EMGChartModel");
            }
        }

        public int NumDataExported
        {
            get { return _numDataExported; }
            set
            {
                _numDataExported = value;
                NotifyPropertyChanged("NumDataExported");
            }
        }

        public string PortName
        {
            get { return _portName; }
            set
            {
                    _portName = value;
                    EMGChartModel = new ChartModel(_portName);
                    NotifyPropertyChanged("PortName");
            }
        }

        public MainWindowViewModel()
        {
            PortNames = XBeeFunctions.GetPortNamesByBaudrate(19200);

            if (PortNames.Count >= 1)
            {
                //PortName = "";
                if (PortNames.Count == 1)
                    _portName = PortNames[0];
                    //_portName = "COM 7";
            }
            else
            {
                MessageBox.Show("At least one port needed for chart application to work");
                //If no COM port added, then close window.
                MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                mainWindow.Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

