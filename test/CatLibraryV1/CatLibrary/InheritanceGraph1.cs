using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// m1: moniker 1, aka. cat-1.0
/// m2: moniker 2, aka. cat-2.0
/// Inheritance graph:
/// I       A         B
///  \       \       /
///   \       \(m1) /(m2)
///    \       \   /
///     C        D
///    / \      /
///   /   \(m1)/(m2)
///  E     \  /
///          F
///         / \
///        G   H
/// </summary>
namespace VersioningTest
{
    public class A
    {
        /// <summary>
        /// this method should show up as inherited member in class `D` only for `cat-1.0`
        /// </summary>
        /// <param name="param1"></param>
        public void MethodFromClassA(string param1) { }
    }

    public class B
    {
        /// <summary>
        /// this method should show up as inherited member in class `D/F/G/H` only for `cat-2.0`
        /// </summary>
        /// <param name="param1"></param>
        public void MethodFromClassB(string param1) { }
    }

    public class C : I { }

    public class D : A { }

    public class E : C { }

    /// <summary>
    /// class F should has two inheritance chains: `C` for `cat-1.0`, and `B -> D` for `cat-2.0`.
    /// </summary>
    public class F : C { }

    public class G : F { }

    public class H : F { }

    public interface I { }

    public static class VersioningExtensions
    {
        /// <summary>
        /// this extension method should show up in interface <c>I</c>, class <c>C</c> and <c>E</c> for all monikers, and in `F/G/H` for `cat-1.0` only.
        /// </summary>
        /// <param name="i"></param>
        public static void ExtMethodForI(this I i)
        {

        }

        /// <summary>
        /// this extension method should show up in class `D` for all monikers, and in `F/G/H` for `cat-2.0` only.
        /// </summary>
        /// <param name="i"></param>
        public static void ExtMethodForD(this D i)
        {

        }
    }
}
