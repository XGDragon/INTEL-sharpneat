namespace SharpNeat.Domains.IPD.Players.Generated
{
    struct IPDPlayerGeneratedTreeIterator
    {
        public bool Success { get { return (Result != null); } }
        public IPDGame.Choices[] Result { get; private set; }
        public int Alpha { get; private set; }
        public int Beta { get; private set; }
        //remember labels..

        public IPDPlayerGeneratedTreeIterator(IPDGame.Choices[] result, int a, int b)
        {
            Result = result;
            Alpha = a;
            Beta = b;
        }
    }
}
