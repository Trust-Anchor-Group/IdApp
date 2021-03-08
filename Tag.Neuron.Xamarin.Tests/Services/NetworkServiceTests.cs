using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin.Services;

namespace Tag.Neuron.Xamarin.Tests.Services
{
    public class NetworkServiceTests
    {
        private readonly TestNetworkService networkService;
        private readonly Mock<ILogService> logService;
        private readonly Mock<IUiDispatcher> dispatcher;

        public NetworkServiceTests()
        {
            this.dispatcher = new Mock<IUiDispatcher>();
            this.logService = new Mock<ILogService>();
            this.networkService = new TestNetworkService(this.logService.Object, this.dispatcher.Object);
        }

        private sealed class TestNetworkService : NetworkService
        {
            public TestNetworkService(ILogService logService, IUiDispatcher uiDispatcher)
            : base(logService, uiDispatcher)
            {
            }

            public bool OnlineFlag { get; set; }

            public override bool IsOnline => OnlineFlag;
        }

        private async Task VerifyRequest<TException, TExceptionRethrown>(Exception e, string message, bool rethrowException, bool displayAlert)
            where TException : Exception
            where TExceptionRethrown : Exception
        {
            Task<bool> TestFuncThatThrows()
            {
                if (!(e is null))
                {
                    throw e;
                }

                return Task.FromResult(true);
            }

            Exception typeOfExceptionCaught = null;
            try
            {
                await this.networkService.TryRequest(TestFuncThatThrows, rethrowException, displayAlert, nameof(VerifyRequest));
            }
            catch (Exception ex)
            {
                typeOfExceptionCaught = ex;
            }
            if (rethrowException)
            {
                Assert.IsInstanceOf<TExceptionRethrown>(typeOfExceptionCaught);
            }

            this.logService.Verify(x => x.LogException(It.IsAny<TException>(), It.IsAny<KeyValuePair<string, string>[]>()), Times.Once);
            this.logService.Reset();
            var displayAlertCount = displayAlert ? Times.Once() : Times.Never();
            this.dispatcher.Verify(x => x.DisplayAlert(It.Is<string>(title => title.Equals(AppResources.ErrorTitle)), It.Is<string>(msg => msg.StartsWith(message))), displayAlertCount);
            this.dispatcher.Reset();
        }

        [Test]
        public async Task Request_CatchesException_AndLogs_AndAlerts()
        {
            this.networkService.OnlineFlag = true;
            await VerifyRequest<TimeoutException, TimeoutException>(new TimeoutException(), AppResources.RequestTimedOut, false, false);
            await VerifyRequest<TaskCanceledException, TaskCanceledException>(new TaskCanceledException(), AppResources.RequestWasCancelled, false, false);
            await VerifyRequest<Exception, Exception>(new Exception("Ooops1"), "Ooops1", false, false);
            await VerifyRequest<TimeoutException, AggregateException>(new AggregateException(new TimeoutException()), AppResources.RequestTimedOut, false, false);
            await VerifyRequest<TaskCanceledException, AggregateException>(new AggregateException(new TaskCanceledException()), AppResources.RequestWasCancelled, false, false);
            await VerifyRequest<Exception, AggregateException>(new AggregateException(new Exception("Ooops2")), "Ooops2", false, false);
        }

        [Test]
        public async Task Request_CatchesException_AndLogs_AndAlerts_AndRethrows()
        {
            this.networkService.OnlineFlag = true;
            await VerifyRequest<TimeoutException, TimeoutException>(new TimeoutException(), AppResources.RequestTimedOut, true, true);
            await VerifyRequest<TaskCanceledException, TaskCanceledException>(new TaskCanceledException(), AppResources.RequestWasCancelled, true, true);
            await VerifyRequest<Exception, Exception>(new Exception("Ooops1"), "Ooops1", true, true);
            await VerifyRequest<TimeoutException, AggregateException>(new AggregateException(new TimeoutException()), AppResources.RequestTimedOut, true, true);
            await VerifyRequest<TaskCanceledException, AggregateException>(new AggregateException(new TaskCanceledException()), AppResources.RequestWasCancelled, true, true);
            await VerifyRequest<Exception, AggregateException>(new AggregateException(new Exception("Ooops2")), "Ooops2", true, true);
        }

        [Test]
        public async Task Request_CatchesException_AndLogs_AndAlerts_WhenOffline()
        {
            this.networkService.OnlineFlag = false;
            await VerifyRequest<MissingNetworkException, MissingNetworkException>(new MissingNetworkException(AppResources.ThereIsNoNetwork), AppResources.ThereIsNoNetwork, false, true);
        }

        [Test]
        public async Task Request_CatchesException_AndLogs_AndAlerts_AndRethrows_WhenOffline()
        {
            this.networkService.OnlineFlag = false;
            await VerifyRequest<MissingNetworkException, MissingNetworkException>(new MissingNetworkException(AppResources.ThereIsNoNetwork), AppResources.ThereIsNoNetwork, true, true);
        }
    }
}