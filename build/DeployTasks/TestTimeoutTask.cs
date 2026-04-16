namespace BuildScripts
{
    [TaskName("Test HttpClient.Timeout")]
    public sealed class TestTimeoutTask : AsyncFrostingTask<BuildContext>
    {
        private static readonly int DefaultTimeoutInSeconds = 100;
        private readonly Func<string, HttpClient> _httpClientFactoryFunction;

        public TestTimeoutTask(Func<string, HttpClient> httpClientFactoryFunction)
        {
            _httpClientFactoryFunction = httpClientFactoryFunction;
        }

        public override async Task RunAsync(BuildContext context)
        {
            var testClient = _httpClientFactoryFunction("HttpClient.Timeout");

            var timeoutInSeconds = testClient.Timeout.TotalSeconds;

            if (timeoutInSeconds > DefaultTimeoutInSeconds)
            {
                context.Log.Information($"HttpClient.Timeout is set at: {testClient.Timeout.TotalSeconds} seconds.");
            }
            else if (Math.Abs(timeoutInSeconds - DefaultTimeoutInSeconds) < 0.01)
            {
                context.Log.Warning($"HttpClient.Timeout is set at: {testClient.Timeout.TotalSeconds} seconds.");
            }
            else
            {
                context.Log.Error($"HttpClient.Timeout is set at: {testClient.Timeout.TotalSeconds} seconds.");
            }
        }
    }
}
