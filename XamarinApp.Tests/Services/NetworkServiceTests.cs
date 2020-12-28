using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Services;

namespace XamarinApp.Tests.Services
{
    public class NetworkServiceTests
    {
        private readonly TestNetworkService networkService;
        private readonly Mock<ILogService> logService;
        private readonly Mock<INavigationService> navigationService;

        public NetworkServiceTests()
        {
            this.logService = new Mock<ILogService>();
            this.networkService = new TestNetworkService(this.logService.Object);
            this.navigationService = new Mock<INavigationService>();
        }

        private sealed class TestNetworkService : NetworkService
        {
            public TestNetworkService(ILogService logService)
            : base(logService)
            {
            }

            public bool OnlineFlag { get; set; }

            public override bool IsOnline => OnlineFlag;
        }

        private async Task VerifyRequest<TException, TExceptionRethrown>(Exception e, string message, bool rethrowException)
            where TException : Exception
            where TExceptionRethrown : Exception
        {
            Task<bool> TestFuncThatThrows()
            {
                if (e != null)
                {
                    throw e;
                }

                return Task.FromResult(true);
            }

            Exception typeOfExceptionCaught = null;
            try
            {
                await this.networkService.Request(this.navigationService.Object, TestFuncThatThrows, rethrowException);
            }
            catch (Exception ex)
            {
                typeOfExceptionCaught = ex;
            }
            if (rethrowException)
            {
                Assert.IsInstanceOf<TExceptionRethrown>(typeOfExceptionCaught);
            }

            this.logService.Verify(x => x.LogException(It.IsAny<TException>()), Times.Once);
            this.logService.Reset();
            this.navigationService.Verify(x => x.DisplayAlert(It.Is<string>(title => title.Equals(Tag.Sdk.Core.AppResources.ErrorTitle)), It.Is<string>(msg => msg.Equals(message))), Times.Once);
            this.navigationService.Reset();
        }

        [Test]
        public async Task Request_CatchesException_AndLogs_AndAlerts()
        {
            this.networkService.OnlineFlag = true;
            await VerifyRequest<TimeoutException, TimeoutException>(new TimeoutException(), AppResources.RequestTimedOut, false);
            await VerifyRequest<TaskCanceledException, TaskCanceledException>(new TaskCanceledException(), AppResources.RequestWasCancelled, false);
            await VerifyRequest<Exception, Exception>(new Exception("Ooops1"), "Ooops1", false);
            await VerifyRequest<TimeoutException, AggregateException>(new AggregateException(new TimeoutException()), AppResources.RequestTimedOut, false);
            await VerifyRequest<TaskCanceledException, AggregateException>(new AggregateException(new TaskCanceledException()), AppResources.RequestWasCancelled, false);
            await VerifyRequest<Exception, AggregateException>(new AggregateException(new Exception("Ooops2")), "Ooops2", false);
        }

        [Test]
        public async Task Request_CatchesException_AndLogs_AndAlerts_AndRethrows()
        {
            this.networkService.OnlineFlag = true;
            await VerifyRequest<TimeoutException, TimeoutException>(new TimeoutException(), AppResources.RequestTimedOut, true);
            await VerifyRequest<TaskCanceledException, TaskCanceledException>(new TaskCanceledException(), AppResources.RequestWasCancelled, true);
            await VerifyRequest<Exception, Exception>(new Exception("Ooops1"), "Ooops1", true);
            await VerifyRequest<TimeoutException, AggregateException>(new AggregateException(new TimeoutException()), AppResources.RequestTimedOut, true);
            await VerifyRequest<TaskCanceledException, AggregateException>(new AggregateException(new TaskCanceledException()), AppResources.RequestWasCancelled, true);
            await VerifyRequest<Exception, AggregateException>(new AggregateException(new Exception("Ooops2")), "Ooops2", true);

        }

        [Test]
        public async Task Request_CatchesException_AndLogs_AndAlerts_WhenOffline()
        {
            this.networkService.OnlineFlag = false;
            await VerifyRequest<MissingNetworkException, MissingNetworkException>(new MissingNetworkException(AppResources.ThereIsNoNetwork), AppResources.ThereIsNoNetwork, false);
        }

        [Test]
        public async Task Request_CatchesException_AndLogs_AndAlerts_AndRethrows_WhenOffline()
        {
            this.networkService.OnlineFlag = false;
            await VerifyRequest<MissingNetworkException, MissingNetworkException>(new MissingNetworkException(AppResources.ThereIsNoNetwork), AppResources.ThereIsNoNetwork, true);
        }
    }
}