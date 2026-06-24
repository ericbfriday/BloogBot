using BloogBot.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class MainViewModelBindingTests
    {
        [TestMethod]
        public void CurrentGatherRouteEnabledBindingHasViewModelProperty()
        {
            var property = typeof(MainViewModel).GetProperty("CurrentGatherRouteEnabled");

            Assert.IsNotNull(property);
            Assert.AreEqual(typeof(bool), property.PropertyType);
        }
    }
}
