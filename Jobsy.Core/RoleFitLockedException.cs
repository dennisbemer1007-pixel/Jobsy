namespace Jobsy.Core;

public sealed class RoleFitLockedException : InvalidOperationException
{
    public RoleFitLockedException()
        : base(Rules.RoleFitCheckCopy.Locked)
    {
    }
}
