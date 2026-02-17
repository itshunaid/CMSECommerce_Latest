using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CMSECommerce.Infrastructure
{
    // Used to define Extension methods for ISession
    public static class SessionExtensions
    {
        public static void SetJson(this ISession session, string key, object value)
        {
            if (session == null) return;

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(value, settings);
            session.SetString(key, json);
        }

        public static T GetJson<T>(this ISession session, string key)
        {
            if (session == null) return default;

            var sessionData = session.GetString(key);
            if (sessionData == null) return default;
            try
            {
                return JsonConvert.DeserializeObject<T>(sessionData);
            }
            catch
            {
                // If deserialization fails, return default to avoid breaking calling code
                return default;
            }
        }
    }
}