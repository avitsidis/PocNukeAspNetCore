using System;
using Xunit;

namespace Demo.Core.Test
{
    public class WeatherForecastServiceTest
    {
        [Fact]
        public void Should_Return_Forecasts()
        {
            var service = new WeatherForecastService();
            Assert.NotEmpty(service.Get());
        }
    }
}
