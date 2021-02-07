using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = SimpleNamespace.Test.CSharpCodeFixVerifier<
    SimpleNamespace.SimpleNamespaceAnalyzer,
    SimpleNamespace.SimpleNamespaceCodeFixProvider>;

namespace SimpleNamespace.Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public async Task No_diagnostic()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Include_public_types()
        {
            var test = @"
namespace ConsoleApp.Sub
{
    public class {|#0:Foo|}
    {
        static void Main() { }
    }
}";
            var expected = VerifyCS.Diagnostic("SimpleNamespace")
                .WithLocation(0)
                .WithMessage("Namespace 'ConsoleApp.Sub' is too complex for the public type 'Foo'");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Ignore_non_public_types()
        {
            var test = @"
namespace ConsoleApp.Sub
{
    class Foo
    {
        static void Main() { }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Ignore_nested_types()
        {
            var test = @"
namespace ConsoleApp
{
    namespace Sub
    {
        public class Foo
        {
            static void Main() { }
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Works_on_structs()
        {
            var test = @"
namespace ConsoleApp.Sub
{
    public struct {|#0:Foo|}
    {
        static void Main() { }
    }
}";
            var expected = VerifyCS.Diagnostic("SimpleNamespace")
                .WithLocation(0)
                .WithMessage("Namespace 'ConsoleApp.Sub' is too complex for the public type 'Foo'");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Works_on_static()
        {
            var test = @"
namespace ConsoleApp.Sub
{
    public static class {|#0:Foo|}
    {
        static void Main() { }
    }
}";
            var expected = VerifyCS.Diagnostic("SimpleNamespace")
                .WithLocation(0)
                .WithMessage("Namespace 'ConsoleApp.Sub' is too complex for the public type 'Foo'");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Works_on_extensions()
        {
            var test = @"
namespace ConsoleApp.Sub
{
    public static class {|#0:FooExtensions|}
    {
        static void Main(this object o) { }
    }
}";
            var expected = VerifyCS.Diagnostic("SimpleNamespace")
                .WithLocation(0)
                .WithMessage("Namespace 'ConsoleApp.Sub' is too complex for the public type 'FooExtensions'");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Fix_is_applied()
        {
            var test = @"
namespace ConsoleApp.Sub
{
    public class {|#0:Foo|}
    {
        static void Main() { }
    }
}";

            var fix = @"
namespace ConsoleApp
{
    public class Foo
    {
        static void Main() { }
    }
}";

            var expected = VerifyCS.Diagnostic("SimpleNamespace")
                .WithLocation(0)
                .WithMessage("Namespace 'ConsoleApp.Sub' is too complex for the public type 'Foo'");

            await VerifyCS.VerifyCodeFixAsync(test, expected, fix);
        }
    }
}
