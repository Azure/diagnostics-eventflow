namespace Microsoft.Diagnostics.EventFlow
{
    public interface IRequireActivation
    {
        /// <summary>
        /// Invoke the activation.
        /// </summary>
        void Activate();
    }
}
