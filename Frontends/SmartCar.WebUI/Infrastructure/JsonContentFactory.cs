global using static SmartCar.WebUI.Infrastructure.JsonContentFactory;

using Newtonsoft.Json;
using System.Text;

namespace SmartCar.WebUI.Infrastructure
{
    internal static class JsonContentFactory
    {
        public static StringContent JsonContent(object payload)
            => new(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
    }
}
