using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Coherent.MvvmHelpers {
  ///<summary>
  /// Gneric ICommand implementation to assist with processing
  /// commands in MVVM.
  ///</summary>
  public class DelegateCommand<T>: ICommand {
    /// <summary>
    /// Defines the method for the action to be executed.
    /// </summary>
    private readonly Action<T> executeAction;

    /// <summary>
    /// Defines the function that determines whether the
    /// action can be executed.
    /// </summary>
    private readonly Func<T, bool> canExecuteAction;

    ///<summary>
    /// Defines an event to raise when the values that
    /// affect "CanExecute" are changed.
    public event EventHandler CanExecuteChanged;

    /// <summary>
    /// Constucts an object that can always execute.
    /// </summary>
    /// <param name="currentExecuteAction"></param>
    /// <remarks></remarks>
    public DelegateCommand(Action<T> executeAction)
      : this(executeAction, null) {
    }

    /// <summary>
    /// Constructs an object to execute.
    /// </summary>
    public DelegateCommand(Action<T> executeAction,
        Func<T, bool> canExecuteAction) {
      this.executeAction = executeAction;
      this.canExecuteAction = canExecuteAction;
    }

    /// <summary>
    /// Defines the method that determines whether the command can
    /// execute in its current state.
    /// </summary>
    public bool CanExecute(object parameter) {
      if(canExecuteAction != null) {
        return canExecuteAction((T)parameter);
      }
      return true;
    }

    /// <summary>
    /// Defines the method to call when the command is invoked.
    /// </summary>
    public void Execute(object parameter) {
      if(CanExecute(parameter)) {
        executeAction((T)parameter);
      }
    }

    public virtual void RaiseCanExecuteChanged() {
      if(CanExecuteChanged!=null) {
        CanExecuteChanged(this, null);
      }
    }
  }
}