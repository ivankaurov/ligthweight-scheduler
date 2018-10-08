namespace Lightweight.Scheduler.Tests.Unit.Internal
{
    using System;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Xunit2;

    internal sealed class AutoMoqDataAttribute : AutoDataAttribute
    {
        private static readonly string DatabaseName = $"Database_{Guid.NewGuid()}";

        private static readonly Random Rnd = new Random();

        public AutoMoqDataAttribute()
            : base(CreateFixture)
        {
        }

        private static Fixture CreateFixture()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            return fixture;
        }
    }
}
