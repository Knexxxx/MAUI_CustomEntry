using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtendedDataEntry;

public partial class MainViewModel: ObservableValidator
{
    [ObservableProperty] 
    private DataEntry.EntryStates _entryState;
    
    [ObservableProperty] 
    private string _someValue = "ABC";
    
    [ObservableProperty] 
    private string _proposedValue = "";

    partial void OnProposedValueChanged(string value)
    {
        Debug.WriteLine("DataEntry proposes Value:" + value);
        // Let's say we have fully validated the proposed value:
        SomeValue = value;
        // now we need to tell the DataEntry whether the value has been accepted or rejected
        EntryState = DataEntry.EntryStates.Locked;

        

    }
}