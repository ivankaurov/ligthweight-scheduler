namespace Lightweight.Scheduler.Core.Configuration
{
    using System.ComponentModel.DataAnnotations;

    public sealed class WorkerPoolOptions
    {
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
    }
}