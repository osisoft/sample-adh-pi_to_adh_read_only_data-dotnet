using Xunit;

namespace PItoADHReadOnlyTests
{
    public class UnitTests
    {
        [Fact]
        public void TestMain()
        {
            Assert.True(PItoADHReadOnly.Program.MainAsync(true).Result);
        }
    }
}
