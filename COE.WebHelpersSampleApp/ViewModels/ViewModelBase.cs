using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace COE.WebHelpersSampleApp.ViewModels
{
    /// <summary>
    /// Interface that defines the base functionality for ViewModel classes.
    /// </summary>
    public interface IViewModelBase : IDisposable
    {
        /// <summary>
        /// Indicates whether the ViewModel has any validation errors.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Gets or sets the parent ViewModel of the current ViewModel.
        /// </summary>
        IViewModelBase? ParentViewModel { get; set; }

        /// <summary>
        /// Traverses up the ViewModel hierarchy to find an ancestor ViewModel of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of ancestor ViewModel to find.</typeparam>
        /// <param name="childViewModel">The starting point ViewModel from which to search upwards.</param>
        /// <returns>The first ancestor ViewModel of the specified type, or default(T) (null) if no matching ancestor is found.</returns>
        T? GetAncestorViewModel<T>(IViewModelBase childViewModel) where T : IViewModelBase;

        /// <summary>
        /// Traverses up the ViewModel hierarchy to find an ancestor ViewModel of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of ancestor ViewModel to find.</typeparam>
        /// <returns>The first ancestor ViewModel of the specified type, or default(T) (null) if no matching ancestor is found.</returns>
        T? GetAncestorViewModel<T>() where T : IViewModelBase;

        /// <summary>
        /// Event that is raised when the validation errors change.
        /// </summary>
        event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>
        /// Event that is raised when a property changes.
        /// </summary>
        event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Adds a validation error for a specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has an error.</param>
        /// <param name="errorMessage">The error message to be added.</param>
        void AddError(string propertyName, string errorMessage);

        /// <summary>
        /// Clears the validation errors for a specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property for which to clear errors.</param>
        void ClearErrors(string propertyName);

        /// <summary>
        /// Retrieves the validation errors for a specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property to retrieve errors for.</param>
        /// <returns>An enumerable collection of error messages, or null if there are no errors.</returns>
        IEnumerable GetErrors(string? propertyName);
    }

    /// <summary>
    /// Abstract base class for ViewModels that implements INotifyPropertyChanged and INotifyDataErrorInfo.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo, IViewModelBase
    {
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ViewModelBase class with an optional parent ViewModel.
        /// </summary>
        /// <param name="parentViewModel">The parent ViewModel to set. If not provided, defaults to null.</param>
        protected ViewModelBase(IViewModelBase? parentViewModel = null)
        {
            ParentViewModel = parentViewModel;
        }

        /// <summary>
        /// Initializes a new instance of the ViewModelBase class.
        /// </summary>
        protected ViewModelBase() { }

        /// <summary>
        /// Gets or sets the parent ViewModel of the current ViewModel.
        /// </summary>
        public IViewModelBase? ParentViewModel { get; set; } = null;

        /// <summary>
        /// Traverses up the ViewModel hierarchy to find an ancestor ViewModel of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of ancestor ViewModel to find.</typeparam>
        /// <param name="childViewModel">The starting point ViewModel from which to search upwards.</param>
        /// <returns>The first ancestor ViewModel of the specified type, or default(T) (null) if no matching ancestor is found.</returns>
        public T? GetAncestorViewModel<T>(IViewModelBase childViewModel) where T : IViewModelBase
        {
            return childViewModel.ParentViewModel switch
            {
                T matchingParent => matchingParent,
                null => default,
                _ => GetAncestorViewModel<T>(childViewModel.ParentViewModel)
            };
        }

        /// <summary>
        /// Traverses up the ViewModel hierarchy to find an ancestor ViewModel of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of ancestor ViewModel to find.</typeparam>
        /// <returns>The first ancestor ViewModel of the specified type, or default(T) (null) if no matching ancestor is found.</returns>
        public T? GetAncestorViewModel<T>() where T : IViewModelBase
        {
            return GetAncestorViewModel<T>(this);
        }

        /// <summary>
        /// Dictionary that stores validation errors for properties.
        /// </summary>
        public readonly Dictionary<string, List<string>> _propertyErrors = new Dictionary<string, List<string>>();

        /// <summary>
        /// Allows a protected way to get the Property Errors
        /// </summary>
        /// <returns></returns>
        protected IReadOnlyDictionary<string, List<string>> GetCurrentErrors()
        {
            return _propertyErrors;
        }

        /// <summary>
        /// Indicates whether the ViewModel has any validation errors.
        /// </summary>
        public bool HasErrors => _propertyErrors.Any();

        /// <summary>
        /// Event that is raised when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Event that is raised when the validation errors change.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>
        /// Retrieves the validation errors for a specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property to retrieve errors for.</param>
        /// <returns>An enumerable collection of error messages, or null if there are no errors.</returns>
        public IEnumerable GetErrors(string? propertyName)
        {
            // If propertyName is null, return all property names that have errors
            if (propertyName == null)
            {
                return _propertyErrors.Keys;
            }

            // Otherwise return the errors for the specific property
            return _propertyErrors.TryGetValue(propertyName, out var errors)
                ? errors
                : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Adds a validation error for a specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has an error.</param>
        /// <param name="errorMessage">The error message to be added.</param>
        public void AddError(string propertyName, string errorMessage)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

            if (!_propertyErrors.ContainsKey(propertyName))
            {
                _propertyErrors[propertyName] = new List<string>();
            }

            if (!_propertyErrors[propertyName].Contains(errorMessage))
            {
                _propertyErrors[propertyName].Add(errorMessage);
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// Clears the validation errors for a specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property for which to clear errors.</param>
        public void ClearErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

            if (_propertyErrors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// Raises the ErrorsChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property whose errors have changed.</param>
        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources, false if being finalized.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Clear events
                PropertyChanged = null;
                ErrorsChanged = null;

                // Clear parent reference
                ParentViewModel = null;

                // Clear errors dictionary
                _propertyErrors.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~ViewModelBase()
        {
            Dispose(false);
        }
    }
}
