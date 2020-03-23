namespace Lightweight.Scheduler.Utils
{
    using System.Collections.Generic;

    using Microsoft.Extensions.Options;

    public abstract class ValidateOptionsBase<TOptions> : IValidateOptions<TOptions>
        where TOptions : class
    {
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            var errors = this.ValidateInternal(options).AsCollection();

            return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
        }

        protected abstract IEnumerable<string> ValidateInternal(TOptions options);
    }
}