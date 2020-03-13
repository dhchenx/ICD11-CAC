using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using IdentityModel.Client;
using System.Text.Encodings.Web;
using RestSharp;
using ICD11CodingTool.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;

namespace ICD11CodingTool.Controller
{
    public class ICD11ApiClient
    {

        private dynamic prettyJson;

        public dynamic GetJson()
        {
            return prettyJson;
        }

        public async Task<List<ICD11Entity>> GetResult(string term,string chapterlist)
        {
            try
            {
                List<ICD11Entity> icd11list = new List<ICD11Entity>();

                dynamic searchResult = await Search(term);
                foreach (var de in searchResult.DestinationEntities)
                {
                    Console.WriteLine(de.TheCode + " " + de.Title);
                    ICD11Entity ie = new ICD11Entity();
                    ie.Code = de.TheCode;
                    ie.Title = de.Title;
                    ie.Score = Convert.ToDouble(de.Score);
                    string Id = de.Id;
                    ie.Id = Id;
                    ie.Data = de;
                    ie.Chapter = de.Chapter;


                    foreach (var pv in de.MatchingPVs)
                    {
                        Console.WriteLine("-" + pv.Label);
                        //string pv_id = pv.PVIWE.Id;
                        PV p1 = new PV();
                       // p1.Id = pv_id;
                        p1.Label = pv.Label;
                        p1.Data = de;
                        p1.Score = pv.Score;
                        ie.PVList.Add(p1);

                    }

                    foreach (var des in de.Descendants)
                    {
                        Console.WriteLine("--" + des.Title + " " + des.TheCode);
                        ICD11Entity ie1 = new ICD11Entity();
                        ie1.Code = des.TheCode;
                        ie1.Title = des.Title;

                        ie1.Data = des;
                        ie1.Id = des.Id;
                        ie1.Score = des.Score;
                        ie1.Chapter = des.Chapter;

                        foreach (var pv1 in des.MatchingPVs)
                        {
                            Console.WriteLine("-" + pv1.Label);
                           // string pv1_id = pv1.PVIWE.Id;
                            PV p2 = new PV();
                           // p2.Id = pv1_id;
                            p2.Label = pv1.Label;
                            p2.Data = des;
                            p2.Score = pv1.Score;
                            ie1.PVList.Add(p2);

                        }


                        ie.Children.Add(ie1);
                    }

                    icd11list.Add(ie);
                }
                return icd11list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<ICD11Entity>();
            }
        }

        private async Task<dynamic> Search(string term)
        {
            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var lines = new string[] {  config.AppSettings.Settings["clientId"].Value,
            config.AppSettings.Settings["clientSecret"].Value};


            var clientId = lines[0];
            var clientSecret = lines[1];

            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://icdaccessmanagement.who.int");
            if (disco.IsError) throw new Exception(disco.Error);

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = "icdapi_access",
                GrantType = "client_credentials",
                ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);

            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            // call api
            client = new HttpClient();
            client.SetBearerToken(tokenResponse.AccessToken);

            HttpRequestMessage request;

            Console.WriteLine();
            Console.WriteLine("****************************************************************");
            Console.WriteLine("Requesting the root foundation URI...");
            request = new HttpRequestMessage(HttpMethod.Get, "https://id.who.int/icd/entity");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            request.Headers.Add("API-Version", "v1");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }

            var resultJson = response.Content.ReadAsStringAsync().Result;
            prettyJson = JValue.Parse(resultJson).ToString(Formatting.Indented); //convert json to a more human readable fashion
            Console.WriteLine(prettyJson);

            Console.WriteLine("****************************************************************");
            Console.WriteLine("Enter a search term:");

            request = new HttpRequestMessage(HttpMethod.Get,
                "https://id.who.int/icd/release/11/2019-04/mms/search?q=" + System.Web.HttpUtility.UrlDecode(term + "%")
                //  + "&useBroaderSynonyms=false&useFlexiSearch=false&theIndex=icd11_mms_en_2018-06-05" +
                 +"&chapterFilter="+chapterList
                );

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            request.Headers.Add("API-Version", "v1");

            response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }

            resultJson = response.Content.ReadAsStringAsync().Result; //Now resultJson has the resulting json string
            Console.WriteLine("****** Search result json *****");
            Console.WriteLine(resultJson);

            prettyJson = JValue.Parse(resultJson).ToString(Formatting.Indented); //convert json to a more human readable fashion
            Console.WriteLine("****** And the pretty json output *****");
            Console.WriteLine(prettyJson);

            //Now trying to parse and get titles from the search result

            Console.WriteLine("****** ICD code and titles from the search *****");
            dynamic searchResult = JsonConvert.DeserializeObject(resultJson);
            Console.WriteLine("Ending the program");

            return searchResult;



        }


        public async Task<List<ICD11Entity>> GetEntityResult(string term)
        {
            try
            {
                List<ICD11Entity> icd11list = new List<ICD11Entity>();

                dynamic searchResult = await SearchEntity(term);
                foreach (var de in searchResult.DestinationEntities)
                {
                    Console.WriteLine(de.TheCode + " " + de.Title);
                    ICD11Entity ie = new ICD11Entity();
                    ie.Code = de.TheCode;
                    ie.Title = de.Title;
                    ie.Score = Convert.ToDouble(de.Score);
                    string Id = de.Id;
                    ie.Id = Id;
                    ie.Data = de;
                    ie.Chapter = de.Chapter;


                    foreach (var pv in de.MatchingPVs)
                    {
                        Console.WriteLine("-" + pv.Label);
                        //string pv_id = pv.PVIWE.Id;
                        PV p1 = new PV();
                        //p1.Id = pv_id;
                        p1.Label = pv.Label;
                        p1.Data = pv;
                        p1.Score = pv.Score;
                        ie.PVList.Add(p1);

                    }

                    foreach (var des in de.Descendants)
                    {
                        Console.WriteLine("--" + des.Title + " " + des.TheCode);
                        ICD11Entity ie1 = new ICD11Entity();
                        ie1.Code = des.TheCode;
                        ie1.Title = des.Title;

                        ie1.Data = des;
                        ie1.Id = des.Id;
                        ie1.Score = des.Score;
                        ie1.Chapter = des.Chapter;

                        foreach (var pv1 in des.MatchingPVs)
                        {
                            Console.WriteLine("-" + pv1.Label);
                            //string pv1_id = pv1.PVIWE.Id;
                            PV p2 = new PV();
                            //p2.Id = pv1_id;
                            p2.Label = pv1.Label;
                            p2.Data = pv1;
                            p2.Score = pv1.Score;
                            ie1.PVList.Add(p2);

                        }


                        ie.Children.Add(ie1);
                    }

                    icd11list.Add(ie);
                }
                return icd11list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<ICD11Entity>();
            }
        }


        private async Task<dynamic> SearchEntity(string term)
        {

            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var lines = new string[] {  config.AppSettings.Settings["clientId"].Value,
            config.AppSettings.Settings["clientSecret"].Value};


            var clientId = lines[0];
            var clientSecret = lines[1];

            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://icdaccessmanagement.who.int");
            if (disco.IsError) throw new Exception(disco.Error);

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = "icdapi_access",
                GrantType = "client_credentials",
                ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);

            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            // call api
            client = new HttpClient();
            client.SetBearerToken(tokenResponse.AccessToken);

            HttpRequestMessage request;

            Console.WriteLine();
            Console.WriteLine("****************************************************************");
            Console.WriteLine("Requesting the root foundation URI...");
            request = new HttpRequestMessage(HttpMethod.Get, "https://id.who.int/icd/entity");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }

            var resultJson = response.Content.ReadAsStringAsync().Result;
            prettyJson = JValue.Parse(resultJson).ToString(Formatting.Indented); //convert json to a more human readable fashion
            Console.WriteLine(prettyJson);

            Console.WriteLine("****************************************************************");
            Console.WriteLine("Enter a search term:");

            request = new HttpRequestMessage(HttpMethod.Get,
                "https://id.who.int/icd/release/11/2018/mms/search?q=" + System.Web.HttpUtility.UrlDecode(term + "%")
                 //  + "&useBroaderSynonyms=false&useFlexiSearch=false&theIndex=icd11_mms_en_2018-06-05" +
                 + "&chapterFilter=" + chapterList
                );

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }

            resultJson = response.Content.ReadAsStringAsync().Result; //Now resultJson has the resulting json string
            Console.WriteLine("****** Search result json *****");
            Console.WriteLine(resultJson);

            prettyJson = JValue.Parse(resultJson).ToString(Formatting.Indented); //convert json to a more human readable fashion
            Console.WriteLine("****** And the pretty json output *****");
            Console.WriteLine(prettyJson);

            //Now trying to parse and get titles from the search result

            Console.WriteLine("****** ICD code and titles from the search *****");
            dynamic searchResult = JsonConvert.DeserializeObject(resultJson);
            Console.WriteLine("Ending the program");

            return searchResult;



        }


        private List<string> chapterList;

        public List<string> ChapterList
        {
            get
            {
                return chapterList;
            }
        }

        public async Task<List<WordCandidate>> GetWordList(string term,string q_chapter)
        {

            var client = new RestClient("https://icd.who.int/ct11_2018-2/api/searchservice?theIndex=icd11_mms_en_2018-06-05&q=" + System.Web.HttpUtility.UrlDecode(term + "%") + "&chapterFilter="+q_chapter+"useBroaderSynonyms=false&useFlexiSearch=false");
            // client.Authenticator = new HttpBasicAuthenticator(username, password);

            var request = new RestRequest(Method.GET);

            Task<IRestResponse> t = client.ExecuteTaskAsync(request);
            t.Wait();
            var restResponse = await t;
            //  Console.WriteLine(restResponse.Content);


            //    dynamic  prettyJson = JValue.Parse(restResponse.Content).ToString(Formatting.Indented); //convert json to a more human readable fashion
            dynamic searchResult = JsonConvert.DeserializeObject(restResponse.Content);
            List<WordCandidate> wlist = new List<WordCandidate>();

            var wordlist = searchResult.words;
            var chapters = searchResult.searchQuery.chapterFilters;
            List<string> clist = new List<string>();
            foreach (var chap in chapters)
            {
                clist.Add(Convert.ToString(chap));
            }

            chapterList = clist;

            foreach (var word in wordlist)
            {
                WordCandidate wc = new WordCandidate();
                wc.Label = word.label;
                wlist.Add(wc);
            }

            return wlist;

        }

    }
}
