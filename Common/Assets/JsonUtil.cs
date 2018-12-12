﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vintagestory.API.Common
{
    public class JsonUtil
    {
        /// <summary>
        /// Reads a Json object, and converts it to the designated type.
        /// </summary>
        /// <typeparam name="T">The designated type</typeparam>
        /// <param name="data">The json object.</param>
        /// <returns></returns>
        public static T FromBytes<T>(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8, true))
                {
                    return JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
                }
            }
        }

        /// <summary>
        /// Converts the object to json.
        /// </summary>
        /// <typeparam name="T">The type to convert</typeparam>
        /// <param name="obj">The object to convert</param>
        /// <returns></returns>
        public static byte[] ToBytes<T>(T obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }


        public static void PopulateObject(object toPopulate, string text, string domain, JsonSerializerSettings settings = null)
        {
            if (domain != "game")
            {
                if (settings == null)
                {
                    settings = new JsonSerializerSettings();
                }
                settings.Converters.Add(new AssetLocationJsonParser(domain));
            }
            JsonConvert.PopulateObject(text, toPopulate, settings);
        }

        /// <summary>
        /// Converts a Json object to a typed object.
        /// </summary>
        /// <typeparam name="T">The type to convert.</typeparam>
        /// <param name="text">The text to deserialize</param>
        /// <param name="domain">The domain of the text.</param>
        /// <param name="settings">The settings of the deserializer. (default: Null)</param>
        /// <returns></returns>
        public static T ToObject<T>(string text, string domain, JsonSerializerSettings settings = null)
        {
            if (domain != "game")
            {
                if (settings == null)
                {
                    settings = new JsonSerializerSettings();
                }
                settings.Converters.Add(new AssetLocationJsonParser(domain));
            }
            
            return JsonConvert.DeserializeObject<T>(text, settings);
        }
    }

    public class AssetLocationJsonParser : JsonConverter
    {
        string domain;

        public AssetLocationJsonParser(string domain)
        {
            this.domain = domain;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AssetLocation);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value is string)
            {
                AssetLocation location = new AssetLocation(reader.Value as string);
                if (!location.HasDomain())
                {
                    location.Domain = domain;
                }
                return location;
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
