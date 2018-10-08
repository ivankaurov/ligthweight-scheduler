namespace Lightweight.Scheduler.Tests.Unit.Internal
{
    using AutoFixture.Xunit2;

    internal class AutoMoqInlineDataAttribute : InlineAutoDataAttribute
    {
        public AutoMoqInlineDataAttribute(params object[] values)
            : base(new AutoMoqDataAttribute(), values)
        {
        }
    }
}
