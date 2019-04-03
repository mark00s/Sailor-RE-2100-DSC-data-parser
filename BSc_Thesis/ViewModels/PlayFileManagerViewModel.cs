using BSc_Thesis.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;


namespace BSc_Thesis.ViewModels
{
    class PlayFileManagerViewModel : FileManagerViewModel, IPlayable
    {
        public DelegateCommand PlayCommand { get; }
        
        public PlayFileManagerViewModel() : base(FileExtension.Wav)
        {
            PlayCommand = new DelegateCommand(Play);
        }

        public void Play()
        {
            if (SelectedRecording != null)
                Process.Start(Path.Combine(OutputFolder, SelectedRecording));
        }
    }
}
