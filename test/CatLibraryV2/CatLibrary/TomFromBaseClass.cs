using MRef.Demo.Enumeration;

namespace CatLibrary
{
    /// <summary>
    /// *TomFromBaseClass* inherits from @CatLibrary.Tom
    /// </summary>
    public class TomFromBaseClass : Tom
    {

        /// <summary>
        /// This is a #ctor with parameter
        /// </summary>
        /// <param name="k"></param>
        public TomFromBaseClass(int k)
        {

        }

        /// <summary>
        /// This is a <b>nested</b> class inside <see cref="TomFromBaseClass"/>
        /// </summary>
        public class Jerry
        {
            /// <summary>
            /// Jerry has different <see cref="ColorType">colors</see>.
            /// </summary>
            /// <param name="color">Color of Jerry.</param>
            public Jerry(ColorType color)
            { }
        }
    }

    /// <summary>
    /// This class extends <see cref="TomFromBaseClass.Jerry"/>.
    /// </summary>
    public static class JerryExtension
    {
        /// <summary>
        /// Jerry can play with a Tom.
        /// </summary>
        /// <param name="jerry"></param>
        /// <param name="tom"></param>
        public static void PlayWith(this TomFromBaseClass.Jerry jerry, Tom tom) { }
    }
}
