namespace XunitPlus;

/// <summary>
/// 保持测试用例的原始顺序，不进行重新排序。
/// 用于保留已经按照 StepAttribute 排序的测试用例顺序。
/// </summary>
public class StepOrderer : ITestCaseOrderer
{
    /// <summary>
    /// 返回测试用例的原始顺序，不进行任何排序操作。
    /// </summary>
    /// <typeparam name="TTestCase">测试用例的类型。</typeparam>
    /// <param name="testCases">要排序的测试用例集合。</param>
    /// <returns>保持原始顺序的测试用例集合。</returns>
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        // 直接返回原始顺序，不进行重新排序
        return testCases.OrderBy(tc =>
                    {
                        var method = tc.TestMethod.Method.ToRuntimeMethod();

                        return method.GetCustomAttributes(typeof(StepAttribute), true)
                            .Cast<StepAttribute>()
                            .Select(x => x.Priority)
                            .DefaultIfEmpty(int.MaxValue)
                            .Min();
                    })
                    .ThenBy(tc => tc.TestMethod.Method.Name);
    }
}
