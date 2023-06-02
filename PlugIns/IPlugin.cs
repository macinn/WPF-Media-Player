using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Plugins
{
    public interface ISubtitlesPlugin
    {
        string Name { get; }
        string Extension { get; }
        ActionItem MenuItem { get; }
        ICollection<DataItem> Load(bool loadTranslation);
        void Save(ICollection<DataItem> subtitles, bool saveTranslation);
    }
    [Flags]
    public enum ActionItem
    {
        Save = 1,
        SaveTranslation = 2,
        Open = 4,
    }
    public partial class DataItem : INotifyPropertyChanged
    {
        public TimeSpan Duration
        {
            get { return End.Subtract(Start); }
            set { End = Start.Add(value); }
        }
        public TimeSpan Start
        {
            get { return start; }
            set
            {
                start = value;
                NotifyPropertyChanged("Duration"); NotifyPropertyChanged();
            }
        }
        private TimeSpan start, end;
        private string s1, s2;
        public TimeSpan End
        {
            get { return end; }
            set { end = value; NotifyPropertyChanged("Duration"); NotifyPropertyChanged(); }
        }
        public string Text
        {
            get { return s1; }
            set { s1 = value; NotifyPropertyChanged(); }
        }
        public string TextTrans
        {
            get { return s2; }
            set { s2 = value; NotifyPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public DataItem()
        {
            this.Start = TimeSpan.Zero;
            this.End = TimeSpan.Zero;
            Text = string.Empty;
            TextTrans = string.Empty;
        }
    }
}
