// -----------------------------------------------------------------------
// <copyright file="IPersistentContext.cs" company="Intermedia">
//   Copyright © Intermedia.net, Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Lightweight.Scheduler.Abstractions.Internal
{
    public interface IPersistentContext
    {
        IUserContext UserContext { get; }

        int Attempt { get; set; }
    }
}