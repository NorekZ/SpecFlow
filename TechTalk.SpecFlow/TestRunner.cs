using System.Threading.Tasks;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;

namespace TechTalk.SpecFlow
{
    public class TestRunner : ITestRunner
    {
        private readonly ITestExecutionEngine executionEngine;

        public int ThreadId { get; private set; }

        public TestRunner(ITestExecutionEngine executionEngine)
        {
            this.executionEngine = executionEngine;
        }

        public FeatureContext FeatureContext
        {
            get { return executionEngine.FeatureContext; }
        }

        public ScenarioContext ScenarioContext
        {
            get { return executionEngine.ScenarioContext; }
        }

        public async Task OnTestRunStartAsync()
        {
            await executionEngine.OnTestRunStartAsync();
        }

        public void InitializeTestRunner(int threadId)
        {
            ThreadId = threadId;
        }

        public async Task OnFeatureStartAsync(FeatureInfo featureInfo)
        {
            await executionEngine.OnFeatureStartAsync(featureInfo);
        }

        public async Task OnFeatureEndAsync()
        {
            await executionEngine.OnFeatureEndAsync();
        }

        public void OnScenarioInitialize(ScenarioInfo scenarioInfo)
        {
            executionEngine.OnScenarioInitialize(scenarioInfo);
        }

        public async Task OnScenarioStartAsync()
        {
            await executionEngine.OnScenarioStartAsync();
        }

        public async Task CollectScenarioErrorsAsync()
        {
            await executionEngine.OnAfterLastStepAsync();
        }

        public async Task OnScenarioEndAsync()
        {
            await executionEngine.OnScenarioEndAsync();
        }

        public async Task OnTestRunEndAsync()
        {
            await executionEngine.OnTestRunEndAsync();
        }

        public async Task GivenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            await executionEngine.StepAsync(StepDefinitionKeyword.Given, keyword, text, multilineTextArg, tableArg);
        }

        public async Task WhenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            await executionEngine.StepAsync(StepDefinitionKeyword.When, keyword, text, multilineTextArg, tableArg);
        }

        public async Task ThenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            await executionEngine.StepAsync(StepDefinitionKeyword.Then, keyword, text, multilineTextArg, tableArg);
        }

        public async Task AndAsync(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            await executionEngine.StepAsync(StepDefinitionKeyword.And, keyword, text, multilineTextArg, tableArg);
        }

        public async Task ButAsync(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            await executionEngine.StepAsync(StepDefinitionKeyword.But, keyword, text, multilineTextArg, tableArg);
        }

        public void Pending()
        {
            executionEngine.Pending();
        }
    }
}
