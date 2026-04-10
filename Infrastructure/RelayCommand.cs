using System;
using System.Windows.Input;

namespace TimeTracker;

public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;

    public RelayCommand(Action execute)
    {
        _execute = _ => execute();
    }

    public RelayCommand(Action<object> execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter) => true;

    public void Execute(object parameter) => _execute(parameter);
}
