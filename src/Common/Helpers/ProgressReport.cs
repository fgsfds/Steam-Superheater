namespace Common.Helpers
{
    /// <summary>
    /// Operation progress reporter
    /// </summary>
    public sealed class ProgressReport
    {
        public ProgressReport()
        {
            Progress = new Progress<float>();
        }

        /// <summary>
        /// Progress value
        /// </summary>
        public Progress<float> Progress { get; set; }

        private string _operationMessage;
        /// <summary>
        /// Operation message
        /// </summary>
        public string OperationMessage
        {
            get => _operationMessage;
            set
            {
                if (_operationMessage != value)
                {
                    _operationMessage = value;
                    NotifyOperationMessageChanged?.Invoke(value);
                }
            }
        }

        public delegate void OperationMessageChanged(string message);
        public event OperationMessageChanged NotifyOperationMessageChanged;
    }
}
