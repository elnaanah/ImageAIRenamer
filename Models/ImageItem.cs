using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageAIRenamer.Models
{
    public class ImageItem : INotifyPropertyChanged
    {
        private string _status = "Pending";
        private string _newName = "";
        
        public required string FilePath { get; set; }
        public required string OriginalName { get; set; }
        
        public string NewName
        {
            get => _newName;
            set { _newName = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
