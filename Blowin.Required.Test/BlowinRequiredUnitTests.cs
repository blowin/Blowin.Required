using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Blowin.Required.Test.CSharpCodeFixVerifier<
    Blowin.Required.BlowinRequiredAnalyzer,
    Blowin.Required.BlowinRequiredCodeFixProvider>;

namespace Blowin.Required.Test
{
    [TestClass]
    public class BlowinRequiredUnitTest
    {
        [DataTestMethod]
        [DataRow(@"class Person
        {
        public string Name { get; set; }
        public int Age { get; set; }
    }")]
        [DataRow(@"class Person
        {
        public string Name { get; set; }
        public int Age { get; set; }

        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }
    }")]
        [DataRow(@"class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }
        }")]
        public async Task Valid(string test)
        {
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [DataTestMethod]
        [DataRow(@"class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            private Person()
            {
                Age = 10;
            }

            public static Person Create()
            {
                return {|#0:new Person()
                {
                    Name = ""
                }|};
            }
        }")]
        public async Task Invalid(string test)
        {
            var expected = VerifyCS.Diagnostic(BlowinRequiredAnalyzer.DiagnosticId).WithLocation(0).WithArguments("Age");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
