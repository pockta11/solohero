namespace SoloHero.Core.Common
{
    public readonly struct Result
    {
        public readonly bool Ok;
        public readonly FailReason Reason;

        private Result(bool ok, FailReason reason)
        {
            Ok = ok;
            Reason = reason;
        }

        public static readonly Result Success = new Result(true, FailReason.None);

        public static Result Fail(FailReason reason) => new Result(false, reason);
    }
}
