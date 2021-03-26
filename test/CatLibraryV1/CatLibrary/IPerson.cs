using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatLibrary
{
    /// <summary>
    /// This is interface of all Person.
    /// </summary>
    public interface IPerson
    {
        /// <summary>
        /// Person's ID
        /// </summary>
        string ID { get; set; }

        /// <summary>
        /// Person's Age
        /// </summary>
        int Age { get; set; }

        /// <summary>
        /// Person's Name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Speak with a language in IPerson interface
        /// </summary>
        /// <param name="language">The language that person speak</param>
        void Speak(string language);

        /// <summary>
        /// Calculates the height in IPerson interface.
        /// </summary>
        /// <param name="age">Tha age of the cat, in years.</param>
        /// <returns>The height of the cat in cm.</returns>
        int GetHeight(int age);
    }
}
