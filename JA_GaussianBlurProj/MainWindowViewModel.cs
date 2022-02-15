using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JA_GaussianBlurProj.Annotations;

namespace JA_GaussianBlurProj
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly object _progressLocker = new object();

        public RelaySyncCommand SelectInputDirectoryCommand { get; }
        public RelaySyncCommand SelectOutputDirectoryCommand { get; }
        public RelayAsyncCommand CalculateCommand { get; }
        public RelayAsyncCommand BenchmarkAsyncCommand { get; }

        private int _threadsCount = 1;
        private float _sigma;
        private int _radius = 1;
        private string _inputDirectory;
        private string _outputDirectory;
        private double _progress;
        private string _duration;
        private double _imagesCount;
        private bool _notBusy = true;

        #region PROPERTIES
        public int ThreadsCount
        {
            get => _threadsCount;
            set
            {
                _threadsCount = value;
                OnPropertyChanged();
            }
        }

        public int Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                OnPropertyChanged();
            }
        }

        public float Sigma
        {
            get => _sigma;
            set
            {
                _sigma = value;
                OnPropertyChanged();
            }
        }

        public string InputDirectory
        {
            get => _inputDirectory;
            set
            {
                _inputDirectory = value;
                OnPropertyChanged();
            }
        }

        public string OutputDirectory
        {
            get => _outputDirectory;
            set
            {
                _outputDirectory = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get
            {
                lock (_progressLocker)
                {
                    return _progress;
                }
            }
            private set
            {
                lock (_progressLocker)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ImagesCount
        {
            get => _imagesCount;
            set
            {
                _imagesCount = value;
                OnPropertyChanged();
            }
        }

        public int SelectedLib { get; set; }

        public string Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged();
            }
        }

        public bool NotBusy
        {
            get => _notBusy;
            set
            {
                _notBusy = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public int TimeMiliseconds { get; private set; } = 0;
        public MainWindowViewModel()
        {
            ThreadsCount = Environment.ProcessorCount;

            SelectInputDirectoryCommand = new RelaySyncCommand(SelectInputDirectoryCommandExecute);
            SelectOutputDirectoryCommand = new RelaySyncCommand(SelectOutputDirectoryCommandExecute);
            CalculateCommand = new RelayAsyncCommand(CalculateButtonCommandExecute);
            BenchmarkAsyncCommand = new RelayAsyncCommand(BenchmarkButtonAsyncCommandExecute);
            Progress = 0.0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
