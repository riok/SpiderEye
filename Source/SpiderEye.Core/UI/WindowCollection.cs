using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SpiderEye
{
    /// <summary>
    /// Represents a collections of windows.
    /// </summary>
    public sealed class WindowCollection : IReadOnlyList<Window>
    {
        internal event EventHandler AllWindowsClosed;

        /// <inheritdoc/>
        public Window this[int index]
        {
            get { return windows[index]; }
        }

        /// <inheritdoc/>
        public int Count
        {
            get { return windows.Count; }
        }

        /// <summary>
        /// Returns the main window (the first opened window which is still open).
        /// </summary>
        public Window MainWindow
        {
            get
            {
                return windows.FirstOrDefault();
            }
        }

        private readonly List<Window> windows = new List<Window>();

        /// <inheritdoc/>
        public IEnumerator<Window> GetEnumerator()
        {
            return windows.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal void Add(Window window)
        {
            // this method and the Closed event are both run on the main thread
            // this means manipulation of the list is safe without locks
            window.Closed += (s, e) =>
            {
                if (windows.Remove(window) && windows.Count == 0)
                {
                    AllWindowsClosed?.Invoke(this, EventArgs.Empty);
                }
            };

            windows.Add(window);
        }
    }
}
