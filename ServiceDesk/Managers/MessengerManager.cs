using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using ServiceDesk.Models;

namespace ServiceDesk.Managers
{
    //==================================================================================================================
    public class MessengerManager
    {
        //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public string SendEmail(string subject, string header, string content, string to, List<string> cc = null)
        {
            //cc = new List<string>();

            string mail;
            mail = "<html>";
            mail += "<table>";
            mail += "<label>" + header + "</label></td></tr>";
            mail += "<tr><td colspan='2' style='width: 300px;font-weight: bold;'>";
            mail += "<tr><td colspan='2'>" + content + "</td></tr>";
            mail += "<br>";
            mail += "</table>";
            mail += "<br><br>";
            mail += "</html>";
            //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://appext.pentafon.com/ApiPentafon/Mailing/BasicSend");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";


                //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {

                    string json = new JavaScriptSerializer().Serialize(new
                    {
                        Host = "smtp.office365.com",
                        port = "587",
                        mailaddress = "sdpentaclick@pentafon.com",
                        emailpassword = "5dp3ntacl1ck20*",
                        mailTo = to,
                        cc = cc,
                        mailTitle = subject,
                        mailMessage = mail
                    });


                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    if (streamReader != null)
                    {
                        var result = streamReader.ReadToEnd();
                    }
                }
                //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
                return httpResponse.StatusDescription;

            }

            catch (WebException ex)
            {
                using (WebResponse response = ex.Response)
                {
                    var httpResponse = (HttpWebResponse)response;

                    using (Stream data = response.GetResponseStream())
                    {
                        StreamReader sr = new StreamReader(data);
                        Console.Write(sr.ReadToEnd());
                        throw new Exception(sr.ReadToEnd());
                    }
                }
            }
        }
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    }
    //==================================================================================================================
}
                    //DEBUGGING----------------- START
                    //string json = new JavaScriptSerializer().Serialize(new
                    //{
                    //    Host = "smtp.ionos.mx",
                    //    port = "465",
                    //    mailaddress = "",     //
                    //    emailpassword = "*",  //
                    //    mailTo = to,
                    //    cc = cc,
                    //    mailTitle = subject,
                    //    mailMessage = mail
                    //});
                    //DEBUGGING----------------- END