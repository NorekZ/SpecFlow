using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BoDi;
using TechTalk.SpecFlow.Bindings.Discovery;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace TechTalk.SpecFlow
{
    public class TestRunnerManager : ITestRunnerManager
    {
        protected readonly IObjectContainer globalContainer;
        protected readonly IContainerBuilder containerBuilder;
        protected readonly SpecFlowConfiguration specFlowConfiguration;
        protected readonly IRuntimeBindingRegistryBuilder bindingRegistryBuilder;

        private readonly ITestTracer testTracer;
        private readonly Dictionary<int, ITestRunner> testRunnerRegistry = new Dictionary<int, ITestRunner>();
        private readonly SemaphoreSlim syncRootSemaphore = new SemaphoreSlim(1);
        private bool isTestRunInitialized;
        private object disposeLockObj;
        private readonly SemaphoreSlim createTestRunnerSemaphore = new SemaphoreSlim(1);

        public Assembly TestAssembly { get; private set; }
        public Assembly[] BindingAssemblies { get; private set; }

        public bool IsMultiThreaded { get { return testRunnerRegistry.Count > 1; } }

        public TestRunnerManager(IObjectContainer globalContainer, IContainerBuilder containerBuilder, SpecFlowConfiguration specFlowConfiguration, IRuntimeBindingRegistryBuilder bindingRegistryBuilder,
            ITestTracer testTracer)
        {
            this.globalContainer = globalContainer;
            this.containerBuilder = containerBuilder;
            this.specFlowConfiguration = specFlowConfiguration;
            this.bindingRegistryBuilder = bindingRegistryBuilder;
            this.testTracer = testTracer;
        }

        public virtual async Task<ITestRunner> CreateTestRunnerAsync(int threadId)
        {
            var testRunner = CreateTestRunnerInstance();
            testRunner.InitializeTestRunner(threadId);

            await createTestRunnerSemaphore.WaitAsync();
            try
            {
                if (!isTestRunInitialized)
                {
                    await InitializeBindingRegistryAsync(testRunner);
                    isTestRunInitialized = true;
                }
            }
            finally
            {
                createTestRunnerSemaphore.Release();
            }

            return testRunner;
        }

        protected virtual async Task InitializeBindingRegistryAsync(ITestRunner testRunner)
        {
            BindingAssemblies = GetBindingAssemblies();
            BuildBindingRegistry(BindingAssemblies);

            await testRunner.OnTestRunStartAsync();

            EventHandler domainUnload = delegate { OnDomainUnloadAsync().Wait(); };
            AppDomain.CurrentDomain.DomainUnload += domainUnload;
            AppDomain.CurrentDomain.ProcessExit += domainUnload;
        }

        protected virtual Assembly[] GetBindingAssemblies()
        {
            var bindingAssemblies = new List<Assembly> { TestAssembly };

            var assemblyLoader = globalContainer.Resolve<IBindingAssemblyLoader>();
            bindingAssemblies.AddRange(
                specFlowConfiguration.AdditionalStepAssemblies.Select(assemblyLoader.Load));
            return bindingAssemblies.ToArray();
        }

        protected virtual void BuildBindingRegistry(IEnumerable<Assembly> bindingAssemblies)
        {
            foreach (Assembly assembly in bindingAssemblies)
            {
                bindingRegistryBuilder.BuildBindingsFromAssembly(assembly);
            }
            bindingRegistryBuilder.BuildingCompleted();
        }

        protected internal virtual async Task OnDomainUnloadAsync()
        {
            await DisposeAsync();
        }

        private async Task FireTestRunEndAsync()
        {
            // this method must not be called multiple times
            var onTestRunnerEndExecutionHost = testRunnerRegistry.Values.FirstOrDefault();
            if (onTestRunnerEndExecutionHost != null)
                await onTestRunnerEndExecutionHost.OnTestRunEndAsync();
        }

        protected virtual ITestRunner CreateTestRunnerInstance()
        {
            var testThreadContainer = containerBuilder.CreateTestThreadContainer(globalContainer);

            return testThreadContainer.Resolve<ITestRunner>();
        }

        public void Initialize(Assembly assignedTestAssembly)
        {
            TestAssembly = assignedTestAssembly;
        }

        public virtual async Task<ITestRunner> GetTestRunnerAsync(int threadId)
        {
            try
            {
                return await GetTestRunnerWithoutExceptionHandlingAsync(threadId);

            }
            catch (Exception ex)
            {
                testTracer.TraceError(ex);
                throw;
            }
        }

        private async Task<ITestRunner> GetTestRunnerWithoutExceptionHandlingAsync(int threadId)
        {
            ITestRunner testRunner;
            if (!testRunnerRegistry.TryGetValue(threadId, out testRunner))
            {
                await syncRootSemaphore.WaitAsync();
                try
                {
                    if (!testRunnerRegistry.TryGetValue(threadId, out testRunner))
                    {
                        testRunner = await CreateTestRunnerAsync(threadId);
                        testRunnerRegistry.Add(threadId, testRunner);

                        if (IsMultiThreaded)
                        {
                            FeatureContext.DisableSingletonInstance();
                            ScenarioContext.DisableSingletonInstance();
                            ScenarioStepContext.DisableSingletonInstance();
                        }
                    }
                }
                finally
                {
                    syncRootSemaphore.Release();
                }
            }
            return testRunner;
        }

        public virtual async Task DisposeAsync()
        {
            if (Interlocked.CompareExchange<object>(ref disposeLockObj, new object(), null) == null)
            {
                await FireTestRunEndAsync();

                // this call dispose on this object, but the disposeLockObj will avoid double execution
                globalContainer.Dispose();

                testRunnerRegistry.Clear();
                await OnTestRunnerManagerDisposed(this);
            }
        }

        #region Static API

        private static readonly Dictionary<Assembly, ITestRunnerManager> testRunnerManagerRegistry = new Dictionary<Assembly, ITestRunnerManager>(1);
        private static readonly SemaphoreSlim testRunnerManagerRegistrySyncRootSemaphore = new SemaphoreSlim(1);
        private const int FixedLogicalThreadId = 0;

        public static async Task<ITestRunnerManager> GetTestRunnerManagerAsync(Assembly testAssembly = null, IContainerBuilder containerBuilder = null, bool createIfMissing = true)
        {
            testAssembly = testAssembly ?? GetCallingAssembly();

            if (!testRunnerManagerRegistry.TryGetValue(testAssembly, out var testRunnerManager))
            {
                await testRunnerManagerRegistrySyncRootSemaphore.WaitAsync();
                try
                {
                    if (!testRunnerManagerRegistry.TryGetValue(testAssembly, out testRunnerManager))
                    {
                        if (!createIfMissing)
                            return null;

                        testRunnerManager = CreateTestRunnerManager(testAssembly, containerBuilder);
                        testRunnerManagerRegistry.Add(testAssembly, testRunnerManager);
                    }
                }
                finally
                {
                    testRunnerManagerRegistrySyncRootSemaphore.Release();
                }
            }
            return testRunnerManager;
        }

        /// <summary>
        /// This is a workaround method solving not correctly working Assembly.GetCallingAssembly() when called from async method (due to state machine).
        /// </summary>
        private static Assembly GetCallingAssembly([CallerMemberName] string callingMethodName = null)
        {
            var stackTrace = new StackTrace();

            var callingMethodIndex = -1;

            for (var i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);

                if (frame.GetMethod().Name == callingMethodName)
                {
                    callingMethodIndex = i;
                    break;
                }
            }

            Assembly result = null;

            if (callingMethodIndex >= 0 && callingMethodIndex + 1 < stackTrace.FrameCount)
            {
                result = stackTrace.GetFrame(callingMethodIndex + 1).GetMethod().DeclaringType?.Assembly;
            }

            return result ?? Assembly.GetCallingAssembly();
        }

        private static ITestRunnerManager CreateTestRunnerManager(Assembly testAssembly, IContainerBuilder containerBuilder = null)
        {
            containerBuilder = containerBuilder ?? new ContainerBuilder();

            var container = containerBuilder.CreateGlobalContainer(testAssembly);
            var testRunnerManager = container.Resolve<ITestRunnerManager>();
            testRunnerManager.Initialize(testAssembly);
            return testRunnerManager;
        }

        public static async Task OnTestRunEndAsync(Assembly testAssembly = null)
        {
            testAssembly = testAssembly ?? Assembly.GetCallingAssembly();
            var testRunnerManager = await GetTestRunnerManagerAsync(testAssembly, createIfMissing: false);
            if (testRunnerManager != null)
            {
                await testRunnerManager.DisposeAsync();
            }
        }

        public static async Task<ITestRunner> GetTestRunnerAsync(Assembly testAssembly = null, int? managedThreadId = null)
        {
            testAssembly = testAssembly ?? Assembly.GetCallingAssembly();
            managedThreadId = GetLogicalThreadId(managedThreadId);
            var testRunnerManager = await GetTestRunnerManagerAsync(testAssembly);
            return await testRunnerManager.GetTestRunnerAsync(managedThreadId.Value);
        }

        private static int GetLogicalThreadId(int? managedThreadId)
        {
            if (ParallelExecutionIsDisabled())
            {
                return FixedLogicalThreadId;
            }

            return managedThreadId ?? Thread.CurrentThread.ManagedThreadId;
        }

        private static bool ParallelExecutionIsDisabled()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariableNames.NCrunch)) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariableNames.SpecflowDisableParallelExecution)))
            {
                return true;
            }

            return false;
        }

        internal static async Task ResetAsync()
        {
            ITestRunnerManager[] testRunnerManagers;
    
            await testRunnerManagerRegistrySyncRootSemaphore.WaitAsync();
            try
            {
                testRunnerManagers = testRunnerManagerRegistry.Values.ToArray();
                testRunnerManagerRegistry.Clear();
            }
            finally
            {
                testRunnerManagerRegistrySyncRootSemaphore.Release();
            }

            foreach (var testRunnerManager in testRunnerManagers)
            {
                await testRunnerManager.DisposeAsync();
            }
        }


        private static async Task OnTestRunnerManagerDisposed(TestRunnerManager testRunnerManager)
        {
            await testRunnerManagerRegistrySyncRootSemaphore.WaitAsync();
            try
            {
                if (testRunnerManagerRegistry.ContainsKey(testRunnerManager.TestAssembly))
                    testRunnerManagerRegistry.Remove(testRunnerManager.TestAssembly);
            }
            finally
            {
                testRunnerManagerRegistrySyncRootSemaphore.Release();
            }
        }

        #endregion
    }
}