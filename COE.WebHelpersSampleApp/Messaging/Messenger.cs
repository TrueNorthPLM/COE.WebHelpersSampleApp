using System.Collections.Concurrent;

namespace COE.WebHelpersSampleApp.Messaging
{
    public interface IMessenger
    {
        /// <summary>
        /// Sends a message to all subscribers of the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to send.</typeparam>
        /// <param name="message">The message instance to send.</param>
        void Send<TMessage>(TMessage message);

        /// <summary>
        /// Subscribes an object to receive all messages of the specified type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="subscriber">The object subscribing to the messages.</param>
        /// <param name="action">The action to perform when a message is received.</param>
        void Subscribe<TMessage>(object subscriber, Action<object> action);

        /// <summary>
        /// Subscribes an object to receive filtered messages of the specified type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="subscriber">The object subscribing to the messages.</param>
        /// <param name="action">The action to perform when a message is received.</param>
        /// <param name="filter">A predicate to filter messages. Only messages that satisfy the predicate will trigger the action.</param>
        void Subscribe<TMessage>(object subscriber, Action<object> action, Func<TMessage, bool> filter);

        /// <summary>
        /// Unsubscribes an object from receiving messages of the specified type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to unsubscribe from.</typeparam>
        /// <param name="subscriber">The object to unsubscribe.</param>
        void UnSubscribe<TMessage>(object subscriber);
    }

    /// <summary>
    /// Defines a simple messaging system for sending and receiving messages of various types.
    /// Supports filtered subscriptions for more granular message handling.
    /// </summary>
    public class Messenger : IMessenger
    {
        // Dictionary to hold subscriptions for different message types
        private readonly ConcurrentDictionary<Type, SynchronizedCollection<Subscription>> _subscriptions =
            new ConcurrentDictionary<Type, SynchronizedCollection<Subscription>>();

        // Dictionary to hold the current state of messages
        private readonly ConcurrentDictionary<Type, object> _currentState =
            new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of the Messenger class.
        /// </summary>
        public Messenger() { }

        /// <summary>
        /// Sends a message of type TMessage to all matching subscribers.
        /// Only subscribers whose filters match the message will receive it.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message being sent.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        public void Send<TMessage>(TMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            _currentState.AddOrUpdate(typeof(TMessage), (o) => message, (o, old) => message);

            if (_subscriptions.TryGetValue(typeof(TMessage), out var subscriptions))
            {
                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        if (subscription.ShouldReceiveMessage(message))
                        {
                            subscription.action(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception but continue processing other subscribers
                        // In a real implementation, you might want to use proper logging
                        System.Diagnostics.Debug.WriteLine($"Error in subscriber: {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes an object to receive messages of type TMessage with no filtering.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to subscribe to.</typeparam>
        /// <param name="subscriber">The object subscribing to the message.</param>
        /// <param name="action">The action to perform when a message is received.</param>
        /// <remarks>
        /// This overload is equivalent to subscribing with a filter that always returns true.
        /// </remarks>
        public void Subscribe<TMessage>(object subscriber, Action<object> action)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Subscribe<TMessage>(subscriber, action, _ => true);
        }

        /// <summary>
        /// Subscribes an object to receive filtered messages of type TMessage.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to subscribe to.</typeparam>
        /// <param name="subscriber">The object subscribing to the message.</param>
        /// <param name="action">The action to perform when a message is received.</param>
        /// <param name="filter">A predicate that determines whether a message should be delivered to this subscriber.
        /// The message will only be delivered if the filter returns true.</param>
        /// <remarks>
        /// The filter is evaluated after type checking but before message delivery.
        /// If there is a current state for the message type and it passes the filter,
        /// the action will be invoked immediately with the current state.
        /// </remarks>
        public void Subscribe<TMessage>(object subscriber, Action<object> action, Func<TMessage, bool> filter)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            var newSubscriber = new Subscription(
                subscriber,
                action,
                obj =>
                {
                    try
                    {
                        return obj is TMessage msg && filter(msg);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception but continue processing
                        System.Diagnostics.Debug.WriteLine($"Error in filter: {ex}");
                        return false;
                    }
                });

            var subscriptions = _subscriptions.GetOrAdd(
                typeof(TMessage),
                _ => new SynchronizedCollection<Subscription>());

            subscriptions.Add(newSubscriber);

            if (_currentState.TryGetValue(typeof(TMessage), out var currentState))
            {
                try
                {
                    if (newSubscriber.ShouldReceiveMessage(currentState))
                    {
                        action(currentState);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception but don't prevent subscription
                    System.Diagnostics.Debug.WriteLine($"Error in initial message delivery: {ex}");
                }
            }
        }


        /// <summary>
        /// Unsubscribes an object from receiving messages of type TMessage.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to unsubscribe from.</typeparam>
        /// <param name="subscriber">The object to unsubscribe.</param>
        public void UnSubscribe<TMessage>(object subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (_subscriptions.TryGetValue(typeof(TMessage), out var subscriptions))
            {
                var subscriptionsToRemove = subscriptions
                    .FirstOrDefault(s => s.subscriber == subscriber);

                if (subscriptionsToRemove != null)
                {
                    subscriptions.Remove(subscriptionsToRemove);
                }
            }
        }

        /// <summary>
        /// Represents a subscription to a message type with optional filtering.
        /// </summary>
        public record Subscription
        {
            /// <summary>
            /// Gets the object that subscribed to receive messages.
            /// </summary>
            public object subscriber { get; }

            /// <summary>
            /// Gets the action to be performed when a message is received.
            /// </summary>
            public Action<object> action { get; }

            /// <summary>
            /// The filter predicate that determines whether a message should be delivered.
            /// </summary>
            private readonly Func<object, bool> filter;

            /// <summary>
            /// Initializes a new instance of the Subscription class with no filtering.
            /// </summary>
            /// <param name="subscriber">The object subscribing to messages.</param>
            /// <param name="action">The action to perform when a message is received.</param>
            public Subscription(object subscriber, Action<object> action)
                : this(subscriber, action, _ => true)
            {
            }

            /// <summary>
            /// Initializes a new instance of the Subscription class with filtering.
            /// </summary>
            /// <param name="subscriber">The object subscribing to messages.</param>
            /// <param name="action">The action to perform when a message is received.</param>
            /// <param name="filter">A predicate that determines whether a message should be delivered.</param>
            public Subscription(object subscriber, Action<object> action, Func<object, bool> filter)
            {
                this.subscriber = subscriber;
                this.action = action;
                this.filter = filter;
            }

            /// <summary>
            /// Determines whether a message should be delivered to this subscription.
            /// </summary>
            /// <param name="message">The message to evaluate.</param>
            /// <returns>True if the message should be delivered, false otherwise.</returns>
            public bool ShouldReceiveMessage(object message)
            {
                return filter(message);
            }
        }
    }
}
