using System.Threading.Tasks;

namespace TechTalk.SpecFlow
{
    public interface ITestRunner
    {
        int ThreadId { get; }
        FeatureContext FeatureContext { get; }
        ScenarioContext ScenarioContext { get; }

        void InitializeTestRunner(int threadId);

        Task OnTestRunStartAsync();
        Task OnTestRunEndAsync();

        Task OnFeatureStartAsync(FeatureInfo featureInfo);
        Task OnFeatureEndAsync();

        void OnScenarioInitialize(ScenarioInfo scenarioInfo);
        Task OnScenarioStartAsync();

        void CollectScenarioErrors();
        Task OnScenarioEndAsync();

        Task GivenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null);
        Task WhenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null);
        Task ThenAsync(string text, string multilineTextArg, Table tableArg, string keyword = null);
        Task AndAsync(string text, string multilineTextArg, Table tableArg, string keyword = null);
        Task ButAsync(string text, string multilineTextArg, Table tableArg, string keyword = null);

        void Pending();
    }
}