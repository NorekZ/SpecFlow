﻿using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace TechTalk.SpecFlow.RuntimeTests
{
    
    public class TestRunnerManagerStaticApiTest : IAsyncLifetime
    {
        private readonly Assembly thisAssembly = Assembly.GetExecutingAssembly();
        private readonly Assembly anotherAssembly = typeof(TestRunnerManager).Assembly;

        public async Task InitializeAsync()
        {
            await TestRunnerManager.ResetAsync();
        }

        [Fact]
        public async Task GetTestRunner_without_arguments_should_return_TestRunner_instance()
        {
            var testRunner = await TestRunnerManager.GetTestRunnerAsync();

            testRunner.Should().NotBeNull();
            testRunner.Should().BeOfType<TestRunner>();
        }

        [Fact]
        public async Task GetTestRunner_should_return_different_instances_for_different_assemblies()
        {
            var testRunner1 = await TestRunnerManager.GetTestRunnerAsync(thisAssembly);
            var testRunner2 = await TestRunnerManager.GetTestRunnerAsync(anotherAssembly);

            testRunner1.Should().NotBe(testRunner2);
        }

        [Fact]
        public void GetTestRunnerManager_without_arguments_should_return_an_instance_for_the_calling_assembly()
        {
            var testRunnerManager = TestRunnerManager.GetTestRunnerManagerAsync();

            testRunnerManager.Should().NotBeNull();
            //testRunnerManager.TestAssembly.Should().BeSameAs(thisAssembly);
        }

        [Fact]
        public async Task GetTestRunnerManager_should_return_null_when_called_with_no_create_flag_and_there_was_no_instance_created_yet()
        {
            await TestRunnerManager.ResetAsync();

            var testRunnerManager = await TestRunnerManager.GetTestRunnerManagerAsync(createIfMissing: false);

            testRunnerManager.Should().BeNull();
        }

        [Binding]
        public class AfterTestRunTestBinding
        {
            public static int AfterTestRunCallCount = 0;

            [AfterTestRun]
            public static void AfterTestRun()
            {
                AfterTestRunCallCount++;
            }
        }

        [Fact]
        public async Task OnTestRunEnd_should_fire_AfterTestRun_events()
        {
            // make sure a test runner is initialized
            await TestRunnerManager.GetTestRunnerAsync(thisAssembly);

            AfterTestRunTestBinding.AfterTestRunCallCount = 0; //reset
            await TestRunnerManager.OnTestRunEndAsync(thisAssembly);

            AfterTestRunTestBinding.AfterTestRunCallCount.Should().Be(1);
        }

        [Fact]
        public async Task OnTestRunEnd_without_arguments_should_fire_AfterTestRun_events_for_calling_assembly()
        {
            // make sure a test runner is initialized
            await TestRunnerManager.GetTestRunnerAsync(thisAssembly);

            AfterTestRunTestBinding.AfterTestRunCallCount = 0; //reset
            await TestRunnerManager.OnTestRunEndAsync(thisAssembly);

            AfterTestRunTestBinding.AfterTestRunCallCount.Should().Be(1);
        }

        [Fact]
        public async Task OnTestRunEnd_should_not_fire_AfterTestRun_events_multiple_times()
        {
            // make sure a test runner is initialized
            await TestRunnerManager.GetTestRunnerAsync(thisAssembly);

            AfterTestRunTestBinding.AfterTestRunCallCount = 0; //reset
            await TestRunnerManager.OnTestRunEndAsync(thisAssembly);
            await TestRunnerManager.OnTestRunEndAsync(thisAssembly);

            AfterTestRunTestBinding.AfterTestRunCallCount.Should().Be(1);
        }

        [Fact]
        public async Task DomainUnload_event_should_not_fire_AfterTestRun_events_multiple_times_after_OnTestRunEnd()
        {
            // make sure a test runner is initialized
            await TestRunnerManager.GetTestRunnerAsync(thisAssembly);

            AfterTestRunTestBinding.AfterTestRunCallCount = 0; //reset
            await TestRunnerManager.OnTestRunEndAsync(thisAssembly);

            // simulating DomainUnload event
            var trm = (TestRunnerManager)await TestRunnerManager.GetTestRunnerManagerAsync(thisAssembly);
            await trm.OnDomainUnloadAsync();

            AfterTestRunTestBinding.AfterTestRunCallCount.Should().Be(1);
        }

        public async Task DisposeAsync()
        {
        }
    }
}
