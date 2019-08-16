using System;

namespace CatLibrary
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="J"></typeparam>
    public class Complex<T, J>
    {
        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum ComplexFlags
        {
            /// <summary>
            /// 
            /// </summary>
            None = 0,
            /// <summary>
            /// 
            /// </summary>
            StickyWrite = 1,
            /// <summary>
            /// 
            /// </summary>
            SkipInitialPreparation = 4096, // 0x00001000
        }
    }
}
