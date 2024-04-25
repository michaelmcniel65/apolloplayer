// Apollo Player v.1.0.00
// Last Updated: 8 April 2024
// Created by: Michael McNiel
// Email: michaelcmcniel@gmail.com
// Github: github.com/michaelmcniel65

// Bug fixes:
// #1 - Fixed a bug where the app would freeze if the play button was pressed twice. Fix described in OnPlayStopped Method
// #2 - Changed music file folder to the Music folder on the user desktop. Fix described in GetMusicFilePath method
// #3 - Added isMax bool to make the PlayNextSong method work properly. This is added onto bug #1. Fix described in OnPlaybackStopped Method
// #4 - Same location of bug 1 and 3. Might need to rewrite OnPlaybackStopped sometime. Deleting a song that was playing would crash app.
// #5 - Song would not stop playing if nothing was selected and the user clicked next or previous. Songs would overlap. Added a StopAndDispose Method where necessary
// #6 - Playing after pausing a song would crash app. Commented out a block of code in btnPlay_Click method. I believe I used it for something, just can't remember now.

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Windows.Interop;
using System.Windows.Forms;
using NAudio.Wave;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ApolloPlayer
{
    public partial class MainWindow : Window
    {
        //STATIC FIELDS FOR MAINWINDOW-------------------------------
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;

        DateTime audioTime;
        DateTime totalAudioTime;

        TimeSpan second = new TimeSpan(0, 0, 3);
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        Random rnd = new();

        List<int> usedSongs = [];
        int numberOfSongs;
        int shufflePreviousCount = 2;

        bool isPaused = false; //Used to prevent reinitialization of output device
        bool isShuffle = false;
        bool isReplay = false;
        bool songDone = false;
        bool fileWasDeleted = false;
        bool btnStopWasClicked = false;
        bool btnNextWasClicked = false;
        bool btnPrevWasClicked = false;
        bool btnPlayWasClicked = false;
        bool isMax = false;

        string currentSong = "";

        float volumeBeforeMute = 0.00f;

        //PROGRAM START----------------------------------------------
        public MainWindow()
        {
            InitializeComponent();
            OnStart();
        }
        private void OnStart()
        {
            UpdateMusicList();
            sldrVolume.Value = .5;
            if (outputDevice != null)
            {
                outputDevice.Volume = .5f;
            }
        }

        //WINDOW CONTROL METHODS-------------------------------------
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        //BUTTON METHODS---------------------------------------------
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            //This method was the recommended starting point for playing audio using NAudio
            //Source: https://github.com/naudio/NAudio/blob/master/Docs/PlayAudioFileWinForms.md

            //Instaniating the items in the listbox to get the full audio file path is from this stack overflow post
            //https://stackoverflow.com/questions/48764206/how-to-get-actual-path-of-a-file-listed-in-listbox
            //The UpdateMusicList helper method assigns the properties which are used here

            var defaultMusic = lbxMusicList.Items[0] as MusicFile;
            var selectedMusic = lbxMusicList.SelectedItem as MusicFile;

            btnPlayWasClicked = true;

            if (outputDevice == null)
            {
                WaveOutEvent();
            }

            //#6 - Removed this code. It was disposing the initialized file when play would be clicked after pausing
            //else
            //{
            //    StopAndDispose();
            //}

            //If no song is selected
            if (lbxMusicList.SelectedIndex == -1)
            {
                if (isShuffle)
                {
                    HandleShuffle();
                }
                else
                {
                    GetPathAndInitialize(defaultMusic);

                    OnPlay(defaultMusic);

                    lbxMusicList.SelectedItem = lbxMusicList.Items[0];
                }
            }
            //If a song is selected
            else
            {

                if (outputDevice?.PlaybackState != PlaybackState.Playing)
                {
                    //Prevents a bug that crashes the program because it tries
                    //to reinitialize an already initialized output device.
                    //Because we're not "stopping" the audio (we're pausing it), we don't want to
                    //dispose of the output device.
                    if (isPaused && outputDevice != null)
                    {
                        isPaused = false;
                        outputDevice?.Play();
                        OnPlay();
                    }
                    //If song was manually stopped
                    else
                    {
                        GetPathAndInitialize(selectedMusic);

                        OnPlay(selectedMusic);
                    }
                }
                //If a song is playing and they select a different one then hit play
                else
                {
                    StopAndDispose();

                    WaveOutEvent();

                    GetPathAndInitialize(selectedMusic);

                    OnPlay(selectedMusic);
                }
            }

            outputDevice?.Play();          
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStopWasClicked = true;

            outputDevice?.Stop();
            dispatcherTimer?.Stop();
        }
        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            outputDevice?.Pause();
            dispatcherTimer?.Stop();

            isPaused = true;
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            btnNextWasClicked = true;

            try
            {
                if (isMax)
                {
                    HandleSkipNext();
                }
                else
                {
                    HandleSkipNext();
                }
            }
            //If the selection goes out of bounds, the playlist goes back to beginning
            catch
            {
                RestartPlaylist(0);
            }
        }
        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            btnPrevWasClicked = true;

            try
            {
                HandleSkipPrev();
            }
            //If playlist goes out of bounds, playlist goes to end
            catch
            {
                RestartPlaylist(lbxMusicList.Items.Count - 1);
            }
        }
        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog musicSearch = new OpenFileDialog()
            {
                InitialDirectory = @"C:\",
                Title = "Browse Audio Files",
                Multiselect = true,

                CheckFileExists = true,
                CheckPathExists = true,

                Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav;"
            };

            if (musicSearch.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (var musicFile in musicSearch.FileNames)
                {
                    HandleFileUpload(musicFile);
                }
            }
        }
        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lbxMusicList.SelectedItem != null)
            {
                DeleteSong();
            }
        }
        private void btnMute_Checked(object sender, RoutedEventArgs e)
        {
            volumeBeforeMute = (float)sldrVolume.Value;
            sldrVolume.Value = 0;
        }
        private void btnMute_Unchecked(object sender, RoutedEventArgs e)
        {
            sldrVolume.Value = volumeBeforeMute;
        }
        private void btnShuffle_Checked(object sender, RoutedEventArgs e)
        {
            isShuffle = true;
        }
        private void btnShuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            isShuffle = false;
        }
        private void btnReplay_Checked(object sender, RoutedEventArgs e)
        {
            isReplay = true;
        }
        private void btnReplay_Unchecked(object sender, RoutedEventArgs e)
        {
            isReplay = false;
        }
        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            About aboutWindow = new About();
            aboutWindow.Show();
        }

        //HELPER METHODS---------------------------------------------
        private void OnPlaybackStopped(object sender, EventArgs e)
        {
            //BUG FIX #1
            //
            //Fixed a bug where the app would crash when the play button was clicked twice. Adding btnPlayWasClicked
            //fixed the problem, but introduced a new one where the next song wouldn't automatically play because
            //play would be marked true, but never set to false. Therefore the sldrProgressBar value would technically
            //never reach the maximum value since we stated if the play button was clicked, we set it to 0 on stop.
            //The next song would not play because it hasn't reached the end.

            //BUG FIX #3
            //
            //Had to add the isMax bool to stop the method from reading a finished song as not reaching the maximum
            //It was working the other day and decided to stop. I added it all the way up top to read the max value while
            //the song was playing, so it marks the song as done before it ends.
            if (isMax)
            {
                audioFile.Position = (long)sldrProgressBar.Maximum;
                sldrProgressBar.Value = sldrProgressBar.Maximum;
                lblTime.Content = totalAudioTime.ToString("HH:mm:ss");
                PlayNextSong();
                isMax = false;
            }
            else if ((btnStopWasClicked 
                || btnNextWasClicked 
                || btnPrevWasClicked
                || btnPlayWasClicked)
                && ((!fileWasDeleted && isMax == false)))
            {
                audioFile.Position = 0;
                sldrProgressBar.Value = 0;
                lblTime.Content = "00:00:00";

                btnStopWasClicked = false;
                btnNextWasClicked = false;
                btnPrevWasClicked = false;
                btnPlayWasClicked = false;
                isMax = false;
            }
            else if (fileWasDeleted)
            {
                sldrProgressBar.Value = 0;
                lblTime.Content = "00:00:00";
                lblTotalTime.Content = "/ 00:00:00";
                fileWasDeleted = false;
            }
            else
            {
                audioFile.Position = (long)sldrProgressBar.Maximum;
                sldrProgressBar.Value = sldrProgressBar.Maximum;
                lblTime.Content = totalAudioTime.ToString("HH:mm:ss");
                PlayNextSong();
            }
        }
        private void WaveOutEvent()
        {
            outputDevice = new WaveOutEvent();
            outputDevice.PlaybackStopped += OnPlaybackStopped;
            
        }
        private void GetPathAndInitialize(MusicFile obj)
        {
            audioFile = new AudioFileReader(obj?.Path.ToString());
            outputDevice?.Init(audioFile);
        }
        private void StopAndDispose()
        {
            if (outputDevice != null)
            {
                dispatcherTimer.Stop();
                outputDevice.Dispose();
                audioFile.Dispose();
            }
        }
        private static string GetMusicFilePath()
        {
            //Changed the rootMusicPath to the Music folder that's on every Windows computer
            //#2 - When packaging into installer, it could not find the Music folder that I had created in the
            //project directory, so it wouldn't even open the app. I changed the directory to be the Music folder
            //that's included in Windows and it has seemed to work
            string rootMusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            return rootMusicPath;
        }
        private void UpdateMusicList()
        {
            lbxMusicList.Items.Clear();

            string rootMusicPath = GetMusicFilePath();

            //Getting the individual music files in the Music folder
            IEnumerable<string> musicFilesPath = Directory
                .GetFiles(rootMusicPath, "*", SearchOption.TopDirectoryOnly)
                    .Where(file => file.ToLower()
                        .EndsWith(".mp3")
                        || file.ToLower()
                            .EndsWith(".wav"));

            foreach (var musicFile in musicFilesPath)
            {
                string entry = Path.GetFileNameWithoutExtension(musicFile);
                MusicFile item = new() { FileName = entry, Path = musicFile, ShortName = FormattedName(entry) };
                lbxMusicList.Items.Add(item);
            }

            numberOfSongs = lbxMusicList.Items.Count;
        }
        private void HandleFileUpload(string musicFile)
        {
            try
            {
                //Getting the file path for the Music folder
                string rootMusicPath = GetMusicFilePath();
                string destinationPath = $"{rootMusicPath}/{Path.GetFileName(musicFile)}";

                if (File.Exists(destinationPath))
                {
                    DialogResult overwriteWarning = System.Windows.Forms.MessageBox.Show($"The file {Path.GetFileName(musicFile)} " +
                        $"already exists.\n\nDo you want to overwrite it?", "File Exists", MessageBoxButtons.YesNo);

                    if (overwriteWarning == System.Windows.Forms.DialogResult.Yes)
                    {
                        File.Copy(musicFile, destinationPath, true);
                        UpdateMusicList();
                    }
                }
                else
                {
                    File.Copy(musicFile, destinationPath, true);
                    UpdateMusicList();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Error");
            }
        }
        private void HandleSkipNext()
        {
            if (lbxMusicList.SelectedIndex == -1)
            {
                if (outputDevice != null)
                {
                    StopAndDispose();
                }

                if (isShuffle)
                {
                    HandleShuffle();
                    WaveOutEvent();
                    outputDevice.Play();
                }
                else
                {
                    lbxMusicList.SelectedItem = lbxMusicList.Items[0];
                    var firstSong = lbxMusicList.SelectedItem as MusicFile;

                    WaveOutEvent();

                    GetPathAndInitialize(firstSong);

                    OnPlay(firstSong);

                    outputDevice.Play();
                }
            }
            else if (outputDevice.PlaybackState == PlaybackState.Playing
                || (outputDevice.PlaybackState != PlaybackState.Playing
                && lbxMusicList.SelectedIndex != -1))
            {
                StopAndDispose();

                if (isShuffle)
                {
                    HandleShuffle();

                    outputDevice.Play();
                }
                else
                {
                    lbxMusicList.SelectedItem = lbxMusicList.Items[lbxMusicList.SelectedIndex + 1];
                    var nextMusic = lbxMusicList.SelectedItem as MusicFile;

                    WaveOutEvent();

                    GetPathAndInitialize(nextMusic);

                    OnPlay(nextMusic);

                    outputDevice.Play();
                }      
            }
        }
        private void HandleSkipPrev()
        {
            if (lbxMusicList.SelectedIndex == -1)
            {
                if (outputDevice != null)
                {
                    StopAndDispose();
                }

                if (isShuffle && usedSongs.Count == 1)
                {
                    HandleShufflePreviousOneItem();
                }
                else if (isShuffle && usedSongs.Count >= 2)
                {
                    HandleShufflePrevious();
                }
                else
                {
                    lbxMusicList.SelectedItem = lbxMusicList.Items[^1];
                    var firstSong = lbxMusicList.SelectedItem as MusicFile;

                    WaveOutEvent();

                    GetPathAndInitialize(firstSong);

                    OnPlay(firstSong);

                    outputDevice.Play();
                } 
            }
            else if (outputDevice.PlaybackState == PlaybackState.Playing
                || (outputDevice.PlaybackState != PlaybackState.Playing
                && lbxMusicList.SelectedIndex != -1))
            {
                if (audioFile.CurrentTime > TimeSpan.FromSeconds(3))
                {
                    audioFile.Position = 0;
                    dispatcherTimer.Start();
                    outputDevice.Play();
                }
                else
                {
                    StopAndDispose();

                    if (isShuffle && usedSongs.Count == 1)
                    {
                        HandleShufflePreviousOneItem();
                    }
                    else if (isShuffle && usedSongs.Count >= 2)
                    {
                        HandleShufflePrevious();
                    }
                    else
                    {
                        lbxMusicList.SelectedItem = lbxMusicList.Items[lbxMusicList.SelectedIndex - 1];
                        var nextMusic = lbxMusicList.SelectedItem as MusicFile;

                        WaveOutEvent();

                        GetPathAndInitialize(nextMusic);

                        OnPlay(nextMusic);
                        outputDevice.Play();
                    }
                }
            }
        }
        private void RestartPlaylist(int index)
        {
            StopAndDispose();

            lbxMusicList.SelectedItem = lbxMusicList.Items[index];
            var firstSong = lbxMusicList.SelectedItem as MusicFile;

            WaveOutEvent();

            GetPathAndInitialize(firstSong);

            OnPlay(firstSong);
            outputDevice.Play();
        }
        private void PlayNextSong()
        {
            if (sldrProgressBar.Value >= sldrProgressBar.Maximum)
            {
                songDone = true;
            }

            if (songDone)
            {
                if (isReplay)
                {
                    SongDoneReplay();
                }
                else
                {
                    SongDoneNoReplay();
                }
            }
        }
        private void DeleteSong()
        {
            var selectedMusic = lbxMusicList.SelectedItem as MusicFile;

            try
            {
                DialogResult deleteWarning = System.Windows.Forms.MessageBox.Show(
                    $"Are you sure you want to remove {selectedMusic.FileName} from your playlist?"
                    , "Delete Song"
                    , MessageBoxButtons.YesNo);

                if (deleteWarning == System.Windows.Forms.DialogResult.Yes)
                {
                    if (selectedMusic.FileName == currentSong)
                    {
                        fileWasDeleted = true;
                        StopAndDispose();
                        tbkCurrentSong.Text = "";

                        File.Delete(selectedMusic.Path);
                        

                        //Because we're not storing the individual songs in a playlist List,
                        //we have to clear the usedSongs List to prevent a crash in case a user
                        //clicks the previous button and the index is out of bounds.

                        //It also is going to play the wrong songs because they're going to shift down
                        //a number for their index. To prevent having to literally rewrite a bunch of code,
                        //we're just going to clear the list and restart. If I port this app over to get
                        //music files from a storage server, then it's not going to be that big of a deal,
                        //but since it's saved locally, I'll take the easy way out.
                        usedSongs.Clear();

                        UpdateMusicList();
                    }
                    else
                    {
                        File.Delete(selectedMusic.Path);
                        fileWasDeleted = true;

                        usedSongs.Clear();

                        UpdateMusicList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }
        private void HandleShuffle()
        {
            if (usedSongs.Count != lbxMusicList.Items.Count)
            {
                int index;

                do
                {
                    index = rnd.Next(numberOfSongs);
                }
                while (usedSongs.Contains(index));

                var randomSong = lbxMusicList.Items[index] as MusicFile;

                GetPathAndInitialize(randomSong);

                OnPlay(randomSong);

                lbxMusicList.SelectedItem = lbxMusicList.Items[index];
                usedSongs.Add(index);

                shufflePreviousCount = 2;
            }
            else
            {
                usedSongs.Clear();

                int index;

                do
                {
                    index = rnd.Next(numberOfSongs);
                }
                while (usedSongs.Contains(index));

                var randomSong = lbxMusicList.Items[index] as MusicFile;

                GetPathAndInitialize(randomSong);

                OnPlay(randomSong);

                lbxMusicList.SelectedItem = lbxMusicList.Items[index];
                usedSongs.Add(index);

                shufflePreviousCount = 2;
            }
        }
        private void HandleShufflePreviousOneItem()
        {
            lbxMusicList.SelectedItem = lbxMusicList.Items[usedSongs[0]];
            MusicFile song = lbxMusicList.SelectedItem as MusicFile;

            WaveOutEvent();

            GetPathAndInitialize(song);

            OnPlay(song);
            outputDevice.Play();
        }
        private void HandleShufflePrevious()
        {
            lbxMusicList.SelectedItem = lbxMusicList.Items[usedSongs[^shufflePreviousCount]];
            MusicFile song = lbxMusicList.SelectedItem as MusicFile;

            WaveOutEvent();

            GetPathAndInitialize(song);

            OnPlay(song);

            outputDevice.Play();

            shufflePreviousCount++;
        }
        private void SongDoneNoReplay()
        {
            try
            {
                StopAndDispose();

                if (isShuffle)
                {
                    HandleShuffle();
                    outputDevice.Play();
                }
                else
                {
                    lbxMusicList.SelectedItem = lbxMusicList.Items[lbxMusicList.SelectedIndex + 1];
                    var nextMusic = lbxMusicList.SelectedItem as MusicFile;

                    WaveOutEvent();

                    GetPathAndInitialize(nextMusic);

                    OnPlay(nextMusic);
                    outputDevice.Play();
                }

                songDone = false;
            }
            catch
            {
                if (isShuffle)
                {
                    HandleShuffle();
                }
                else
                {
                    lbxMusicList.SelectedItem = lbxMusicList.Items[0];
                    var nextMusic = lbxMusicList.SelectedItem as MusicFile;

                    WaveOutEvent();

                    GetPathAndInitialize(nextMusic);

                    OnPlay(nextMusic);

                    outputDevice.Play();

                    songDone = false;
                }
            }
        }
        private void SongDoneReplay()
        {
            StopAndDispose();

            var song = lbxMusicList.SelectedItem as MusicFile;

            WaveOutEvent();

            GetPathAndInitialize(song);

            OnPlay(song);
            outputDevice.Play();

            songDone = false;
        }
        private static string FormattedName(string entry)
        {
            string formatted = entry;

            if (entry.Length > 18)
            {
                string shortName = entry[..19];
                formatted = $"{shortName}...";

                return formatted;
            }

            return formatted;
        }
        private void RightToLeftMarquee(string song)
        {
            double endPoint = song.Length * 15;
            //A huge thanks to Razen Paul for this code for the scrolling text. Not perfect for my needs because
            //it's made for Silverlight, but it does the job pretty well I think.
            //https://asp-blogs.azurewebsites.net/razan/a-simple-text-marquee-control-in-silverlight

            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.From = canCurrentSong.Width;
            doubleAnimation.To = -(endPoint);
            doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            doubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(10));
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(doubleAnimation);
            Storyboard.SetTarget(doubleAnimation, tbkCurrentSong);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Canvas.Left)"));
            storyboard.Begin();
        }

        //TIMER AND TRACKBAR METHODS---------------------------
        private void OnPlay()
        {
            //This method is used to prevent the program from crashing when you pause then
            //play the song. Overloaded method is used for everything else.

            totalAudioTime = new(audioFile.TotalTime.Ticks);

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 1)
            };

            sldrProgressBar.Maximum = audioFile.Length;
            lblTotalTime.Content = totalAudioTime.ToString("/ HH:mm:ss");

            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Start();
        }
        private void OnPlay(MusicFile file)
        {
            //Converting audioFile to DateTime so we can use the correct time formatting
            //for the label content
            totalAudioTime = new(audioFile.TotalTime.Ticks);

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 1)
            };

            sldrProgressBar.Maximum = audioFile.Length;
            lblTotalTime.Content = totalAudioTime.ToString("/ HH:mm:ss");

            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Start();

            currentSong = file.FileName;
            tbkCurrentSong.Text = $"{file.FileName}";

            RightToLeftMarquee(currentSong);
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            
            audioTime = new(audioFile.CurrentTime.Ticks);
            sldrProgressBar.Value = audioFile.Position;
            lblTime.Content = audioTime.ToString("HH:mm:ss");

            //BUG FIX #3 - this is the added bool described in OnPlaybackStopped method
            if (audioTime >= totalAudioTime - second || sldrProgressBar.Value == sldrProgressBar.Maximum)
            {
                isMax = true;
            }
        }
        private void sldrProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //This is used for getting the green color on the trackbar when the thumb
            //moves to the right

            //This works for the audio trackbar and volume trackbar
            sldrProgressBar.SelectionStart = 0;
            sldrProgressBar.SelectionEnd = sldrProgressBar.Value;

            sldrVolume.SelectionStart = 0;
            sldrVolume.SelectionEnd = sldrVolume.Value;
        }
        private void sldrProgressBar_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            dispatcherTimer?.Stop();
            outputDevice?.Pause();
        }
        private void sldrProgressBar_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            dispatcherTimer?.Start();
            outputDevice?.Play();
            //Prevents a bug that crashes program if the user moves the thumb on the trackbar while
            //there is no audio file or output device (usually at the start of the program)
            if (outputDevice != null && dispatcherTimer != null)
            {
                audioFile.Position = (long)sldrProgressBar.Value;
            }
        }
        private void sldrVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((bool)btnMute.IsChecked && sldrVolume.Value > 0)
            {
                btnMute.IsChecked = false;
            }
            if (outputDevice != null)
            {
                outputDevice.Volume = (float)sldrVolume.Value;
            }
        }
    }

    //FILE NAME AND PATH CLASS---------------------------------------
    public class MusicFile
    {
        //This class is used for displaying the file name to the listbox correctly
        //and for getting the full file path for the selected audio file
        public string FileName { get; set; }
        public string Path { get; set; }
        public string ShortName { get; set; }
    }
}