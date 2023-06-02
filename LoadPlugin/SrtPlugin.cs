using Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SrtPlugin
{
    internal class LoadPluginClass : ISubtitlesPlugin
    {
        public string Name => "SrtPlugin";

        public string Extension => ".srt";

        public ActionItem MenuItem => ActionItem.Open | ActionItem.Save | ActionItem.SaveTranslation;

        public ICollection<DataItem> Load(bool loadTranslation)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            bool? result = dialog.ShowDialog();
            ObservableCollection<DataItem>? collection = new ObservableCollection<DataItem>();
            if (result == false)
            {
                return collection;
            }
            string path = dialog.FileName;
            if (System.IO.Path.GetExtension(path) != Extension)
            {
                MessageBox.Show("Wrong file extension!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return collection;
            }
            using (StreamReader sr = new StreamReader(path))
            {
                string? line = sr.ReadLine();  
                while(line != null)
                {
                    line = sr.ReadLine();          
                    DataItem item = new DataItem();
                    string[] spans = line.Split(" --> ");
                    item.Start = TimeSpan.Parse(spans[0]);
                    item.End = TimeSpan.Parse(spans[1]);
                    if (!loadTranslation)
                        item.Text = sr.ReadLine();
                    else
                        item.TextTrans = sr.ReadLine();
                    collection.Add(item);
                    line = sr.ReadLine();
                    line = sr.ReadLine();
                }
            }
            return collection;
        }

        public void Save(ICollection<DataItem> subtitles, bool saveTranslation)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            bool? result = dialog.ShowDialog();

            if (result == false)
            {
                return;
            }
            string path = dialog.FileName;
            if (System.IO.Path.GetExtension(path) != Extension)
            {
                MessageBox.Show("Wrong file extension!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            using (StreamWriter sw = new StreamWriter(path))
            {
                int n = 1;

                foreach (DataItem record in subtitles)
                {
                    sw.WriteLine(n++);
                    sw.WriteLine($"{record.Start.ToString(@"hh\:mm\:ss\,fff")} --> {record.End.ToString(@"hh\:mm\:ss\,fff")}");
                    if(!saveTranslation) 
                        sw.WriteLine(record.Text);
                    else
                        sw.WriteLine(record.TextTrans);
                    sw.WriteLine();
                }
            }
        }
    }
}
