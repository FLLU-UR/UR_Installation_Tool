using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UR_EducationSoftware.Base
{
  [Serializable]
  public class Observable : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}

