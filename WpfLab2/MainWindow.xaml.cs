using Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace WpfLab2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    [ValueConversion(typeof(string), typeof(MediaElement))]
    public class MediaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (object.Equals(value, null)) return null;
            string path = (string)value;
            Uri uri = new Uri(path);
            return uri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class TextLenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string item)
                return $"Text: {item.Length} characters";
            else
                return "Text:";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo culture)
        {
            TimeSpan span = (TimeSpan)value;
            if (parameter != null)
                return span.ToString(@"hh\:mm\:ss\.fff");
            StringBuilder str = new StringBuilder();

            if (span.TotalSeconds < 0)
                str.Append('-');
            if (span.Duration().Hours > 0)
                str.Append(span.ToString(@"h\:mm\:ss"));
            else if (span.Duration().Minutes > 0)
                str.Append(span.ToString(@"m\:ss"));
            else
                str.Append(span.ToString(@"%s"));

            if (span.Duration().Milliseconds > 0)
                str.Append(span.ToString(@"\.FFF"));

            return str.ToString();
        }

        public object ConvertBack(object value, Type t, object parameter, CultureInfo culture)
        {
            string str = (string)value;

            if (TimeSpan.TryParseExact(str, new String[] { @"%s", @"%s\.f", @"%s\.ff", @"%s\.fff", @"%m\:", @"%m\:ss", @"%h\:mm\:ss", @"%h\:mm\:ss\.f", @"%h\:mm\:ss\.ff", @"%h\:mm\:ss\.fff", @"%m\:ss\.f", @"%m\:ss\.ff", @"%m\:ss\.fff", @"\:%m" }, System.Globalization.NumberFormatInfo.InvariantInfo, out TimeSpan span))
            {
                return span;
            }
            return DependencyProperty.UnsetValue;

        }
    }

    public partial class MainWindow : Window
    {
        public static ObservableCollection<DataItem> rows { get; set; }
        public string PathToMedia { get; set; }
        private bool mediaPlayerIsPlaying = false;
        private bool userIsDraggingSlider = false;
        public static int SelectedIndex { get; set; }
        string PluginsPath = @".\Plugins";
        Dictionary<ActionItem, Action<object, RoutedEventArgs>> ClickActions = new Dictionary<ActionItem, Action<object, RoutedEventArgs>>();
        public MainWindow()
        {
            rows = new ObservableCollection<DataItem>();
            {
                rows.Add(new DataItem());
                rows.Add(new DataItem());
                rows.Add(new DataItem());
                rows.Add(new DataItem());
                rows.Add(new DataItem());

                rows[0].Start = new TimeSpan(0, 0, 3);
                rows[1].Start = new TimeSpan(0, 0, 10);
                rows[2].Start = new TimeSpan(0, 0, 15);
                rows[3].Start = new TimeSpan(0, 0, 20);
                rows[4].Start = new TimeSpan(0, 0, 30);

                rows[0].Duration = new TimeSpan(0, 0, 5);
                rows[1].Duration = new TimeSpan(0, 0, 10);
                rows[2].Duration = new TimeSpan(0, 0, 5);
                rows[3].Duration = new TimeSpan(0, 0, 15);
                rows[4].Duration = new TimeSpan(0, 0, 8);

                rows[0].Text = "Text 0";
                rows[1].Text = "Text 1";
                rows[2].Text = "Text 2";
                rows[3].Text = "Text 3";
                rows[4].Text = "Text 4";
                rows[0].TextTrans = "TextTrans 0";
                rows[1].TextTrans = "TextTrans 1";
                rows[2].TextTrans = "TextTrans 2";
                rows[3].TextTrans = "TextTrans 3";
                rows[4].TextTrans = "TextTrans 4";
            }
            DataContext = this;
            timer.Interval = TimeSpan.FromMilliseconds(17);
            timer.Tick += time_Tick;

            //ClickActions.Add(ActionItem.Open, (sender, e) => LoadPlugins());
            //ClickActions.Add(ActionItem.Save, (sender, e) => LoadPlugins());
            //ClickActions.Add(ActionItem.SaveTranslation, (sender, e) => LoadPlugins());

            InitializeComponent();
            DataG.Items.SortDescriptions.Add(new SortDescription(DataG.Columns[0].SortMemberPath, ListSortDirection.Ascending));
            DataG.Columns[0].SortDirection = ListSortDirection.Ascending;
            //DataG.Items.Refresh();

            Directory.CreateDirectory(PluginsPath);
        }

        // https://www.c-sharpcorner.com/article/simple-plugin-architecture-using-reflection-with-wpf-projects/
        private void LoadPlugins()
        {
            try
            {

                string[] files = Directory.GetFiles(System.IO.Path.GetFullPath(PluginsPath), "*.dll");
                foreach (string file in files)
                {
                    Assembly _Assembly = Assembly.LoadFile(file);
                    List<Type>? types = _Assembly.GetTypes()?.ToList();
                    Type? type = types?.Find(a => typeof(ISubtitlesPlugin).IsAssignableFrom(a));
                    ISubtitlesPlugin? win;
                    if (type != null)
                    {
                        win = (ISubtitlesPlugin)Activator.CreateInstance(type);

                        if (win.MenuItem.HasFlag(ActionItem.Save))
                        {
                            ClickActions[ActionItem.Save] = (sender, e) => win.Save(rows, Translation.IsChecked);
                        }
                        if (win.MenuItem.HasFlag(ActionItem.SaveTranslation))
                        {
                            ClickActions[ActionItem.SaveTranslation] = (sender, e) => win.Save(rows, Translation.IsChecked);
                        }
                        if (win.MenuItem.HasFlag(ActionItem.Open))
                        {
                            ClickActions[ActionItem.Open] = (sender, e) =>
                            {
                                ICollection<DataItem>? col = win.Load(Translation.IsChecked);
                                rows.Clear();
                                foreach (DataItem item in col)
                                {
                                    rows.Add(item);
                                }
                            };
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Internal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitB(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AboutB(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("informacje");
        }

        private void sort(object sender, DataGridSortingEventArgs e)
        {
            rows = new ObservableCollection<DataItem>(rows.OrderBy(a => a.Start.TotalSeconds));
        }

        private void TransaltionB(object sender, RoutedEventArgs e)
        {
            switch (this.DataG.Columns[3].Visibility)
            {
                case Visibility.Visible:
                    {
                        RColumn.Width = new GridLength(0, GridUnitType.Star);
                        TranslationBottom.Visibility = Visibility.Collapsed;
                        this.DataG.Columns[3].Visibility = Visibility.Collapsed;
                    }
                    break;
                case Visibility.Collapsed:
                    {
                        RColumn.Width = new GridLength(50, GridUnitType.Star);
                        TranslationBottom.Visibility = Visibility.Visible;
                        this.DataG.Columns[3].Visibility = Visibility.Visible;
                    }
                    break;
            }

        }

        private void ClickB(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                PathToMedia = dialog.FileName;
            }
            Uri newUri;
            if (!Uri.TryCreate(PathToMedia, UriKind.RelativeOrAbsolute, out newUri))
                return;

            Media.Source = newUri;
            MovieSlider.Minimum = 0;
            while (!Media.NaturalDuration.HasTimeSpan) Thread.Sleep(100);
            MovieSlider.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;

            timer.Start();
        }
        static DispatcherTimer timer = new DispatcherTimer();
        private void time_Tick(object sender, EventArgs e)
        {
            if (!userIsDraggingSlider)
            {
                MovieTimer.Text = Media.Position.ToString(@"hh\:mm\:ss\.fff");
                MovieSlider.Value = Media.Position.TotalSeconds;
                StringBuilder sb = new StringBuilder();
                foreach (DataItem item in rows)
                {
                    if (item.Start < Media.Position && item.End > Media.Position)
                    {
                        if (sb.Length != 0) sb.Append("\n");
                        if (!Translation.IsChecked) sb.Append(item.Text);
                        else sb.Append(item.TextTrans);
                    }
                }
                if (sb.Length == 0)
                {
                    SubtitleBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SubtitleBox.Text = sb.ToString();
                    SubtitleBox.Visibility = Visibility.Visible;
                }
            }
            else
            {
                MovieTimer.Text = new TimeSpan((long)(TimeSpan.TicksPerSecond * MovieSlider.Value)).ToString(@"hh\:mm\:ss\.fff");
            }
        }

        private void Media_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Media.Volume += (e.Delta > 0) ? 0.1 : -0.1;
        }

        private void Media_Click(object sender, MouseButtonEventArgs e)
        {
            if (this.mediaPlayerIsPlaying)
            { this.PauseB(null, null); }
            else this.PlayB(null, null);
        }

        private void DataG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            DataItem? lbi = e.AddedItems[0] as DataItem;
            if (lbi != null)
            {
                Media.Position = lbi.Start;
                Media.Play();
            }
        }

        private void PlayB(object sender, RoutedEventArgs e)
        {
            Media.Play();
            timer.Start();
            mediaPlayerIsPlaying = true;
        }

        private void PauseB(object sender, RoutedEventArgs e)
        {
            Media.Pause();
            timer.Stop();
            mediaPlayerIsPlaying = false;
        }

        private void StopB(object sender, RoutedEventArgs e)
        {
            Media.Stop();
            timer.Stop();
            mediaPlayerIsPlaying = false;
        }

        private void MovieSlider_Drag(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Media.Position = TimeSpan.FromSeconds(MovieSlider.Value);
            MovieTimer.Text = Media.Position.ToString(@"hh\:mm\:ss\.fff");
            userIsDraggingSlider = false;
            //if(mediaPlayerIsPlaying) Media.Play();
        }

        private void MovieSlider_DragStart(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void Media_MouseRB(object sender, MouseButtonEventArgs e)
        {
            ContextMenu = (ContextMenu)Resources["ContextMenu"];
        }

        private void Context_Del(object sender, RoutedEventArgs e)
        {
            for (int i = DataG.SelectedItems.Count - 1; i >= 0; i--)
            {
                object? item = DataG.SelectedItems[i];
                DataItem? dataI = item as DataItem;
                if (dataI != null) rows.Remove(dataI);
            }
        }

        private void Context_AddAfter(object sender, RoutedEventArgs e)
        {
            DataItem item = new DataItem();
            if (DataG.SelectedItems.Count != 0)
            {
                TimeSpan max = new TimeSpan();
                foreach (object selectedItem in DataG.SelectedItems)
                {
                    DataItem currentitem = (DataItem)selectedItem;
                    if (currentitem.End > max)
                        max = currentitem.End;
                }
                item.Start = max;
                item.End = max;
            }
            rows.Add(item);
        }

        private void Context_Add(object sender, RoutedEventArgs e)
        {
            DataItem? item = new DataItem();
            if (item != null && MainWindow.rows.Count > 0)
            {
                item.Start = MainWindow.rows.Max(x => x.End);
                item.End = MainWindow.rows.Max(x => x.End);
            }
            rows.Add(item);
        }

        private void MenuClick(object sender, RoutedEventArgs e, ActionItem item)
        {
            if (ClickActions.ContainsKey(item))
            {
                ClickActions[item](sender, e);
            }
            else
            {
                LoadPlugins();
                if (ClickActions.ContainsKey(item))
                {
                    ClickActions[item](sender, e);
                }
            }
        }

        private void MenuClick_SaveTrans(object sender, RoutedEventArgs e)
        {
            MenuClick(sender, e, ActionItem.SaveTranslation);
        }

        private void MenuClick_Save(object sender, RoutedEventArgs e)
        {
            MenuClick(sender, e, ActionItem.Save);
        }

        private void MenuClick_Open(object sender, RoutedEventArgs e)
        {
            MenuClick(sender, e, ActionItem.Open);
        }

        private void Slider_ValChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!userIsDraggingSlider && Math.Abs(e.NewValue - Media.Position.TotalSeconds) > 0.2)
            {
                Media.Position = TimeSpan.FromSeconds(MovieSlider.Value);
                MovieTimer.Text = Media.Position.ToString(@"hh\:mm\:ss\.fff");
            }
        }
        private void DataG_Added(object sender, InitializingNewItemEventArgs e)
        {
            DataItem item = (DataItem)e.NewItem;
            if (item != null && MainWindow.rows.Count > 0)
            {
                item.Start = MainWindow.rows.Max(x => x.End);
                item.End = MainWindow.rows.Max(x => x.End);
            }
        }
    }
}
