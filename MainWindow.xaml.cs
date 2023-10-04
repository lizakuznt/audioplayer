using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace pleer
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer;
        private List<string> musicFiles;
        private int currentTrackIndex;
        private bool isPlaying;
        private bool isRepeatMode;
        private bool isShuffleMode;
        private Thread progressUpdateThread;
        private Thread timerThread;
        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            mediaPlayer = new MediaPlayer();
            musicFiles = new List<string>();
            currentTrackIndex = 0;
            isPlaying = false;
            isRepeatMode = false;
            isShuffleMode = false;
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folderPath = dialog.FileName;
                LoadMusicFiles(folderPath);
                PlaySelectedTrack();
            }
        }

        private void LoadMusicFiles(string folderPath)
        {
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => file.EndsWith(".mp3") || file.EndsWith(".m4a") || file.EndsWith(".wav"))
                .ToList();

            if (files.Any())
            {
                musicFiles = files;
                currentTrackIndex = 0;
            }
            else
            {
                MessageBox.Show("Папка не содержит подходящих аудиофайлов.");
            }
        }

        private void PlaySelectedTrack()
        {
            if (musicFiles.Count > 0)
            {
                mediaPlayer.Open(new Uri(musicFiles[currentTrackIndex]));
                mediaPlayer.Play();
                isPlaying = true;
                UpdatePlayPauseButtonContent();
                StartProgressUpdateThread();
                StartTimerThread();
            }
        }

        private void MusicSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                var newPosition = TimeSpan.FromSeconds(e.NewValue);
                mediaPlayer.Position = newPosition;
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                PausePlayback();
            }
            else
            {
                ResumePlayback();
            }
        }

        private void PausePlayback()
        {
            mediaPlayer.Pause();
            isPlaying = false;
            UpdatePlayPauseButtonContent();
        }

        private void ResumePlayback()
        {
            mediaPlayer.Play();
            isPlaying = true;
            UpdatePlayPauseButtonContent();
        }

        private void UpdatePlayPauseButtonContent()
        {
            playPauseButton.Content = isPlaying ? "Пауза" : "Старт";
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrackIndex > 0)
            {
                currentTrackIndex--;
                PlaySelectedTrack();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (isShuffleMode)
            {
                PlayRandomTrack();
            }
            else
            {
                PlayNextTrack();
            }
        }

        private void PlayNextTrack()
        {
            if (currentTrackIndex < musicFiles.Count - 1)
            {
                currentTrackIndex++;
                PlaySelectedTrack();
            }
            else if (isRepeatMode)
            {
                currentTrackIndex = 0;
                PlaySelectedTrack();
            }
            else
            {
                StopPlayback();
            }
        }

        private void PlayRandomTrack()
        {
            Random random = new Random();
            currentTrackIndex = random.Next(0, musicFiles.Count);
            PlaySelectedTrack();
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            isRepeatMode = !isRepeatMode;
            repeatButton.Content = isRepeatMode ? "Повтор On" : "Повтор Off";
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            isShuffleMode = !isShuffleMode;
            shuffleButton.Content = isShuffleMode ? "Перемешать On" : "Перемешать Off";
        }

        private void StartProgressUpdateThread()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            progressUpdateThread = new Thread(() =>
            {
                while (isPlaying && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                    Dispatcher.Invoke(() => { ProgressBar.Value = mediaPlayer.Position.TotalSeconds; });
                }
            });

            progressUpdateThread.Start();
        }

        private void StartTimerThread()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            timerThread = new Thread(() =>
            {
                while (isPlaying && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                    Dispatcher.Invoke(() => { UpdateRemainingTimeText(); });
                }
            });

            timerThread.Start();
        }

        private void UpdateRemainingTimeText()
        {
            var remainingTime = mediaPlayer.NaturalDuration.TimeSpan - mediaPlayer.Position;
            timerLabel.Text = $"Time Remaining: {remainingTime:mm\\:ss}";
        }

        private void StopPlayback()
        {
            cancellationTokenSource?.Cancel();

            mediaPlayer.Stop();
            isPlaying = false;
            ProgressBar.Value = 0;
            timerLabel.Text = "Time Remaining: 00:00";
            UpdatePlayPauseButtonContent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            cancellationTokenSource?.Cancel();
        }
    }
}
