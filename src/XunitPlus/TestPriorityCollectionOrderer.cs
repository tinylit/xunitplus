using Xunit;

namespace XunitPlus
{
    /// <summary>
    /// 保持测试用例的原始顺序，不进行重新排序。
    /// 用于保留已经按照 StepAttribute 排序的测试用例顺序。
    /// </summary>
    public class StepCollectionOrderer : ITestCollectionOrderer
    {
        private readonly IReadOnlyDictionary<Guid, Type> _uniqueTypes;

        public StepCollectionOrderer(IReadOnlyDictionary<Guid, Type> uniqueTypes)
        {
            _uniqueTypes = uniqueTypes;
        }

        public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
        {
            // 直接返回原始顺序，不进行重新排序
            return testCollections.OrderBy(tc => tc.CollectionDefinition is null ? int.MaxValue : Step(tc.CollectionDefinition.ToRuntimeType()))
                .ThenBy(tc => _uniqueTypes.TryGetValue(tc.UniqueID, out var serverType) ? Step(serverType) : int.MaxValue)
                .ThenBy(tc => tc.DisplayName);
        }

        private static int Step(Type type)
        {
            return type.GetCustomAttributes(typeof(TestPriorityAttribute), true)
                .Cast<TestPriorityAttribute>()
                .Select(x => x.Value)
                .DefaultIfEmpty(int.MaxValue)
                .Min();
        }
    }
}