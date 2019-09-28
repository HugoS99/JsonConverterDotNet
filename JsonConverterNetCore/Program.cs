using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JsonConverter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using (StreamReader reader = File.OpenText(@"C:\Users\hugos\Desktop\test.json"))
            {
                JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(reader));

                JToken token = o.SelectToken("$..['/pet/findByStatus'].get");

                List<JsonConverterType> list = JsonConverterJObj(o, "/pet/findByTags", "get");

                foreach (var item in list)
                {
                    Console.WriteLine(item.ToString());
                }

                Console
                    .Write("/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////\n");

                String output = JsonConvert.SerializeObject(list);

                Console.WriteLine(output);

                Console.Read();
            }
        }

        private static List<JsonConverterType> JsonConverterFilename(String filename, String HttpHeader, String methodToConvert)
        {
            StreamReader reader = File.OpenText(@"C:\Users\hugos\Desktop\test.json");

            JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(reader));

            return JsonConverterJObj(o, HttpHeader, methodToConvert);
        }

        private static List<JsonConverterType> JsonConverterJObj(JObject jObj, string HttpHeader, string methodToConvert)
        {
            StringBuilder builder = new StringBuilder();

            String JSONPathStringMethod = builder.AppendFormat("$..['{0}'].['{1}']", HttpHeader, methodToConvert.ToLower()).ToString();

            JToken token = jObj.SelectToken(JSONPathStringMethod);

            if (token is null)
            {
                throw new Exception("JSONPATH FAIL METHOD NOT FOUND");
            }

            if (methodToConvert.ToLower().Equals("put") || methodToConvert.ToLower().Equals("post"))
            {
                IEnumerable<JToken> tokenParameters = token.SelectTokens("$..parameters");

                if (tokenParameters != null)
                {
                    List<JsonConverterType> response = new List<JsonConverterType>();

                    foreach (JToken item in tokenParameters)
                    {
                        response.AddRange(PropertiesJSONConverterNoRef(item, jObj));
                    }
                    return response;
                }
            }

            JToken tokenSchema = token.SelectToken("$..responses..200");

            if (tokenSchema != null)
            {
                JToken tokenReference = tokenSchema.SelectToken("$..schema..['$ref']");

                if (tokenReference != null)
                {
                    return PropertiesJSONConverterRef(tokenReference, jObj);
                }
                else
                {
                    return GenericJSONObjCreator(tokenSchema);
                }
            }
            else
            {
                return GenericJSONObjCreator(token);
            }
        }

        private static List<JsonConverterType> PropertiesJSONConverterNoRef(JToken token, JObject root)
        {
            List<JsonConverterType> list = new List<JsonConverterType>();

            PropertiesJSONConverterRec(token, root, list, "");

            return list;
        }

        private static List<JsonConverterType> PropertiesJSONConverterRef(JToken token, JObject root)
        {
            List<JsonConverterType> list = new List<JsonConverterType>();

            String reference = token.Value<String>();

            reference = reference.Replace('#', '$');
            reference = reference.Replace('/', '.');
            JToken getReference = root.SelectToken(reference + "..properties");

            PropertiesJSONConverterRec(getReference, root, list, "");

            return list;
        }

        private static void PropertiesJSONConverterRec(JToken token, JObject root, List<JsonConverterType> list, String cat)
        {
            //String reference = token.Value<String>("$ref");

            //reference = reference.Replace('#', '$');
            //reference = reference.Replace('/', '.');
            //JToken getReference = jObj.SelectToken(reference + "..proper");

            foreach (JToken item in token.Children())
            {
                String[] splitted = item.Path.Split('.');
                String propertyname = splitted[splitted.Length - 1];
                String indexname = propertyname;
                var posNameToken = item.SelectToken("$.['name']");

                if (posNameToken != null)
                {
                    propertyname = posNameToken.Value<String>();
                }

                JToken tokenReference = item.SelectToken("$..['$ref']");

                if (tokenReference != null)
                {
                    String reference = tokenReference.Value<String>();

                    reference = reference.Replace('#', '$');
                    reference = reference.Replace('/', '.');
                    JToken getReference = root.SelectToken(reference + "..properties");

                    PropertiesJSONConverterRec(getReference, root, list, indexname + ".");
                }
                else
                {
                    foreach (var tempToken in item.SelectTokens("$..['type']"))
                    {
                        String nametype = tempToken.Value<String>();
                        if (!nametype.ToUpper().Equals("ARRAY"))
                        {
                            list.Add(new JsonConverterType(propertyname, cat + indexname, nametype));
                        }
                    }
                }
            }
        }

        private static List<JsonConverterType> GenericJSONObjCreator(JToken token)
        {
            List<JsonConverterType> list = new List<JsonConverterType>();

            GenericJSONObjCreatorRec(token, list, "");

            Console.WriteLine(list);

            return list;
        }

        private static void GenericJSONObjCreatorRec(JToken token, List<JsonConverterType> list, String cat)
        {
            foreach (JToken item in token.Children())
            {
                String[] splitted = item.Path.Split('.');
                String propertyname = splitted[splitted.Length - 1];
                JTokenType type = item.Type;

                if (type.Equals(JTokenType.Object) || type.Equals(JTokenType.Property))
                {
                    GenericJSONObjCreatorRec(item, list, propertyname + ".");
                }

                list.Add(new JsonConverterType(propertyname, cat + propertyname, type.ToString()));
            }
        }
    }
}