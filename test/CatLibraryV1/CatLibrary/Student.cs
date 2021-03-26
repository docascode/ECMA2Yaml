using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatLibrary
{
    /// <summary>
    ///
    /// </summary>
    /// <inheritdoc />
    public class Student : IPerson
    {
        /// <summary>
        ///
        /// </summary>
        /// <inheritdoc />
        public string ID { get; set; }

        /// <inheritdoc />
        public int Age { get; set; }

        /// <summary>
        /// Student's Name
        /// </summary>
        /// <inheritdoc />
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="language"></param>
        /// <inheritdoc />
        public void Speak(string language)
        {

        }

        /// <summary>
        /// Calculates the height in Student class.
        /// </summary>
        /// <param name="age"></param>
        /// <returns></returns>
        /// <inheritdoc />
        public int GetHeight(int age)
        {
            if (age < 20)
            {
                return age * 10;
            }
            else
            {
                return 180;
            }
        }

        /// <inheritdoc cref="M:CatLibrary.IPerson.GetHeight(System.Int32)" />
        public int GetHeight1(int age)
        {
            if (age < 20)
            {
                return age * 10;
            }
            else
            {
                return 180;
            }
        }

        /// <inheritdoc cref="M:CatLibrary.IPerson.GetHeight2(System.Int32)" />
        public int GetHeight2(int age)
        {
            if (age < 20)
            {
                return age * 10;
            }
            else
            {
                return 180;
            }
        }
    }
}
