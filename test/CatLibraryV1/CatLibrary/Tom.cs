using System;

namespace CatLibrary
{
    /// <summary>
    /// Tom class is only inherit from Object. Not any member inside itself.
    /// </summary>
    public class Tom
    {
        /// <summary>
        /// This is a Tom Method with complex type as return
        /// </summary>
        /// <param name="a">A complex input</param>
        /// <param name="b">Another complex input</param>
        /// <returns>Complex @CatLibrary.TomFromBaseClass</returns>
        /// <exception cref="NotImplementedException">This is not implemented</exception>
        /// <exception cref="ArgumentException">This is the exception to be thrown when implemented</exception>
        /// <exception cref="CatException{T}">This is the exception in current documentation</exception>
        public Complex<string, TomFromBaseClass> TomMethod(Complex<TomFromBaseClass, TomFromBaseClass> a, Tuple<string, Tom> b)
        {
            throw new NotImplementedException();
        }
    }
}
