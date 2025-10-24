namespace XunitPlus
{
    /// <summary>
    /// 标记一个方法为测试步骤，可选指定执行顺序的优先级。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class StepAttribute : Attribute
    {
        /// <summary>
        /// 获取测试步骤的执行优先级。
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 初始化 <see cref="StepAttribute"/> 类的新实例。
        /// </summary>
        public StepAttribute()
        {
            Priority = 0;
        }

        /// <summary>
        /// 使用指定的优先级初始化 <see cref="StepAttribute"/> 类的新实例。
        /// </summary>
        /// <param name="priority">测试步骤的执行优先级。较小的值先执行。</param>
        public StepAttribute(int priority)
        {
            Priority = priority;
        }
    }
}