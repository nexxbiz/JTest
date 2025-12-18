using JTest.Core.Models;
using JTest.Core.Steps;

namespace JTest.Core.Execution;

/// <summary>
/// Service for executing test cases with dataset support.
/// 
/// Variable Scoping Rules:
/// - env: Immutable variables that never change across iterations (e.g., baseUrl, credentials)
/// - globals: Shared state that persists across all dataset iterations (modifications in one iteration are visible in subsequent iterations)
/// - ctx, this, named variables: Reset to original values for each iteration to prevent pollution between datasets
/// 
/// This ensures proper isolation between dataset iterations while maintaining shared global state when needed.
/// </summary>
public class TestCaseExecutor
{
    
}