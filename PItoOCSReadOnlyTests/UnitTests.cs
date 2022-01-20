using Xunit;

namespace PItoOCSReadOnlyTests
{
    public class UnitTests
    {
        [Fact]
        public void TestMain()
        {
            Assert.True(PItoOCSReadOnly.Program.MainAsync(true).Result);
        }
    }
}
