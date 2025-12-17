using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageAIRenamer.Domain.Entities;

/// <summary>
/// Represents an image item with status tracking
/// </summary>
public class ImageItem : INotifyPropertyChanged
{
    private string _status = "Pending";
    private string _newName = "";
    private bool _isSelected = true;
    
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

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
