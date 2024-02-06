using FluentAssertions;

namespace MediaHelper.Tests
{
    [TestClass]
    public class ExifWatcherWrapperTest
    {
        [TestMethod]
        public void ShouldStartNewWatcher()
        {
            ////Arrange
            //Task.Run(() =>
            //{
            //    Task.Delay(1).Wait();
            //    ExifWatcherWrapper.StartNewWatcher();
            //});

            ////Act
            //var Started = Helper.DidProcessStartInTimeRange(
            //    ExifWatcherWrapper.ToolName, 10_000);

            ////Assert
            //Started.Should().BeTrue();
        }
    }
}