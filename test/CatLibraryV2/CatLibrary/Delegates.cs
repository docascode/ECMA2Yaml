using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatLibrary
{

    //delegate
    /// <summary>
    /// Delegate in the namespace
    /// </summary>
    /// <param name="pics">a name list of pictures.</param>
    /// <param name="name">give out the needed name.</param>
    public delegate void MRefNormalDelegate(List<string> pics, out string name);

    /// <summary>
    /// Generic delegate with many constrains.
    /// </summary>
    /// <typeparam name="K">Generic K.</typeparam>
    /// <typeparam name="T">Generic T.</typeparam>
    /// <typeparam name="L">Generic L.</typeparam>
    /// <param name="k">Type K.</param>
    /// <param name="t">Type T.</param>
    /// <param name="l">Type L.</param>
    public delegate void MRefDelegate<in K, T, L>(K k, T t, L l)
        where K : class, IComparable
        where T : struct
        where L : Tom, IEnumerable<long>;

    /// <summary>
    /// Fake delegate
    /// </summary>
    /// <typeparam name="T">Fake para</typeparam>
    /// <param name="num">Fake para</param>
    /// <param name="name">Fake para</param>
    /// <param name="scores">Optional Parameter.</param>
    /// <returns>Return a fake number to confuse you.</returns>
    public delegate int FakeDelegate<out T>(long num, string name, params object[] scores);
}
