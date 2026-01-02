using JTest.Core.Models;

namespace JTest.Core.Utilities;

public interface IGlobalConfigurationAccessor
{
    GlobalConfiguration Configuration { get; }
}