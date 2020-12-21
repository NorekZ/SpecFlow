using System.Threading.Tasks;
using TechTalk.SpecFlow.CommonModels;

namespace TechTalk.SpecFlow.Analytics
{
    public interface IAnalyticsTransmitter
    {
        Task<IResult> TransmitSpecFlowProjectCompilingEventAsync(SpecFlowProjectCompilingEvent projectCompilingEvent);
        Task<IResult> TransmitSpecFlowProjectRunningEventAsync(SpecFlowProjectRunningEvent projectRunningEvent);
    }
}
