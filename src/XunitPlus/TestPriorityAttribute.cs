namespace XunitPlus
{
    /// <summary>
    ///     标记一个类的在多个类同时执行的执行优先级。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TestPriorityAttribute : Attribute
    {
        /// <summary>
        /// 获取测试步骤的执行优先级。
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// 初始化 <see cref="TestPriorityAttribute"/> 类的新实例。
        /// </summary>
        public TestPriorityAttribute()
        {
            Value = 0;
        }

        /// <summary>
        /// 初始化 <see cref="TestPriorityAttribute"/> 类的新实例。
        /// </summary>
        /// <param name="value">测试步骤的执行优先级。较小的值先执行。</param>
        public TestPriorityAttribute(int value)
        {
            Value = value;
        }
    }
}