using System.Windows.Input;

namespace COE.WebHelpersSampleApp.Commands
{
    /// <summary>
    /// A base class for implementing commands in WPF applications. 
    /// This class allows for asynchronous command execution and provides 
    /// mechanisms to determine if the command can execute.
    /// </summary>
    public class CommandsBase : ICommand
    {
        private readonly Func<object?, Task> _executeFunc;
        private readonly Predicate<object?>? _canExecuteFunc;

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        /// <remarks>
        /// This event is used by the command infrastructure to notify listeners
        /// that the ability of the command to execute has changed.
        /// </remarks>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandsBase"/> class with
        /// an action to execute and a predicate to determine if the command can execute.
        /// </summary>
        /// <param name="executeAction">
        /// An <see cref="Action{T}"/> that defines the action to execute when the command is invoked.
        /// </param>
        /// <param name="canExecute">
        /// A <see cref="Predicate{T}"/> that defines the condition for whether the command can execute.
        /// </param>
        public CommandsBase(Action<object?> executeAction, Predicate<object?>? canExecute = null)
            : this(p => { executeAction(p); return Task.CompletedTask; }, canExecute)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandsBase"/> class with
        /// an asynchronous function to execute and an optional predicate to determine if the command can execute.
        /// </summary>
        /// <param name="executeFunc">
        /// A <see cref="Func{T, TResult}"/> that defines the asynchronous function to execute when the command is invoked.
        /// </param>
        /// <param name="canExecute">
        /// A <see cref="Predicate{T}"/> that defines the condition for whether the command can execute.
        /// </param>
        public CommandsBase(Func<object?, Task> executeFunc, Predicate<object?>? canExecute = null)
        {
            _executeFunc = executeFunc ?? throw new ArgumentNullException(nameof(executeFunc));
            _canExecuteFunc = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute.
        /// </summary>
        /// <param name="parameter">
        /// An optional parameter to be used by the <see cref="CanExecute"/> method.
        /// </param>
        /// <returns>
        /// <c>true</c> if the command can execute; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecuteFunc?.Invoke(parameter) ?? true;
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="parameter">
        /// An optional parameter to be used by the <see cref="Execute"/> method.
        /// </param>
        public async void Execute(object? parameter)
        {
            await _executeFunc(parameter);
        }

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event to indicate that
        /// the ability of the command to execute has changed.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
