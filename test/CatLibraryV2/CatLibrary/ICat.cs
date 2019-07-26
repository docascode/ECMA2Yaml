using System;

namespace CatLibrary
{
    /// <summary>
    /// Cat's interface
    /// </summary>
    public interface ICat : IAnimal
    {
        //event
        /// <summary>
        /// eat event of cat. Every cat must implement this event.
        /// </summary>
        event EventHandler eat;

        /// <summary>
        /// All cat can catch a Jerry.
        /// </summary>
        void CatchJerry();
    }
}
