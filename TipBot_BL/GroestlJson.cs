using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TipBot_BL {
    public class GroestlJson {
        public static string TipBotRequest(string methodName, List<string> parameters) {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Preferences.QT_IP);
            webRequest.Credentials = new NetworkCredential(Preferences.QT_Username, Preferences.QT_Password);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            string respValue = string.Empty;

            JObject joe = new JObject();
            joe.Add(new JProperty("jsonrpc", "1.0"));
            joe.Add(new JProperty("id", "1"));
            joe.Add(new JProperty("method", methodName));

            JArray props = new JArray();
            foreach (var parameter in parameters) {
                props.Add(parameter);
            }
            joe.Add(new JProperty("params", props));

            //serialize json for the request
            string s = JsonConvert.SerializeObject(joe);
            byte[] byteArray = Encoding.UTF8.GetBytes(s);
            webRequest.ContentLength = byteArray.Length;
            Stream dataSteam = webRequest.GetRequestStream();
            dataSteam.Write(byteArray, 0, byteArray.Length);
            dataSteam.Close();

            try {
                WebResponse webResponse = webRequest.GetResponse();
                var streamReader = new StreamReader(webResponse.GetResponseStream(), true);
                respValue = streamReader.ReadToEnd();
                var data = JsonConvert.DeserializeObject(respValue).ToString();
                return data;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return "Error";
            }
        }
    }
}
