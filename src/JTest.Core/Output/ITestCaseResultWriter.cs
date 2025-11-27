using JTest.Core.Models;

namespace JTest.Core.Output
{
    public interface ITestCaseResultWriter
    {
        void Write(TextWriter writer, JTestCaseResult testCaseResult, bool isDebug);
    }
}
