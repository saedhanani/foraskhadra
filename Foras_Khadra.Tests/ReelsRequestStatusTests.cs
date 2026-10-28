using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class ReelsRequestStatusTests
    {
        [Fact]
        public void NewRequest_IsPending()
        {
            Assert.Equal(ReelsRequestStatus.Pending, new ReelsRequest().Status);
        }

        [Theory]
        [InlineData(ReelsRequestStatus.Pending)]
        [InlineData(ReelsRequestStatus.InProgress)]
        [InlineData(ReelsRequestStatus.Completed)]
        [InlineData(ReelsRequestStatus.Rejected)]
        public void SetStatus_RoundTripsThroughTheFlags(ReelsRequestStatus status)
        {
            var request = new ReelsRequest { IsInProgress = true, IsCompleted = true, IsRejected = true };

            request.SetStatus(status);

            Assert.Equal(status, request.Status);
            // علم واحد على الأكثر يبقى صحيحًا
            var flagsSet = new[] { request.IsInProgress, request.IsCompleted, request.IsRejected }.Count(f => f);
            Assert.True(flagsSet <= 1);
        }

        [Fact]
        public void Status_RejectionWinsOverStaleFlags()
        {
            // بيانات قديمة قد تحمل أكثر من علم؛ الرفض هو ما يراه المستخدم
            var request = new ReelsRequest { IsInProgress = true, IsCompleted = true, IsRejected = true };

            Assert.Equal(ReelsRequestStatus.Rejected, request.Status);
        }
    }
}
