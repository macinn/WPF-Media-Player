using Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SaveTPlugin
{
    internal class SaveTPlugClass : ISubtitlesPlugin
    {
        public string Name => "SaveTPlug";

        public string Extension => ".srt";

        public ActionItem MenuItem => ActionItem.SaveTranslation;

        public ICollection<DataItem> Load()
        {
            return new List<DataItem>();
        }

        public void Save(ICollection<DataItem> subtitles)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            bool? result = dialog.ShowDialog();

            if (result == false)
            {
                return;
            }
            string path = dialog.FileName;
            if (Path.GetExtension(path) != Extension)
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
                    sw.WriteLine(record.TextTrans);
                    sw.WriteLine();
                }
            }
        }
    }
}
