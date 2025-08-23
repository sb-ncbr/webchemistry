//-----------------------------------------------------------------------
//  This source is a slightly modified version from the WeakEventListner
//  in the Silverlight Toolkit Source (www.codeplex.com/Silverlight)
//---------------------------Original Source-----------------------------
// <copyright company="Microsoft">
//      (c) Copyright Microsoft Corporation.
//      This source is subject to the Microsoft Public License (Ms-PL).
//      Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//      All other rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace WebChemistry.Framework.Utils
{
    using System;
    
    /// <summary>
    /// Implements a weak event listener that allows the owner to be garbage
    /// collected if its only remaining link is an event handler.
    /// </summary>
    /// <typeparam name="TInstance">Type of rootInstance listening for the event.</typeparam>
    /// <typeparam name="TSource">Type of source for the event.</typeparam>
    /// <typeparam name="TEventArgs">Type of event arguments for the event.</typeparam>
    public class WeakEventListener<TInstance, TSource, TEventArgs> where TInstance : class
    {

        #region Fields

        /// <summary>
        /// WeakReference to the rootInstance listening for the event.
        /// </summary>    
        private WeakReference _weakInstance;
        
        /// <summary>
        /// To hold a reference to source object. With this instance the WeakEventListener 
        /// can guarantee that the handler get unregistered when listener is released.
        /// </summary>
        private WeakReference _weakSource;

        /// <summary>
        /// Delegate to the method to call when the event fires.
        /// </summary>
        private Action<TInstance, TSource, TEventArgs> _onEventAction;

        /// <summary>
        /// Delegate to the method to call when detaching from the event.
        /// </summary>
        private Action<WeakEventListener<TInstance, TSource, TEventArgs>, TSource> _onDetachAction;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instances of the WeakEventListener class.
        /// </summary>
        /// <param name="rootInstance">Instance subscribing to the event.</param>
        public WeakEventListener(TInstance instance, TSource source)
        {
            if (null == instance)
            {
                throw new ArgumentNullException("instance");
            }

            if (source == null)
                throw new ArgumentNullException("source");

            _weakInstance = new WeakReference(instance);
            _weakSource = new WeakReference(source);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the method to call when the event fires.
        /// </summary>
        public Action<TInstance, TSource, TEventArgs> OnEventAction
        {
            get { return _onEventAction; }
            set
            {
                // CHANGED: NEVER REMOVE THIS CHECK. IT CAN CAUSE A MEMORY LEAK.
                /*if (value != null && !value.Method.IsStatic)
                    throw new ArgumentException("OnEventAction method must be static " +
                              "otherwise the event WeakEventListner class does not prevent memory leaks.");*/

                _onEventAction = value;
            }
        }

        /// <summary>
        /// Gets or sets the method to call when detaching from the event.
        /// </summary>
        public Action<WeakEventListener<TInstance, TSource, TEventArgs>, TSource> OnDetachAction
        {
            get { return _onDetachAction; }
            set
            {

                // CHANGED: NEVER REMOVE THIS CHECK. IT CAN CAUSE A MEMORY LEAK.
                /*if (value != null && !value.Method.IsStatic)
                    throw new ArgumentException("OnDetachAction method must be static otherwise " +
                               "the event WeakEventListner cannot guarantee to unregister the handler.");*/

                _onDetachAction = value;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Handler for the subscribed event calls OnEventAction to handle it.
        /// </summary>
        /// <param name="source">Event source.</param>
        /// <param name="eventArgs">Event arguments.</param>
        public void OnEvent(object source, TEventArgs eventArgs)
        {
            TInstance target = (TInstance)_weakInstance.Target;
            if (null != target)
            {
                // Call registered action
                if (null != OnEventAction)
                {
                    OnEventAction(target, (TSource)source, eventArgs);
                }
            }
            else
            {
                // Detach from event
                Detach();
            }
        }

        /// <summary>
        /// Detaches from the subscribed event.
        /// </summary>
        public void Detach()
        {
            // CHANGED: 30.03.2010
            TSource source = (TSource)_weakSource.Target;
            if (null != OnDetachAction && source != null)
            {
                // CHANGED: Passing the source instance also, because of static event handlers
                OnDetachAction(this, source);
                OnDetachAction = null;
            }
        }

        #endregion
    }
}
