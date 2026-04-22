using Xunit;

namespace XunitPlus;

/// <summary>
/// 按照 <see cref="StepAttribute"/> 的优先级对测试用例进行排序。
/// 优先级值越小越先执行，未标记的测试用例排在最后。
/// </summary>
public class StepOrderer : ITestCaseOrderer
{
    /// <summary>
    /// 按照 <see cref="StepAttribute.Priority"/> 升序排列测试用例，相同优先级按方法名排序。
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