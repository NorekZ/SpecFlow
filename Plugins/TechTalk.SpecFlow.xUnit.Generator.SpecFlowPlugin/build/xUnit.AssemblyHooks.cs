using global::System;
using global::Xunit;
using global::TechTalk.SpecFlow;

namespace InternalSpecFlow
{
    public class XUnitAssemblyFixture
    {
        static XUnitAssemblyFixture()
        {
            var currentAssembly = typeof(XUnitAssemblyFixture).Assembly;

            TestRunnerManager.OnTestRunStartAsync(currentAssembly).GetAwaiter().GetResult();
        }
    }
}

